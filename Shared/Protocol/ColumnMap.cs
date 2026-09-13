using System;
using System.Collections.Generic;

namespace Substation.Shared
{
    /// <summary>
    /// Binds one logsheet column (1-24) to one channel on one agent.
    /// </summary>
    public sealed class ColumnBinding
    {
        public int Column;            // 1-24, matches textBox1..textBox24
        public string ServerId = "";  // "S132" or "S33"
        public string Channel = "";   // "YARMJA.KV", "T1.MW", ...
        public string Label = "";     // shown in the collector report
        public string BusClass = "";  // "132", "33" or "" - drives the range check

        /// <summary>Optional second source used when the primary reads nothing.
        /// The 33 kV incomer kV columns can fall back to the busbar boxes.</summary>
        public string FallbackServerId = "";
        public string FallbackChannel = "";

        public bool HasFallback
        {
            get { return !string.IsNullOrEmpty(FallbackServerId) && !string.IsNullOrEmpty(FallbackChannel); }
        }

        public static ColumnBinding FromJson(Dictionary<string, object> node)
        {
            return new ColumnBinding
            {
                Column = Json.Int(node, "col", 0),
                ServerId = Json.Str(node, "source"),
                Channel = Json.Str(node, "channel"),
                Label = Json.Str(node, "label"),
                BusClass = Json.Str(node, "busClass"),
                FallbackServerId = Json.Str(node, "fallbackSource"),
                FallbackChannel = Json.Str(node, "fallbackChannel")
            };
        }

        public Dictionary<string, object> ToJson()
        {
            var node = new Dictionary<string, object>
            {
                { "col", Column },
                { "source", ServerId },
                { "channel", Channel },
                { "label", Label },
                { "busClass", BusClass }
            };
            if (HasFallback)
            {
                node["fallbackSource"] = FallbackServerId;
                node["fallbackChannel"] = FallbackChannel;
            }
            return node;
        }
    }

    /// <summary>The 24 bindings that turn two agent frames into one logsheet row.</summary>
    public sealed class ColumnMap
    {
        public const int ColumnCount = 24;

        public List<ColumnBinding> Bindings = new List<ColumnBinding>();

        public ColumnBinding For(int column)
        {
            foreach (var b in Bindings)
                if (b.Column == column) return b;
            return null;
        }

        /// <summary>
        /// The wiring of this substation: OHL-1 and OHL-2 come from the 132 kV
        /// wall view, the three transformer incomers and the four logged cable
        /// feeders from the 33 kV wall view. Used when no column map is on disk.
        /// </summary>
        public static ColumnMap Default()
        {
            var map = new ColumnMap();

            Add(map, 1, "S132", "YARMJA.KV", "Yarmja KV", "132");
            Add(map, 2, "S132", "YARMJA.A", "Yarmja A", "132");
            Add(map, 3, "S132", "YARMJA.MW", "Yarmja MW", "132");
            Add(map, 4, "S132", "YARMJA.MVAR", "Yarmja MVAR", "132");

            Add(map, 5, "S132", "QAYARA.KV", "Qayra KV", "132");
            Add(map, 6, "S132", "QAYARA.A", "Qayra A", "132");
            Add(map, 7, "S132", "QAYARA.MW", "Qayra MW", "132");
            Add(map, 8, "S132", "QAYARA.MVAR", "Qayra MVAR", "132");

            // The red boxes on the 33 kV view carry A / MW / MVAR only. The kV
            // column is served by the busbar box feeding that incomer, which is
            // why the three BUS ROIs exist even though they are not red-outlined.
            Add(map, 9, "S33", "BUS1A.KV", "T1 KV", "33");
            Add(map, 10, "S33", "T1.A", "T1 A", "33");
            Add(map, 11, "S33", "T1.MW", "T1 MW", "33");
            Add(map, 12, "S33", "T1.MVAR", "T1 MVAR", "33");

            Add(map, 13, "S33", "BUS2A.KV", "T2 KV", "33");
            Add(map, 14, "S33", "T2.A", "T2 A", "33");
            Add(map, 15, "S33", "T2.MW", "T2 MW", "33");
            Add(map, 16, "S33", "T2.MVAR", "T2 MVAR", "33");

            Add(map, 17, "S33", "BUS2B.KV", "T3 KV", "33");
            Add(map, 18, "S33", "T3.A", "T3 A", "33");
            Add(map, 19, "S33", "T3.MW", "T3 MW", "33");
            Add(map, 20, "S33", "T3.MVAR", "T3 MVAR", "33");

            // Feeder columns log active power only.
            Add(map, 21, "S33", "FDR_DOMEZ.MW", "Domez MW", "33");
            Add(map, 22, "S33", "FDR_SUMMER.MW", "Summer MW", "33");
            Add(map, 23, "S33", "FDR_SALAM1.MW", "Salam 1 MW", "33");
            Add(map, 24, "S33", "FDR_SALAM2.MW", "Salam 2 MW", "33");

            return map;
        }

        private static void Add(ColumnMap map, int col, string server, string channel, string label, string busClass)
        {
            map.Bindings.Add(new ColumnBinding
            {
                Column = col,
                ServerId = server,
                Channel = channel,
                Label = label,
                BusClass = busClass
            });
        }

        public static ColumnMap FromJson(List<object> nodes)
        {
            var map = new ColumnMap();
            foreach (object n in nodes)
            {
                var binding = ColumnBinding.FromJson(Json.Dict(n));
                if (binding.Column >= 1 && binding.Column <= ColumnCount) map.Bindings.Add(binding);
            }
            return map.Bindings.Count == 0 ? Default() : map;
        }

        public List<object> ToJson()
        {
            var list = new List<object>();
            foreach (var b in Bindings) list.Add(b.ToJson());
            return list;
        }
    }
}
