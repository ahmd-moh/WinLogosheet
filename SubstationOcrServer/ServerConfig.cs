using System;
using System.Collections.Generic;
using System.IO;
using Substation.Capture;
using Substation.Shared;

namespace SubstationOcrServer
{
    /// <summary>server.config.json — the 132 kV node.</summary>
    public sealed class ServerConfig : NodeConfig
    {
        /// <summary>Interface to bind. Keep it on the substation LAN address —
        /// 0.0.0.0 exposes the node on every interface of the server.</summary>
        public string ListenAddress = "0.0.0.0";
        public int Port = AgentProtocol.DefaultPort;

        /// <summary>Hosts allowed to connect. Empty means any host may.</summary>
        public List<string> AllowedClients = new List<string>();

        /// <summary>How long the QR code stays up. Clamped to 3-5 seconds.</summary>
        public int QrSeconds = 5;

        /// <summary>Which display shows the code: "primary" (the operator's own
        /// screen, and the default), "secondary", or a screen index.</summary>
        public string QrScreen = "primary";

        /// <summary>Stamped into nothing today — the LS1 payload the companion
        /// app parses carries no substation field — but it labels the window so
        /// an operator can tell two substations apart at a glance.</summary>
        public string SubstationCode = "MSL-E";

        /// <summary>Channel-to-column bindings for the QR payload.</summary>
        public ColumnMap Columns = ColumnMap.Default();

        /// <summary>
        /// nodeId of the client node, for the things this node asks of it —
        /// today, a calibration. It has to match nodeId in client.config.json:
        /// a request addressed to a name nothing answers to simply waits until
        /// it expires.
        /// </summary>
        public string ClientNodeId = "S33";

        /// <summary>
        /// Widest calibration picture to ask the client node for. A wall view
        /// narrowed to 1920 px still shows whether a box sits on its panel, and
        /// it keeps the answer inside the protocol's line limit on the large
        /// displays some substations run. 0 asks for the full resolution.
        /// </summary>
        public int CalibrationMaxWidth = 1920;

        public ServerConfig()
        {
            NodeId = "S132";
            DisplayName = "132 kV SWITCHGEAR SLD WALL VIEW";
            RoiConfigPath = "Config\\roi-132kv.json";
        }

        public static ServerConfig Load(string path)
        {
            var config = new ServerConfig();

            if (!File.Exists(path))
            {
                config.SetBaseFolder(path);
                config.Save(path);
                return config;
            }

            var root = Json.ReadFile(path);
            config.ReadCommon(root, path);

            config.ListenAddress = Json.Str(root, "listenAddress", config.ListenAddress);
            config.Port = Json.Int(root, "port", config.Port);
            config.AllowedClients = Json.Strings(root, "allowedClients");
            config.SubstationCode = Json.Str(root, "substationCode", config.SubstationCode);
            config.ClientNodeId = Json.Str(root, "clientNodeId", config.ClientNodeId);
            config.CalibrationMaxWidth = Math.Max(0, Json.Int(root, "calibrationMaxWidth", config.CalibrationMaxWidth));

            // 3 to 5 seconds on screen, as specified.
            config.QrSeconds = Math.Max(3, Math.Min(5, Json.Int(root, "qrSeconds", config.QrSeconds)));
            config.QrScreen = Json.Str(root, "qrScreen", config.QrScreen);

            config.Columns = ColumnMap.FromJson(Json.List(root, "columns"));
            return config;
        }

        public void Save(string path)
        {
            var root = new Dictionary<string, object>
            {
                { "_readme", "132 kV SERVER node. It reads its own secondary screen every hour, " +
                             "receives the 33 kV client's values over TCP, and shows the LS1 QR " +
                             "code on the main screen when the operator presses Ctrl+Shift+7, 8, 9. " +
                             "sharedSecret must match the client's." }
            };
            WriteCommon(root);

            root["listenAddress"] = ListenAddress;
            root["port"] = Port;
            root["allowedClients"] = AllowedClients;
            root["substationCode"] = SubstationCode;
            root["clientNodeId"] = ClientNodeId;
            root["calibrationMaxWidth"] = CalibrationMaxWidth;
            root["qrSeconds"] = QrSeconds;
            root["qrScreen"] = QrScreen;
            root["columns"] = Columns.ToJson();

            Json.WriteFile(path, root);
        }
    }
}
