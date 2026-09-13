using System;
using System.Collections.Generic;
using System.IO;
using Substation.Shared;

namespace Substation.Capture
{
    /// <summary>
    /// node.config.json — everything one PC's node needs to know.
    ///
    /// The same program runs on the 132 kV and the 33 kV PC, and this file is
    /// the only thing that makes it one or the other: which boxes it reads
    /// (roiConfigPath) and which of the 24 QR columns it fills (the columns whose
    /// source is its nodeId).
    /// </summary>
    public sealed class NodeConfig
    {
        public const string FileName = "node.config.json";

        /// <summary>"S132" or "S33". Names this node's stored hours and picks its
        /// own columns out of the column map.</summary>
        public string NodeId = "";

        public string DisplayName = "";

        public string RoiConfigPath = "";

        /// <summary>
        /// Where Tesseract's eng.traineddata lives. This is the machine-wide
        /// installer's folder; a node that finds nothing there falls back to the
        /// other usual locations rather than failing every capture, so it only
        /// needs setting when Tesseract is somewhere unusual. See <see cref="TessData"/>.
        /// </summary>
        public string TessDataPath = @"C:\Program Files\Tesseract-OCR\tessdata";

        /// <summary>
        /// Which display carries the SCADA wall view.
        ///
        /// "secondary" is the default and what these PCs use: the wall view
        /// lives on the second head while the primary screen is the operator's
        /// desktop. "primary" reads the main screen. A plain number picks that
        /// index out of Screen.AllScreens when a PC drives more than two.
        /// </summary>
        public string CaptureScreen = "secondary";

        /// <summary>Minute past each hour at which the screen is read.</summary>
        public int CaptureMinute = SessionClock.DefaultCaptureMinute;

        public string DataFolder = "Data";
        public string LogFolder = "Logs";
        public int RetentionDays = 120;

        /// <summary>
        /// The node runs with no window and no tray icon — set this true while
        /// commissioning, when someone needs to force a reading or check the
        /// boxes, or when another program holds the QR hotkey and the code has
        /// to be shown from the tray instead.
        /// </summary>
        public bool ShowTrayIcon = false;

        /// <summary>
        /// Start this node when the SCADA account logs on. Covers a reboot; it
        /// does not restart the daily session, which stays a manual act.
        /// </summary>
        public bool RunAtLogon = false;

        /// <summary>Quit the process when the 07:00 window closes, instead of
        /// going idle. Idle keeps the QR hotkey usable at 07:00, which is exactly
        /// when the day is read, so this ships off.</summary>
        public bool ExitWhenSessionEnds = false;

        /// <summary>Keep the full screen capture on this PC's disk. Large;
        /// useful during commissioning only.</summary>
        public bool KeepFullScreenshots = false;

        /// <summary>How long the QR code stays up. Clamped to 3-5 seconds.</summary>
        public int QrSeconds = 5;

        /// <summary>Which display shows the code: "primary" (the operator's own
        /// screen, and the default), "secondary", or a screen index.</summary>
        public string QrScreen = "primary";

        /// <summary>Stamped into nothing today — the LS1 payload the companion
        /// app parses carries no substation field — but it labels the QR caption
        /// so an operator can tell two substations apart at a glance.</summary>
        public string SubstationCode = "MSL-E";

        /// <summary>
        /// Channel-to-column bindings for the QR payload. Only the bindings whose
        /// source is <see cref="NodeId"/> are filled on this PC; the rest belong
        /// to the other PC, whose code the phone merges in.
        /// </summary>
        public ColumnMap Columns = ColumnMap.Default();

        public string BaseFolder { get; private set; }

        public string ResolvePath(string relative)
        {
            if (string.IsNullOrEmpty(relative)) return BaseFolder;
            return Path.IsPathRooted(relative) ? relative : Path.Combine(BaseFolder, relative);
        }

        /// <summary>
        /// Reads the file. There is deliberately no default written when it is
        /// missing: a default would have to be one of the two PCs, and a 33 kV
        /// PC quietly running as 132 kV reads the wrong boxes all night.
        /// </summary>
        public static NodeConfig Load(string path)
        {
            if (!File.Exists(path))
                throw new FileNotFoundException(
                    "The file does not exist. On a new PC, copy the config for this PC out of the " +
                    "Config folder, put it next to the program and rename it " + FileName + ":\n\n" +
                    "    132 kV PC:   Config\\node-132kv.config.json\n" +
                    "    33 kV PC:    Config\\node-33kv.config.json", path);

            var root = Json.ReadFile(path);
            var config = new NodeConfig { BaseFolder = Path.GetDirectoryName(Path.GetFullPath(path)) };

            config.NodeId = Json.Str(root, "nodeId", config.NodeId).Trim();
            config.DisplayName = Json.Str(root, "displayName", config.DisplayName);
            config.RoiConfigPath = Json.Str(root, "roiConfigPath", config.RoiConfigPath);
            config.TessDataPath = Json.Str(root, "tessDataPath", config.TessDataPath);
            config.CaptureScreen = Json.Str(root, "captureScreen", config.CaptureScreen);
            config.CaptureMinute = Math.Max(0, Math.Min(59, Json.Int(root, "captureMinute", config.CaptureMinute)));
            config.DataFolder = Json.Str(root, "dataFolder", config.DataFolder);
            config.LogFolder = Json.Str(root, "logFolder", config.LogFolder);
            config.RetentionDays = Json.Int(root, "retentionDays", config.RetentionDays);
            config.ShowTrayIcon = Json.Bool(root, "showTrayIcon", config.ShowTrayIcon);
            config.RunAtLogon = Json.Bool(root, "runAtLogon", config.RunAtLogon);
            config.ExitWhenSessionEnds = Json.Bool(root, "exitWhenSessionEnds", config.ExitWhenSessionEnds);
            config.KeepFullScreenshots = Json.Bool(root, "keepFullScreenshots", config.KeepFullScreenshots);

            // 3 to 5 seconds on screen, as specified.
            config.QrSeconds = Math.Max(3, Math.Min(5, Json.Int(root, "qrSeconds", config.QrSeconds)));
            config.QrScreen = Json.Str(root, "qrScreen", config.QrScreen);
            config.SubstationCode = Json.Str(root, "substationCode", config.SubstationCode);
            config.Columns = ColumnMap.FromJson(Json.List(root, "columns"));

            if (config.NodeId.Length == 0)
                throw new InvalidDataException(
                    "\"nodeId\" is empty. Set it to \"S132\" on the 132 kV PC or \"S33\" on the 33 kV PC.");

            if (string.IsNullOrWhiteSpace(config.RoiConfigPath))
                throw new InvalidDataException(
                    "\"roiConfigPath\" is empty. It names this PC's box file, e.g. Config\\roi-33kv.json.");

            return config;
        }
    }
}
