using System.Collections.Generic;

namespace ACOverlay
{
    /// <summary>
    /// Statischer Datenspeicher, auf den beide Fenster zugreifen.
    /// Wird nur vom MainWindow geschrieben, vom MapWindow gelesen.
    /// </summary>
    public static class SharedState
    {
        public static readonly object Lock = new();

        // Strecke
        public static List<TrackPoint> CurrentLapPoints { get; } = new();
        public static List<TrackPoint> BestLapPoints    { get; set; } = new();

        // Fahrzeugposition
        public static float CarX      { get; set; }
        public static float CarZ      { get; set; }
        public static float CarNormPos { get; set; }

        // Info
        public static int   BestLapMs  { get; set; } = int.MaxValue;
        public static string BestLapStr  { get; set; } = "";
        public static string Track       { get; set; } = "";
        public static string Car         { get; set; } = "";

        // Temporär: alle Runden dieser Session pro Track+Car
        public static Dictionary<string, List<LapData>> SessionLaps { get; set; } = new();
        public static int   CurrentLap  { get; set; }
        public static string CurrentTime { get; set; } = "—";
        public static float SpeedKmh   { get; set; }
        public static bool  IsConnected { get; set; }
    }
}
