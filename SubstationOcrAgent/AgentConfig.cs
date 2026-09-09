using System;
using System.Collections.Generic;
using System.IO;
using Substation.Shared;

namespace SubstationOcrAgent
{
    /// <summary>
    /// agent.config.json, read once at start-up and re-read on demand from the
    /// tray menu so a coordinate fix does not need a restart.
    /// </summary>
    public sealed class AgentConfig
    {
        /// <summary>Identity used by the column map: "S132" or "S33".</summary>
        public string ServerId = "S132";

        public string DisplayName = "132 kV SWITCHGEAR SLD WALL VIEW";

        /// <summary>Interface to bind. Keep it on the substation LAN address —
        /// 0.0.0.0 exposes the agent on every interface of the server.</summary>
        public string ListenAddress = "0.0.0.0";
        public int Port = AgentProtocol.DefaultPort;

        /// <summary>Shared secret. Empty disables authentication entirely; the
        /// agent logs a warning at start-up when that is the case.</summary>
        public string SharedSecret = "";

        /// <summary>Hosts allowed to connect. Empty means any host may connect.</summary>
        public List<string> AllowedClients = new List<string>();

        public string RoiConfigPath = "Config\\roi-132kv.json";
        public string TessDataPath = @"C:\Users\DCS_User\AppData\Local\Programs\Tesseract-OCR\tessdata";

        /// <summary>0-based monitor index. Servers with the wall view on a second
        /// head set this to 1.</summary>
        public int MonitorIndex = 0;

        /// <summary>Minute of each hour at which the agent reads its display.</summary>
        public int CaptureMinute = 2;

        public bool AutoCaptureEnabled = true;

        /// <summary>Logsheet workday start. Hours 8..24 belong to the session
        /// date, hours 1..7 to the morning after.</summary>
        public int WorkdayStartHour = 8;

        public string DataFolder = "Data";
        public string LogFolder = "Logs";
        public int RetentionDays = 120;

        /// <summary>
        /// Off by default: the hourly path sends numbers only. Turning this on
        /// lets WinLogosheet pull a single ROI crop when an operator presses
        /// "Show evidence" while chasing a bad reading.
        /// </summary>
        public bool AllowRoiImages = false;

        /// <summary>Keep the full screenshot on the agent host after a capture.
        /// Useful during commissioning, wasteful afterwards.</summary>
        public bool KeepFullScreenshots = false;

        public string BaseFolder { get; private set; }

        public string ResolvePath(string relative)
        {
            if (string.IsNullOrEmpty(relative)) return BaseFolder;
            return Path.IsPathRooted(relative) ? relative : Path.Combine(BaseFolder, relative);
        }

        public static AgentConfig Load(string path)
        {
            var cfg = new AgentConfig
            {
                BaseFolder = Path.GetDirectoryName(Path.GetFullPath(path))
            };

            if (!File.Exists(path))
            {
                cfg.Save(path);
                return cfg;
            }

            var root = Json.ReadFile(path);
            cfg.ServerId = Json.Str(root, "serverId", cfg.ServerId);
            cfg.DisplayName = Json.Str(root, "displayName", cfg.DisplayName);
            cfg.ListenAddress = Json.Str(root, "listenAddress", cfg.ListenAddress);
            cfg.Port = Json.Int(root, "port", cfg.Port);
            cfg.SharedSecret = Json.Str(root, "sharedSecret", cfg.SharedSecret);
            cfg.AllowedClients = Json.Strings(root, "allowedClients");
            cfg.RoiConfigPath = Json.Str(root, "roiConfigPath", cfg.RoiConfigPath);
            cfg.TessDataPath = Json.Str(root, "tessDataPath", cfg.TessDataPath);
            cfg.MonitorIndex = Json.Int(root, "monitorIndex", cfg.MonitorIndex);
            cfg.CaptureMinute = Math.Max(0, Math.Min(59, Json.Int(root, "captureMinute", cfg.CaptureMinute)));
            cfg.AutoCaptureEnabled = Json.Bool(root, "autoCaptureEnabled", cfg.AutoCaptureEnabled);
            cfg.WorkdayStartHour = Math.Max(0, Math.Min(23, Json.Int(root, "workdayStartHour", cfg.WorkdayStartHour)));
            cfg.DataFolder = Json.Str(root, "dataFolder", cfg.DataFolder);
            cfg.LogFolder = Json.Str(root, "logFolder", cfg.LogFolder);
            cfg.RetentionDays = Json.Int(root, "retentionDays", cfg.RetentionDays);
            cfg.AllowRoiImages = Json.Bool(root, "allowRoiImages", cfg.AllowRoiImages);
            cfg.KeepFullScreenshots = Json.Bool(root, "keepFullScreenshots", cfg.KeepFullScreenshots);
            return cfg;
        }

        public void Save(string path)
        {
            Json.WriteFile(path, new Dictionary<string, object>
            {
                { "serverId", ServerId },
                { "displayName", DisplayName },
                { "listenAddress", ListenAddress },
                { "port", Port },
                { "sharedSecret", SharedSecret },
                { "allowedClients", AllowedClients },
                { "roiConfigPath", RoiConfigPath },
                { "tessDataPath", TessDataPath },
                { "monitorIndex", MonitorIndex },
                { "captureMinute", CaptureMinute },
                { "autoCaptureEnabled", AutoCaptureEnabled },
                { "workdayStartHour", WorkdayStartHour },
                { "dataFolder", DataFolder },
                { "logFolder", LogFolder },
                { "retentionDays", RetentionDays },
                { "allowRoiImages", AllowRoiImages },
                { "keepFullScreenshots", KeepFullScreenshots }
            });
        }
    }
}
