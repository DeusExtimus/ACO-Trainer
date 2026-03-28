using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace ACOverlay
{
    public partial class MainWindow : Window
    {
        const int GWL_EXSTYLE       = -20;
        const int WS_EX_LAYERED     = 0x80000;
        const int WS_EX_TRANSPARENT = 0x20;

        [DllImport("user32.dll")] static extern int  GetWindowLong(IntPtr hwnd, int index);
        [DllImport("user32.dll")] static extern int  SetWindowLong(IntPtr hwnd, int index, int v);
        [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr hwnd, IntPtr after, int x, int y, int w, int h, uint f);

        static readonly IntPtr HWND_TOPMOST = new(-1);
        const uint SWP_NOMOVE = 0x0002, SWP_NOSIZE = 0x0001;

        readonly ACSharedMemoryReader _reader = new();
        readonly DispatcherTimer      _timer  = new();
        Thread? _connectThread;

        float _maxSpeed   = 50f;
        string _track     = "";
        string _car       = "";
        bool   _comboLoaded = false;

        readonly List<TrackPoint> _currentLapPoints = new();
        List<TrackPoint>          _bestLapPoints    = new();
        int   _lastLap     = -1;
        int   _bestLapMs   = int.MaxValue;
        float _lastNormPos = -1f;

        // Temporäre Rundenspeicherung (nur für aktuelle Programmlaufzeit)
        // Key = "track__car", Value = Liste aller Runden dieser Session
        readonly Dictionary<string, List<LapData>> _sessionLaps = new();

        const float GRAPH_BACK  = 0.08f;
        const float GRAPH_AHEAD = 0.25f;

        static readonly Brush BrushSpeed     = new SolidColorBrush(Color.FromArgb(230, 34,  255, 102));
        static readonly Brush BrushBestSpeed = new SolidColorBrush(Color.FromArgb(180, 255, 255, 255));
        static readonly Brush BrushGrid      = new SolidColorBrush(Color.FromArgb(20,  255, 255, 255));

        static MainWindow()
        {
            BrushSpeed.Freeze();
            BrushBestSpeed.Freeze();
            BrushGrid.Freeze();
        }

        public MainWindow() { InitializeComponent(); }

        void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Position: links unten
            var screen = SystemParameters.WorkArea;
            Left = screen.Left + 10;
            Top  = screen.Bottom - Height - 10;

            MakeClickThrough();

            var map = new MapWindow();
            map.Show();

            _connectThread = new Thread(ConnectLoop) { IsBackground = true };
            _connectThread.Start();

            _timer.Interval = TimeSpan.FromMilliseconds(33);
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        void MakeClickThrough()
        {
            var hwnd = new System.Windows.Interop.WindowInteropHelper(this).Handle;
            SetWindowLong(hwnd, GWL_EXSTYLE,
                GetWindowLong(hwnd, GWL_EXSTYLE) | WS_EX_LAYERED | WS_EX_TRANSPARENT);
            SetWindowPos(hwnd, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
        }

        void ConnectLoop()
        {
            while (true)
            {
                if (!_reader.IsOpen)
                {
                    _reader.TryReopen();
                    SharedState.IsConnected = _reader.IsOpen;
                }
                Thread.Sleep(2000);
            }
        }

        void Timer_Tick(object? sender, EventArgs e)
        {
            if (!_reader.IsOpen) return;

            SPageFilePhysics phys;
            SPageFileGraphic gfx;
            SPageFileStatic  stat;
            try
            {
                phys = _reader.ReadPhysics();
                gfx  = _reader.ReadGraphics();
                stat = _reader.ReadStatic();
            }
            catch { _reader.TryReopen(); return; }

            // Track+Car laden
            string track = stat.track    ?? "";
            string car   = stat.carModel ?? "";
            if (!_comboLoaded && track.Length > 0 && car.Length > 0)
            {
                _track = track; _car = car; _comboLoaded = true;

                var saved = LapStore.Load(_track, _car);
                if (saved != null)
                {
                    _bestLapMs     = saved.LapTimeMs;
                    _bestLapPoints = saved.Points;
                    lock (SharedState.Lock)
                    {
                        SharedState.BestLapPoints = _bestLapPoints;
                        SharedState.BestLapMs     = _bestLapMs;
                        SharedState.BestLapStr    = saved.LapTimeStr;
                    }
                }
                lock (SharedState.Lock) { SharedState.Track = _track; SharedState.Car = _car; }
            }

            if (phys.speedKmh > _maxSpeed) _maxSpeed = phys.speedKmh + 20f;

            UpdateLapTracking(phys, gfx);
            UpdateSharedState(phys, gfx);
            DrawGraph();
        }

        void UpdateSharedState(SPageFilePhysics p, SPageFileGraphic gfx)
        {
            lock (SharedState.Lock)
            {
                SharedState.CurrentLapPoints.Clear();
                SharedState.CurrentLapPoints.AddRange(_currentLapPoints);
                SharedState.CarX        = gfx.carCoordinates_X;
                SharedState.CarZ        = gfx.carCoordinates_Z;
                SharedState.CarNormPos  = gfx.normalizedCarPosition;
                SharedState.CurrentLap  = gfx.completedLaps + 1;
                SharedState.CurrentTime = gfx.currentTime ?? "—";
                SharedState.SpeedKmh    = p.speedKmh;
                SharedState.IsConnected = true;
            }
        }

        void UpdateLapTracking(SPageFilePhysics p, SPageFileGraphic gfx)
        {
            float normPos = gfx.normalizedCarPosition;
            if (Math.Abs(normPos - _lastNormPos) > 0.001f || _currentLapPoints.Count == 0)
            {
                _currentLapPoints.Add(new TrackPoint(
                    gfx.carCoordinates_X, gfx.carCoordinates_Z,
                    p.gas, p.brake, normPos, p.speedKmh));
                _lastNormPos = normPos;
            }
            if (_currentLapPoints.Count > 2000) _currentLapPoints.RemoveAt(0);

            if (gfx.completedLaps != _lastLap && _lastLap >= 0)
            {
                int ms = gfx.iLastTime;
                if (ms > 0)
                {
                    // Temporär speichern (Session, nicht auf Disk)
                    string key = $"{_track}__{_car}";
                    if (!_sessionLaps.ContainsKey(key))
                        _sessionLaps[key] = new List<LapData>();
                    _sessionLaps[key].Add(new LapData
                    {
                        LapTimeMs  = ms,
                        LapTimeStr = FormatMs(ms),
                        RecordedAt = DateTime.Now,
                        Points     = new List<TrackPoint>(_currentLapPoints)
                    });
                    SharedState.SessionLaps = _sessionLaps;

                    // Beste Runde persistent speichern
                    if (ms < _bestLapMs)
                    {
                        _bestLapMs     = ms;
                        _bestLapPoints = new List<TrackPoint>(_currentLapPoints);
                        var lapData = new LapData
                        {
                            LapTimeMs  = ms,
                            LapTimeStr = FormatMs(ms),
                            RecordedAt = DateTime.Now,
                            Points     = _bestLapPoints
                        };
                        LapStore.Save(lapData, _track, _car);
                        lock (SharedState.Lock)
                        {
                            SharedState.BestLapPoints = _bestLapPoints;
                            SharedState.BestLapMs     = ms;
                            SharedState.BestLapStr    = lapData.LapTimeStr;
                        }
                    }
                }
                _currentLapPoints.Clear();
            }
            _lastLap = gfx.completedLaps;
        }

        void DrawGraph()
        {
            double w = ComboCanvas.ActualWidth;
            double h = ComboCanvas.ActualHeight;
            if (w < 10 || h < 10) return;
            ComboCanvas.Children.Clear();

            float currentNormPos = SharedState.CarNormPos;

            float scaleMax = _maxSpeed < 10 ? 10 : _maxSpeed;
            if (_bestLapPoints.Count > 5)
            {
                float bm = _bestLapPoints.Max(p => p.Speed);
                if (bm > scaleMax) scaleMax = bm;
            }

            double SpeedToY(float kmh) =>
                h - Math.Clamp(kmh / scaleMax, 0, 1) * (h - 4) - 2;

            double RelDist(float normPos)
            {
                double d = normPos - currentNormPos;
                if (d >  0.5) d -= 1.0;
                if (d < -0.5) d += 1.0;
                return d;
            }

            double RelToX(double rel) =>
                w * (rel + GRAPH_BACK) / (GRAPH_BACK + GRAPH_AHEAD);

            // Gridlinien
            foreach (double pct in new[] { 0.25, 0.5, 0.75 })
                ComboCanvas.Children.Add(new Line
                {
                    X1 = 0, X2 = w, Y1 = h - pct * h, Y2 = h - pct * h,
                    Stroke = BrushGrid, StrokeThickness = 0.5
                });

            double nowX = RelToX(0);
            ComboCanvas.Children.Add(new Line
            {
                X1 = nowX, X2 = nowX, Y1 = 0, Y2 = h,
                Stroke = new SolidColorBrush(Color.FromArgb(40, 255, 255, 255)),
                StrokeThickness = 1
            });

            // Weiß gestrichelt: beste Runde
            if (_bestLapPoints.Count > 5 && _bestLapPoints.Max(p => p.Speed) > 1)
            {
                var visible = _bestLapPoints
                    .Select(p => (rel: RelDist(p.NormPos), speed: p.Speed))
                    .Where(t => t.rel >= -GRAPH_BACK && t.rel <= GRAPH_AHEAD)
                    .OrderBy(t => t.rel)
                    .ToList();

                if (visible.Count > 1)
                {
                    var geo = new StreamGeometry();
                    using (var ctx = geo.Open())
                    {
                        ctx.BeginFigure(new Point(RelToX(visible[0].rel), SpeedToY(visible[0].speed)), false, false);
                        ctx.PolyLineTo(visible.Skip(1)
                            .Select(t => new Point(RelToX(t.rel), SpeedToY(t.speed)))
                            .ToList(), true, false);
                    }
                    geo.Freeze();
                    ComboCanvas.Children.Add(new System.Windows.Shapes.Path
                    {
                        Data = geo, Stroke = BrushBestSpeed, StrokeThickness = 1.5,
                        StrokeLineJoin = PenLineJoin.Round,
                        StrokeDashArray = new DoubleCollection { 5, 4 }
                    });
                }
            }

            // Grün: aktuelle Runde
            if (_currentLapPoints.Count > 5)
            {
                var visible = _currentLapPoints
                    .Select(p => (rel: RelDist(p.NormPos), speed: p.Speed))
                    .Where(t => t.rel >= -GRAPH_BACK && t.rel <= GRAPH_AHEAD)
                    .OrderBy(t => t.rel)
                    .ToList();

                if (visible.Count > 1)
                {
                    var geo = new StreamGeometry();
                    using (var ctx = geo.Open())
                    {
                        ctx.BeginFigure(new Point(RelToX(visible[0].rel), SpeedToY(visible[0].speed)), false, false);
                        ctx.PolyLineTo(visible.Skip(1)
                            .Select(t => new Point(RelToX(t.rel), SpeedToY(t.speed)))
                            .ToList(), true, false);
                    }
                    geo.Freeze();
                    ComboCanvas.Children.Add(new System.Windows.Shapes.Path
                    {
                        Data = geo, Stroke = BrushSpeed, StrokeThickness = 2,
                        StrokeLineJoin = PenLineJoin.Round,
                        StrokeStartLineCap = PenLineCap.Round,
                        StrokeEndLineCap   = PenLineCap.Round,
                    });

                    double cy = SpeedToY(_currentLapPoints.Last().Speed);
                    ComboCanvas.Children.Add(new Ellipse
                    {
                        Width = 7, Height = 7, Fill = BrushSpeed,
                        Margin = new Thickness(nowX - 3.5, cy - 3.5, 0, 0)
                    });
                }
            }
        }

        static string FormatMs(int ms)
        {
            var t = TimeSpan.FromMilliseconds(ms);
            return $"{(int)t.TotalMinutes}:{t.Seconds:D2}.{t.Milliseconds:D3}";
        }
    }
}
