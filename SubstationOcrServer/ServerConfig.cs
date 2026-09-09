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

        /// <summary>How long each QR code stays on the main screen. The brief
        /// clamps this to 3-5 seconds.</summary>
        public int QrSeconds = 5;

        public string SubstationCode = "MSL-E";
        public string QrFormat = "v2json";
        public string QrEcc = "L";
        public string QrUrlTemplate = "";

        /// <summary>Channel-to-column bindings for the QR payload.</summary>
        public ColumnMap Columns = ColumnMap.Default();

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

            // 3 to 5 seconds on screen, as specified.
            config.QrSeconds = Math.Max(3, Math.Min(5, Json.Int(root, "qrSeconds", config.QrSeconds)));

            var qr = Json.Dict(root, "qr");
            config.QrFormat = Json.Str(qr, "format", config.QrFormat);
            config.QrEcc = Json.Str(qr, "ecc", config.QrEcc);
            config.QrUrlTemplate = Json.Str(qr, "urlTemplate", config.QrUrlTemplate);

            config.Columns = ColumnMap.FromJson(Json.List(root, "columns"));
            return config;
        }

        public void Save(string path)
        {
            var root = new Dictionary<string, object>
            {
                { "_readme", "132 kV SERVER node. It reads its own secondary screen every hour, " +
                             "receives the 33 kV client's values over TCP, and flashes the QR code " +
                             "on the main screen on Ctrl+Shift+7+8+9. sharedSecret must match the " +
                             "client's." }
            };
            WriteCommon(root);

            root["listenAddress"] = ListenAddress;
            root["port"] = Port;
            root["allowedClients"] = AllowedClients;
            root["substationCode"] = SubstationCode;
            root["qrSeconds"] = QrSeconds;
            root["qr"] = new Dictionary<string, object>
            {
                { "format", QrFormat },
                { "ecc", QrEcc },
                { "urlTemplate", QrUrlTemplate }
            };
            root["columns"] = Columns.ToJson();

            Json.WriteFile(path, root);
        }
    }
}
