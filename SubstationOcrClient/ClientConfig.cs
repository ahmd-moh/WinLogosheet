using System;
using System.Collections.Generic;
using System.IO;
using Substation.Capture;
using Substation.Shared;

namespace SubstationOcrClient
{
    /// <summary>client.config.json — the 33 kV node.</summary>
    public sealed class ClientConfig : NodeConfig
    {
        /// <summary>Address of the 132 kV server node.</summary>
        public string ServerHost = "192.168.0.1";
        public int ServerPort = AgentProtocol.DefaultPort;

        /// <summary>Socket timeout for one push.</summary>
        public int TimeoutMs = 15000;

        /// <summary>How often to retry the backlog while the link is down.</summary>
        public int RetrySeconds = 120;

        public ClientConfig()
        {
            NodeId = "S33";
            DisplayName = "33 kV SWITCHGEAR SLD WALL VIEW";
            RoiConfigPath = "Config\\roi-33kv.json";
        }

        public static ClientConfig Load(string path)
        {
            var config = new ClientConfig();

            if (!File.Exists(path))
            {
                config.SetBaseFolder(path);
                config.Save(path);
                return config;
            }

            var root = Json.ReadFile(path);
            config.ReadCommon(root, path);
            config.ServerHost = Json.Str(root, "serverHost", config.ServerHost);
            config.ServerPort = Json.Int(root, "serverPort", config.ServerPort);
            config.TimeoutMs = Json.Int(root, "timeoutMs", config.TimeoutMs);
            config.RetrySeconds = Math.Max(15, Json.Int(root, "retrySeconds", config.RetrySeconds));
            return config;
        }

        public void Save(string path)
        {
            var root = new Dictionary<string, object>
            {
                { "_readme", "33 kV CLIENT node. It reads its own secondary screen every hour " +
                             "and pushes the values to the 132 kV server node. sharedSecret must " +
                             "match the server's." }
            };
            WriteCommon(root);
            root["serverHost"] = ServerHost;
            root["serverPort"] = ServerPort;
            root["timeoutMs"] = TimeoutMs;
            root["retrySeconds"] = RetrySeconds;

            Json.WriteFile(path, root);
        }
    }
}
