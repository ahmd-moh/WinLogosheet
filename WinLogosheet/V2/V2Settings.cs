using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Substation.Shared;

namespace WinLogosheet.V2
{
    /// <summary>One agent WinLogosheet talks to.</summary>
    public sealed class AgentEndpoint
    {
        public string Id = "";       // must match the agent's serverId
        public string Host = "";
        public int Port = AgentProtocol.DefaultPort;
        public string Label = "";
        public bool Enabled = true;

        public override string ToString()
        {
            return (string.IsNullOrEmpty(Label) ? Id : Label) + " (" + Host + ":" + Port + ")";
        }

        public static AgentEndpoint FromJson(Dictionary<string, object> node)
        {
            return new AgentEndpoint
            {
                Id = Json.Str(node, "id"),
                Host = Json.Str(node, "host"),
                Port = Json.Int(node, "port", AgentProtocol.DefaultPort),
                Label = Json.Str(node, "label"),
                Enabled = Json.Bool(node, "enabled", true)
            };
        }

        public Dictionary<string, object> ToJson()
        {
            return new Dictionary<string, object>
            {
                { "id", Id }, { "host", Host }, { "port", Port },
                { "label", Label }, { "enabled", Enabled }
            };
        }
    }

    /// <summary>
    /// winlogosheet.v2.json — which agents to poll, how the 24 logsheet columns
    /// map onto their channels, and how the QR handoff is built.
    /// </summary>
    public sealed class V2Settings
    {
        public const string FileName = "winlogosheet.v2.json";

        public bool Enabled = true;
        public List<AgentEndpoint> Agents = new List<AgentEndpoint>();
        public string SharedSecret = "";
        public int TimeoutMs = 15000;

        /// <summary>Minute past the hour at which WinLogosheet pulls the agents.
        /// One minute behind the agents' own capture minute, so the values are
        /// already stored when the collector asks for them.</summary>
        public int CollectMinute = 3;
        public bool AutoCollect = true;

        /// <summary>Below this confidence a value is still filled in, but the
        /// collector reports it so the operator checks it against the screen.</summary>
        public double LowConfidenceThreshold = 60.0;

        /// <summary>Never overwrite a value the operator typed by hand.</summary>
        public bool PreserveManualEdits = true;

        public ColumnMap Columns = ColumnMap.Default();

        // QR handoff
        public string QrFormat = "v2json";   // v2json | csv | url
        public string QrEcc = "M";           // L | M | Q | H
        public string QrUrlTemplate = "";    // used when QrFormat is "url"
        public string SubstationCode = "MSL-E";

        public AgentEndpoint FindAgent(string id)
        {
            foreach (AgentEndpoint a in Agents)
                if (string.Equals(a.Id, id, StringComparison.OrdinalIgnoreCase)) return a;
            return null;
        }

        public static string DefaultPath()
        {
            return Path.Combine(Application.StartupPath, FileName);
        }

        public static V2Settings Load(string path = null)
        {
            path = path ?? DefaultPath();

            var settings = new V2Settings();
            if (!File.Exists(path))
            {
                settings.Agents.Add(new AgentEndpoint
                {
                    Id = "S132", Host = "127.0.0.1", Port = AgentProtocol.DefaultPort,
                    Label = "132 kV server"
                });
                settings.Agents.Add(new AgentEndpoint
                {
                    Id = "S33", Host = "192.168.0.2", Port = AgentProtocol.DefaultPort,
                    Label = "33 kV server"
                });
                settings.Save(path);
                return settings;
            }

            var root = Json.ReadFile(path);
            settings.Enabled = Json.Bool(root, "enabled", true);
            settings.SharedSecret = Json.Str(root, "sharedSecret");
            settings.TimeoutMs = Json.Int(root, "timeoutMs", settings.TimeoutMs);
            settings.CollectMinute = Math.Max(0, Math.Min(59, Json.Int(root, "collectMinute", settings.CollectMinute)));
            settings.AutoCollect = Json.Bool(root, "autoCollect", settings.AutoCollect);
            settings.LowConfidenceThreshold = Json.Dbl(root, "lowConfidenceThreshold", settings.LowConfidenceThreshold);
            settings.PreserveManualEdits = Json.Bool(root, "preserveManualEdits", settings.PreserveManualEdits);

            settings.Agents.Clear();
            foreach (object node in Json.List(root, "agents"))
                settings.Agents.Add(AgentEndpoint.FromJson(Json.Dict(node)));

            settings.Columns = ColumnMap.FromJson(Json.List(root, "columns"));

            var qr = Json.Dict(root, "qr");
            settings.QrFormat = Json.Str(qr, "format", settings.QrFormat);
            settings.QrEcc = Json.Str(qr, "ecc", settings.QrEcc);
            settings.QrUrlTemplate = Json.Str(qr, "urlTemplate", settings.QrUrlTemplate);
            settings.SubstationCode = Json.Str(root, "substationCode", settings.SubstationCode);

            return settings;
        }

        public void Save(string path = null)
        {
            path = path ?? DefaultPath();

            var agents = new List<object>();
            foreach (AgentEndpoint a in Agents) agents.Add(a.ToJson());

            Json.WriteFile(path, new Dictionary<string, object>
            {
                { "_readme", "V2 settings. sharedSecret must match agent.config.json on both " +
                             "servers. Agent ids must match the serverId each agent reports, and " +
                             "the source of every column below." },
                { "enabled", Enabled },
                { "substationCode", SubstationCode },
                { "sharedSecret", SharedSecret },
                { "timeoutMs", TimeoutMs },
                { "collectMinute", CollectMinute },
                { "autoCollect", AutoCollect },
                { "lowConfidenceThreshold", LowConfidenceThreshold },
                { "preserveManualEdits", PreserveManualEdits },
                { "agents", agents },
                { "columns", Columns.ToJson() },
                { "qr", new Dictionary<string, object>
                    {
                        { "format", QrFormat },
                        { "ecc", QrEcc },
                        { "urlTemplate", QrUrlTemplate }
                    }
                }
            });
        }
    }
}
