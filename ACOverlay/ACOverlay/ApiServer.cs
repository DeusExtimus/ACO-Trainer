using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace ACOverlay
{
    public static class ApiServer
    {
        static HttpListener? _listener;
        static Thread?       _thread;

        public static void Start(int port = 54321)
        {
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();
            _thread = new Thread(Loop) { IsBackground = true, Name = "ApiServer" };
            _thread.Start();
        }

        static void Loop()
        {
            while (_listener?.IsListening == true)
            {
                try
                {
                    var ctx = _listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => Handle(ctx));
                }
                catch { /* listener stopped */ }
            }
        }

        static void Handle(HttpListenerContext ctx)
        {
            var req  = ctx.Request;
            var resp = ctx.Response;

            // CORS für localhost
            resp.Headers.Add("Access-Control-Allow-Origin", "*");
            resp.Headers.Add("Access-Control-Allow-Methods", "GET, POST, OPTIONS");
            resp.Headers.Add("Access-Control-Allow-Headers", "*");
            resp.ContentType = "application/json; charset=utf-8";

            if (req.HttpMethod == "OPTIONS")
            {
                resp.StatusCode = 204;
                resp.Close();
                return;
            }

            string path = req.Url?.AbsolutePath ?? "/";
            string body = "";

            try
            {
                body = path switch
                {
                    // GET /status — ist ACOverlay verbunden?
                    "/status" => JsonSerializer.Serialize(new
                    {
                        connected = SharedState.IsConnected,
                        track     = SharedState.Track,
                        car       = SharedState.Car,
                        lap       = SharedState.CurrentLap,
                        time      = SharedState.CurrentTime,
                    }),

                    // GET /combos — alle Track+Car Kombos dieser Session
                    "/combos" => JsonSerializer.Serialize(
                        SharedState.SessionLaps.Keys
                            .Select(k =>
                            {
                                var parts = k.Split("__", 2);
                                return new { key = k, track = parts[0], car = parts.Length > 1 ? parts[1] : "" };
                            })
                            .ToList()
                    ),

                    // GET /laps?key=track__car — alle Runden einer Kombo
                    "/laps" => HandleLaps(req),

                    // GET /best?key=track__car — gespeicherte beste Runde
                    "/best" => HandleBest(req),

                    // POST /save_theoretical — speichert Theoretical Best als neue beste Runde
                    "/save_theoretical" => HandleSaveTheoretical(req),

                    _ => "{\"error\":\"not found\"}"
                };

                if (path == "/save_theoretical" && req.HttpMethod == "POST")
                    body = HandleSaveTheoretical(req);
            }
            catch (Exception ex)
            {
                resp.StatusCode = 500;
                body = JsonSerializer.Serialize(new { error = ex.Message });
            }

            var bytes = Encoding.UTF8.GetBytes(body);
            resp.ContentLength64 = bytes.Length;
            resp.OutputStream.Write(bytes, 0, bytes.Length);
            resp.Close();
        }

        static string HandleLaps(HttpListenerRequest req)
        {
            string key = req.QueryString["key"] ?? "";
            if (!SharedState.SessionLaps.TryGetValue(key, out var laps))
                return "[]";

            return JsonSerializer.Serialize(laps.Select((lap, idx) => new
            {
                index      = idx,
                lapTimeMs  = lap.LapTimeMs,
                lapTimeStr = lap.LapTimeStr,
                recordedAt = lap.RecordedAt,
                points     = lap.Points.Select(p => new
                {
                    normPos = p.NormPos,
                    speed   = p.Speed,
                    gas     = p.Gas,
                    brake   = p.Brake,
                }).ToList()
            }).ToList());
        }

        static string HandleBest(HttpListenerRequest req)
        {
            string key    = req.QueryString["key"] ?? "";
            var parts     = key.Split("__", 2);
            string track  = parts[0];
            string car    = parts.Length > 1 ? parts[1] : "";
            var best      = LapStore.Load(track, car);
            if (best == null) return "null";

            return JsonSerializer.Serialize(new
            {
                lapTimeMs  = best.LapTimeMs,
                lapTimeStr = best.LapTimeStr,
                recordedAt = best.RecordedAt,
                points     = best.Points.Select(p => new
                {
                    normPos = p.NormPos,
                    speed   = p.Speed,
                    gas     = p.Gas,
                    brake   = p.Brake,
                }).ToList()
            });
        }

        static string HandleSaveTheoretical(HttpListenerRequest req)
        {
            using var reader = new System.IO.StreamReader(req.InputStream, Encoding.UTF8);
            string json = reader.ReadToEnd();

            var doc = JsonDocument.Parse(json);
            string key   = doc.RootElement.GetProperty("key").GetString() ?? "";
            var parts    = key.Split("__", 2);
            string track = parts[0];
            string car   = parts.Length > 1 ? parts[1] : "";

            var pointsEl = doc.RootElement.GetProperty("points");
            var points   = new List<TrackPoint>();
            foreach (var p in pointsEl.EnumerateArray())
            {
                points.Add(new TrackPoint(
                    X:       0,
                    Z:       0,
                    Gas:     p.GetProperty("gas").GetSingle(),
                    Brake:   p.GetProperty("brake").GetSingle(),
                    NormPos: p.GetProperty("normPos").GetSingle(),
                    Speed:   p.GetProperty("speed").GetSingle()
                ));
            }

            int lapTimeMs = doc.RootElement.GetProperty("lapTimeMs").GetInt32();
            var lapData = new LapData
            {
                LapTimeMs  = lapTimeMs,
                LapTimeStr = $"THEO {FormatMs(lapTimeMs)}",
                RecordedAt = DateTime.Now,
                Points     = points
            };
            LapStore.Save(lapData, track, car);

            // SharedState aktualisieren
            lock (SharedState.Lock)
            {
                SharedState.BestLapPoints = points;
                SharedState.BestLapMs     = lapTimeMs;
                SharedState.BestLapStr    = lapData.LapTimeStr;
            }

            return JsonSerializer.Serialize(new { ok = true, saved = lapData.LapTimeStr });
        }

        static string FormatMs(int ms)
        {
            var t = TimeSpan.FromMilliseconds(ms);
            return $"{(int)t.TotalMinutes}:{t.Seconds:D2}.{t.Milliseconds:D3}";
        }
    }
}
