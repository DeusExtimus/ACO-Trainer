using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ACOverlay
{
    public partial class MapWindow : Window
    {
        const int GWL_EXSTYLE       = -20;
        const int WS_EX_LAYERED     = 0x80000;
        const int WS_EX_TRANSPARENT = 0x20;

        [DllImport("user32.dll")] static extern int  GetWindowLong(IntPtr h, int i);
        [DllImport("user32.dll")] static extern int  SetWindowLong(IntPtr h, int i, int v);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int ht, uint f);

        static readonly IntPtr HWND_TOPMOST = new(-1);
        const uint SWP_NOMOVE = 0x0002, SWP_NOSIZE = 0x0001;

        static readonly Brush BrushBestLap = new SolidColorBrush(Color.FromArgb(110, 255, 215, 0));
        static readonly Brush BrushCar     = new SolidColorBrush(Color.FromArgb(255, 255, 215, 0));
        static readonly Brush BrushGrid    = new SolidColorBrush(Color.FromArgb(20,  255, 255, 255));

        static MapWindow()
        {
            BrushBestLap.Freeze();
            BrushCar.Freeze();
            BrushGrid.Freeze();
        }

        readonly DispatcherTimer _timer = new();

        public MapWindow() { InitializeComponent(); }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var screen = SystemParameters.WorkArea;
            Left = screen.Right - Width - 8;
            Top  = screen.Top   + 40;
            // MakeClickThrough(); // DEBUG: deaktiviert

            _timer.Interval = TimeSpan.FromMilliseconds(50); // 20 fps reicht für Karte
            _timer.Tick += (_, _) => Render();
            _timer.Start();
        }

        void MakeClickThrough()
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            int style = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, style | WS_EX_LAYERED | WS_EX_TRANSPARENT);
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }

        void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed) DragMove();
        }

        void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

        void Render()
        {
            List<TrackPoint> best, current;
            float carX, carZ;

            lock (SharedState.Lock)
            {
                best    = new List<TrackPoint>(SharedState.BestLapPoints);
                current = new List<TrackPoint>(SharedState.CurrentLapPoints);
                carX    = SharedState.CarX;
                carZ    = SharedState.CarZ;
            }

            StatusDot.Foreground = SharedState.IsConnected
                ? new SolidColorBrush(Color.FromRgb(34, 255, 102))
                : new SolidColorBrush(Color.FromRgb(255, 68, 68));

            TxtLap.Text  = $"{SharedState.CurrentLap}";
            TxtTime.Text = SharedState.CurrentTime;
            TxtBest.Text = SharedState.BestLapStr.Length > 0 ? $"B {SharedState.BestLapStr}" : "";

            string track = SharedState.Track;
            string car   = SharedState.Car;
            TxtCombo.Text = track.Length > 0
                ? $"{track}  ·  {car}"
                : "TRACK MAP";

            DrawMap(best, current, carX, carZ);
        }

        void DrawMap(List<TrackPoint> best, List<TrackPoint> current, float carX, float carZ)
        {
            TrackCanvas.Children.Clear();

            double cw = TrackCanvas.ActualWidth;
            double ch = TrackCanvas.ActualHeight;
            if (cw < 10 || ch < 10) return;

            // Referenzlinie für die gesamte Strecke (für Bounding Box + Lookahead)
            var fullTrack = best.Count > 20 ? best : current;
            if (fullTrack.Count < 4) return;

            // ── Lookahead-Fenster ─────────────────────────────────────────────
            // Aktuelle NormPos aus SharedState lesen
            float carNorm = SharedState.CarNormPos;

            // Fenster: 6% zurück, 20% voraus — passt ca. 2-3 Kurven
            const float lookBack  = 0.06f;
            const float lookAhead = 0.22f;

            float winStart = (carNorm - lookBack  + 1f) % 1f;
            float winEnd   = (carNorm + lookAhead + 1f) % 1f;

            // Punkte im Fenster selektieren (mit Wraparound-Support)
            List<TrackPoint> WindowPts(List<TrackPoint> src)
            {
                if (src.Count < 4) return src;
                bool wraps = winStart > winEnd; // Ziel überschreitet 0/1-Grenze
                return src.Where(p =>
                    wraps
                        ? p.NormPos >= winStart || p.NormPos <= winEnd
                        : p.NormPos >= winStart && p.NormPos <= winEnd
                ).OrderBy(p =>
                {
                    // Sortierung relativ zu winStart (damit wraparound korrekt sortiert)
                    float rel = (p.NormPos - winStart + 1f) % 1f;
                    return rel;
                }).ToList();
            }

            var visibleBest    = WindowPts(fullTrack);
            var visibleCurrent = WindowPts(current.Count > 4 ? current : new List<TrackPoint>());

            // Bounding Box NUR aus sichtbaren Best-Lap-Punkten
            var bboxPts = visibleBest.Count > 3 ? visibleBest : visibleCurrent;
            if (bboxPts.Count < 2) return;

            float minX = bboxPts.Min(p => p.X), maxX = bboxPts.Max(p => p.X);
            float minZ = bboxPts.Min(p => p.Z), maxZ = bboxPts.Max(p => p.Z);
            float rx = maxX - minX, rz = maxZ - minZ;

            // Mindestgröße damit bei Geraden nicht überzoomt wird
            const float minRange = 80f; // Meter
            if (rx < minRange) { float d = (minRange - rx) / 2; minX -= d; maxX += d; rx = minRange; }
            if (rz < minRange) { float d = (minRange - rz) / 2; minZ -= d; maxZ += d; rz = minRange; }

            const double pad = 18;
            double scale = Math.Min((cw - pad * 2) / rx, (ch - pad * 2) / rz);

            Point ToC(float x, float z) => new(pad + (x - minX) * scale, pad + (z - minZ) * scale);

            // ── Gold: Best-Lap-Spur im Fenster ───────────────────────────────
            if (visibleBest.Count > 1)
            {
                var geo = new StreamGeometry();
                using (var ctx = geo.Open())
                {
                    ctx.BeginFigure(ToC(visibleBest[0].X, visibleBest[0].Z), false, false);
                    ctx.PolyLineTo(visibleBest.Skip(1).Select(p => ToC(p.X, p.Z)).ToList(), true, false);
                }
                geo.Freeze();
                TrackCanvas.Children.Add(new System.Windows.Shapes.Path
                {
                    Data = geo, Stroke = BrushBestLap, StrokeThickness = 6,
                    StrokeLineJoin = PenLineJoin.Round,
                    StrokeStartLineCap = PenLineCap.Round,
                    StrokeEndLineCap   = PenLineCap.Round,
                });
            }

            // ── Farbige Spur: aktuelle Runde im Fenster ───────────────────────
            if (visibleCurrent.Count > 1)
            {
                for (int i = 1; i < visibleCurrent.Count; i++)
                {
                    var a = visibleCurrent[i - 1];
                    var b = visibleCurrent[i];
                    Color c = b.Brake > 0.05f
                        ? Color.FromArgb(220, 255, 60,  60)
                        : b.Gas > 0.1f
                            ? Color.FromArgb(200, 34,  255, 100)
                            : Color.FromArgb(150, 0,   191, 255);
                    var br = new SolidColorBrush(c); br.Freeze();
                    TrackCanvas.Children.Add(new Line
                    {
                        X1 = ToC(a.X, a.Z).X, Y1 = ToC(a.X, a.Z).Y,
                        X2 = ToC(b.X, b.Z).X, Y2 = ToC(b.X, b.Z).Y,
                        Stroke = br, StrokeThickness = 3,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap   = PenLineCap.Round,
                    });
                }
            }

            // ── Auto-Dot ──────────────────────────────────────────────────────
            var cp = ToC(carX, carZ);
            TrackCanvas.Children.Add(new Ellipse
            {
                Width = 11, Height = 11, Fill = BrushCar,
                Effect = new System.Windows.Media.Effects.DropShadowEffect
                    { Color = Colors.Gold, BlurRadius = 12, ShadowDepth = 0 },
                Margin = new Thickness(cp.X - 5.5, cp.Y - 5.5, 0, 0)
            });
        }
    }
}
