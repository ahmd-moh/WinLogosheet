using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

namespace Substation.Shared
{
    /// <summary>
    /// One red-outlined measurement box on a SCADA wall view.
    ///
    /// V1 screenshotted a single strip and let Tesseract find numbers anywhere in
    /// it, so a stray label could shift every value by one column. V2 reads only
    /// the boxes an engineer marked, and each box declares exactly which rows it
    /// contains — the position of a value in the logsheet no longer depends on
    /// what OCR happened to pick up next to it.
    /// </summary>
    public sealed class RoiDefinition
    {
        /// <summary>Stable identifier, unique inside one RoiConfig (e.g. "T1").</summary>
        public string Id = "";

        /// <summary>Text shown on the SCADA screen, for the calibration overlay.</summary>
        public string Label = "";

        /// <summary>Channel prefix. Channel keys are "<Channel>.<row>", e.g. "T1.MW".</summary>
        public string Channel = "";

        /// <summary>Disabled ROIs are kept in the file but never captured or sent.</summary>
        public bool Enabled = true;

        /// <summary>Box in reference-screen pixels (see RoiConfig.ReferenceWidth).</summary>
        public int X, Y, W, H;

        /// <summary>Row names top to bottom, e.g. ["KV","A","MW","MVAR"].</summary>
        public List<string> Rows = new List<string>();

        /// <summary>Upscale factor before OCR. The 33 kV boxes are ~10 px per row,
        /// well under the ~20 px x-height Tesseract wants, so they need 6x.</summary>
        public int Scale = 4;

        /// <summary>SCADA panels are light-on-dark; Tesseract prefers dark-on-light.</summary>
        public bool Invert = true;

        /// <summary>"green" isolates the green phosphor text, "gray" is a plain
        /// luminance conversion. Green gives far better separation on these screens.</summary>
        public string ColorChannel = "green";

        /// <summary>Rows read below this mean confidence are reported but flagged.</summary>
        public double MinConfidence = 55.0;

        /// <summary>Pixels trimmed from each row band before OCR, to drop the
        /// 1 px separator lines between the stacked value cells.</summary>
        public int RowPadding = 1;

        public Rectangle Rect
        {
            get { return new Rectangle(X, Y, W, H); }
            set { X = value.X; Y = value.Y; W = value.Width; H = value.Height; }
        }

        public string ChannelKey(string row)
        {
            string prefix = string.IsNullOrEmpty(Channel) ? Id : Channel;
            return prefix + "." + row;
        }

        public IEnumerable<string> ChannelKeys()
        {
            foreach (string row in Rows) yield return ChannelKey(row);
        }

        public static RoiDefinition FromJson(Dictionary<string, object> node)
        {
            var rect = Json.Dict(node, "rect");
            var roi = new RoiDefinition
            {
                Id = Json.Str(node, "id"),
                Label = Json.Str(node, "label"),
                Channel = Json.Str(node, "channel"),
                Enabled = Json.Bool(node, "enabled", true),
                X = Json.Int(rect, "x", 0),
                Y = Json.Int(rect, "y", 0),
                W = Json.Int(rect, "w", 0),
                H = Json.Int(rect, "h", 0),
                Rows = Json.Strings(node, "rows"),
                Scale = Math.Max(1, Math.Min(12, Json.Int(node, "scale", 4))),
                Invert = Json.Bool(node, "invert", true),
                ColorChannel = Json.Str(node, "colorChannel", "green"),
                MinConfidence = Json.Dbl(node, "minConfidence", 55.0),
                RowPadding = Math.Max(0, Json.Int(node, "rowPadding", 1))
            };
            if (string.IsNullOrEmpty(roi.Channel)) roi.Channel = roi.Id;
            return roi;
        }

        public Dictionary<string, object> ToJson()
        {
            return new Dictionary<string, object>
            {
                { "id", Id },
                { "label", Label },
                { "channel", Channel },
                { "enabled", Enabled },
                { "rect", new Dictionary<string, object> { { "x", X }, { "y", Y }, { "w", W }, { "h", H } } },
                { "rows", Rows },
                { "scale", Scale },
                { "invert", Invert },
                { "colorChannel", ColorChannel },
                { "minConfidence", MinConfidence },
                { "rowPadding", RowPadding }
            };
        }
    }

    /// <summary>The full set of boxes read from one SCADA display.</summary>
    public sealed class RoiConfig
    {
        public int Schema = 1;

        /// <summary>Must match the agent's serverId and the column map, e.g. "S132".</summary>
        public string ServerId = "";

        public string DisplayName = "";

        /// <summary>Screen size the rectangles were calibrated against. If the
        /// server runs at a different resolution the rectangles are scaled
        /// proportionally, which keeps a calibration usable after a display swap.</summary>
        public int ReferenceWidth = 1920;
        public int ReferenceHeight = 1080;

        public List<RoiDefinition> Rois = new List<RoiDefinition>();

        public RoiDefinition Find(string id)
        {
            foreach (var r in Rois)
                if (string.Equals(r.Id, id, StringComparison.OrdinalIgnoreCase)) return r;
            return null;
        }

        public IEnumerable<RoiDefinition> EnabledRois()
        {
            foreach (var r in Rois)
                if (r.Enabled && r.W > 0 && r.H > 0 && r.Rows.Count > 0) yield return r;
        }

        /// <summary>
        /// Rectangle in actual screen pixels. Returns the calibrated rectangle
        /// unchanged when the screen matches the reference size.
        /// </summary>
        public Rectangle RectFor(RoiDefinition roi, Size screen)
        {
            if (ReferenceWidth <= 0 || ReferenceHeight <= 0) return roi.Rect;
            if (screen.Width == ReferenceWidth && screen.Height == ReferenceHeight) return roi.Rect;

            double sx = (double)screen.Width / ReferenceWidth;
            double sy = (double)screen.Height / ReferenceHeight;
            return new Rectangle(
                (int)Math.Round(roi.X * sx),
                (int)Math.Round(roi.Y * sy),
                Math.Max(1, (int)Math.Round(roi.W * sx)),
                Math.Max(1, (int)Math.Round(roi.H * sy)));
        }

        public static RoiConfig Load(string path)
        {
            var root = Json.ReadFile(path);
            var cfg = new RoiConfig
            {
                Schema = Json.Int(root, "schema", 1),
                ServerId = Json.Str(root, "serverId"),
                DisplayName = Json.Str(root, "displayName"),
                ReferenceWidth = Json.Int(root, "referenceWidth", 1920),
                ReferenceHeight = Json.Int(root, "referenceHeight", 1080)
            };
            foreach (object node in Json.List(root, "rois"))
                cfg.Rois.Add(RoiDefinition.FromJson(Json.Dict(node)));
            return cfg;
        }

        public void Save(string path)
        {
            var rois = new List<object>();
            foreach (var r in Rois) rois.Add(r.ToJson());

            Json.WriteFile(path, new Dictionary<string, object>
            {
                { "schema", Schema },
                { "serverId", ServerId },
                { "displayName", DisplayName },
                { "referenceWidth", ReferenceWidth },
                { "referenceHeight", ReferenceHeight },
                { "rois", rois }
            });
        }
    }
}
