using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;

namespace ACOverlay
{
    public record TrackPoint(float X, float Z, float Gas, float Brake, float NormPos, float Speed = 0);

    public class LapData
    {
        public int      LapTimeMs   { get; set; }
        public string   LapTimeStr  { get; set; } = "";
        public DateTime RecordedAt  { get; set; }
        public List<TrackPoint> Points { get; set; } = new();
    }

    public static class LapStore
    {
        private static readonly string SaveDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "ACOverlay");

        // Schlüssel: "track__car" — Sonderzeichen bereinigt
        private static string SafeKey(string track, string car)
        {
            static string Clean(string s) =>
                string.IsNullOrWhiteSpace(s) ? "unknown"
                : string.Concat(s.Select(c => char.IsLetterOrDigit(c) || c == '-' ? c : '_'));
            return $"{Clean(track)}__{Clean(car)}";
        }

        private static string FilePath(string track, string car) =>
            Path.Combine(SaveDir, $"best_{SafeKey(track, car)}.json");

        public static void Save(LapData lap, string track, string car)
        {
            Directory.CreateDirectory(SaveDir);
            var json = JsonSerializer.Serialize(lap, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(FilePath(track, car), json);
        }

        public static LapData? Load(string track, string car)
        {
            var path = FilePath(track, car);
            if (!File.Exists(path)) return null;
            try { return JsonSerializer.Deserialize<LapData>(File.ReadAllText(path)); }
            catch { return null; }
        }
    }
}
