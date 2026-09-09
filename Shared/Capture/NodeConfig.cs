using System;
using System.Collections.Generic;
using System.IO;
using Substation.Shared;

namespace Substation.Capture
{
    /// <summary>
    /// Settings both nodes share. The server and client each add their own on
    /// top of these.
    /// </summary>
    public abstract class NodeConfig
    {
        /// <summary>Identity used on the wire and in the column map: "S132" or "S33".</summary>
        public string NodeId = "";

        public string DisplayName = "";

        /// <summary>Shared secret for the socket link. Must match on both nodes.</summary>
        public string SharedSecret = "";

        public string RoiConfigPath = "";
        public string TessDataPath = @"C:\Users\DCS_User\AppData\Local\Programs\Tesseract-OCR\tessdata";

        /// <summary>
        /// Which display carries the SCADA wall view.
        ///
        /// "secondary" is the default and what these servers use: the wall view
        /// lives on the second head while the primary screen is the operator's
        /// desktop. "primary" reads the main screen. A plain number picks that
        /// index out of Screen.AllScreens when a server drives more than two.
        /// </summary>
        public string CaptureScreen = "secondary";

        /// <summary>Minute past each hour at which the screen is read.</summary>
        public int CaptureMinute = SessionClock.DefaultCaptureMinute;

        public string DataFolder = "Data";
        public string LogFolder = "Logs";
        public int RetentionDays = 120;

        /// <summary>
        /// The nodes run with no window and no tray icon — set this true only
        /// while commissioning, when someone needs to force a reading or open
        /// the calibration overlay without a remote desktop session.
        /// </summary>
        public bool ShowTrayIcon = false;

        /// <summary>Quit the process when the 07:00 window closes, instead of
        /// going idle. Idle keeps the server's QR hotkey usable at 07:00, which
        /// is exactly when the day is read, so this ships off.</summary>
        public bool ExitWhenSessionEnds = false;

        /// <summary>Keep the full screen capture on this node's disk. Large;
        /// useful during commissioning only.</summary>
        public bool KeepFullScreenshots = false;

        public string BaseFolder { get; protected set; }

        public string ResolvePath(string relative)
        {
            if (string.IsNullOrEmpty(relative)) return BaseFolder;
            return Path.IsPathRooted(relative) ? relative : Path.Combine(BaseFolder, relative);
        }

        protected void ReadCommon(Dictionary<string, object> root, string path)
        {
            BaseFolder = Path.GetDirectoryName(Path.GetFullPath(path));

            NodeId = Json.Str(root, "nodeId", NodeId);
            DisplayName = Json.Str(root, "displayName", DisplayName);
            SharedSecret = Json.Str(root, "sharedSecret", SharedSecret);
            RoiConfigPath = Json.Str(root, "roiConfigPath", RoiConfigPath);
            TessDataPath = Json.Str(root, "tessDataPath", TessDataPath);
            CaptureScreen = Json.Str(root, "captureScreen", CaptureScreen);
            CaptureMinute = Math.Max(0, Math.Min(59, Json.Int(root, "captureMinute", CaptureMinute)));
            DataFolder = Json.Str(root, "dataFolder", DataFolder);
            LogFolder = Json.Str(root, "logFolder", LogFolder);
            RetentionDays = Json.Int(root, "retentionDays", RetentionDays);
            ShowTrayIcon = Json.Bool(root, "showTrayIcon", ShowTrayIcon);
            ExitWhenSessionEnds = Json.Bool(root, "exitWhenSessionEnds", ExitWhenSessionEnds);
            KeepFullScreenshots = Json.Bool(root, "keepFullScreenshots", KeepFullScreenshots);
        }

        protected void WriteCommon(Dictionary<string, object> root)
        {
            root["nodeId"] = NodeId;
            root["displayName"] = DisplayName;
            root["sharedSecret"] = SharedSecret;
            root["roiConfigPath"] = RoiConfigPath;
            root["tessDataPath"] = TessDataPath;
            root["captureScreen"] = CaptureScreen;
            root["captureMinute"] = CaptureMinute;
            root["dataFolder"] = DataFolder;
            root["logFolder"] = LogFolder;
            root["retentionDays"] = RetentionDays;
            root["showTrayIcon"] = ShowTrayIcon;
            root["exitWhenSessionEnds"] = ExitWhenSessionEnds;
            root["keepFullScreenshots"] = KeepFullScreenshots;
        }

        protected void SetBaseFolder(string path)
        {
            BaseFolder = Path.GetDirectoryName(Path.GetFullPath(path));
        }
    }
}
