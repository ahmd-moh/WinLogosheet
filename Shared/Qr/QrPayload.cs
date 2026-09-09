using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Substation.Qr
{
    /// <summary>How the payload is framed and how large a code may get.</summary>
    public sealed class QrOptions
    {
        public string SubstationCode = "MSL-E";

        /// <summary>"v2json" — the framed format below. "csv" — plain unframed
        /// text for a scanner that only wants lines.</summary>
        public string Format = "v2json";

        /// <summary>L, M, Q or H. L fits the most data in one symbol.</summary>
        public string Ecc = "L";

        /// <summary>Optional wrapper, with {payload} where the framed string goes.</summary>
        public string UrlTemplate = "";
    }

    /// <summary>
    /// Builds the payload the Android app scans.
    ///
    /// Wire format, one chunk per QR code:
    ///
    ///     WLS2|&lt;substation&gt;|&lt;yyyy-MM-dd&gt;|&lt;part&gt;/&lt;total&gt;|&lt;crc32&gt;|&lt;chunk&gt;
    ///
    /// crc32 is the CRC-32 (uppercase hex, 8 digits) of the COMPLETE body, not of
    /// the chunk, so the phone can prove it reassembled every part before it
    /// accepts the session. The body is:
    ///
    ///     row (";" row)*      row := "H" hour ":" v1 "," ... "," v24
    ///
    /// Hours are clock hours in session order — 7…23, then 0…6. An empty field
    /// means that hour has no value for that column; it is not a zero.
    /// </summary>
    public sealed class QrPayloadBuilder
    {
        public const string Magic = "WLS2";

        /// <summary>Chunking targets a little under the true ceiling so the
        /// framing header always fits alongside the chunk.</summary>
        private const int HeaderAllowance = 48;

        public const int ColumnCount = 24;

        private readonly QrOptions _options;

        public QrPayloadBuilder(QrOptions options)
        {
            _options = options ?? new QrOptions();
        }

        public QrEcc Ecc { get { return QrCode.ParseEcc(_options.Ecc); } }

        /// <summary>
        /// One or more payload strings — one per QR code. A session normally fits
        /// a single code at error-correction L; a heavily loaded substation with
        /// four-digit currents can spill into a second, and the phone reassembles
        /// by part number.
        /// </summary>
        public List<string> Build(string sessionDate, IEnumerable<int> hoursInOrder,
                                  Dictionary<int, string[]> hourValues, QrEcc ecc)
        {
            if (string.Equals(_options.Format, "csv", StringComparison.OrdinalIgnoreCase))
                return new List<string> { BuildCsv(sessionDate, hoursInOrder, hourValues) };

            string body = BuildBody(hoursInOrder, hourValues);
            string crc = Crc32Hex(body);

            int budget = QrCode.MaxBytes((int)ecc) - HeaderAllowance;
            if (budget < 64) budget = 64;

            List<string> chunks = Split(body, budget);
            var payloads = new List<string>(chunks.Count);

            for (int i = 0; i < chunks.Count; i++)
            {
                string framed = string.Join("|", new[]
                {
                    Magic,
                    Sanitise(_options.SubstationCode),
                    sessionDate,
                    (i + 1).ToString(CultureInfo.InvariantCulture) + "/" +
                        chunks.Count.ToString(CultureInfo.InvariantCulture),
                    crc,
                    chunks[i]
                });

                payloads.Add(WrapUrl(framed));
            }

            return payloads;
        }

        private string WrapUrl(string framed)
        {
            if (!string.Equals(_options.Format, "url", StringComparison.OrdinalIgnoreCase)) return framed;
            if (string.IsNullOrEmpty(_options.UrlTemplate)) return framed;

            return _options.UrlTemplate.Replace("{payload}", Uri.EscapeDataString(framed));
        }

        // ── Body ───────────────────────────────────────────────────────────

        private static string BuildBody(IEnumerable<int> hoursInOrder, Dictionary<int, string[]> hourValues)
        {
            var sb = new StringBuilder();

            foreach (int hour in hoursInOrder)
            {
                string[] values;
                if (!hourValues.TryGetValue(hour, out values)) continue;
                if (IsAllEmpty(values)) continue;

                if (sb.Length > 0) sb.Append(';');
                sb.Append('H').Append(hour.ToString(CultureInfo.InvariantCulture)).Append(':');

                for (int i = 0; i < ColumnCount; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(Field(values, i));
                }
            }

            return sb.ToString();
        }

        private static string BuildCsv(string sessionDate, IEnumerable<int> hoursInOrder,
                                       Dictionary<int, string[]> hourValues)
        {
            var sb = new StringBuilder();
            sb.Append(sessionDate).Append('\n');

            foreach (int hour in hoursInOrder)
            {
                string[] values;
                if (!hourValues.TryGetValue(hour, out values)) continue;
                if (IsAllEmpty(values)) continue;

                sb.Append(hour.ToString(CultureInfo.InvariantCulture));
                for (int i = 0; i < ColumnCount; i++) sb.Append(',').Append(Field(values, i));
                sb.Append('\n');
            }
            return sb.ToString();
        }

        private static bool IsAllEmpty(string[] values)
        {
            if (values == null) return true;
            foreach (string v in values)
                if (!string.IsNullOrEmpty(v)) return false;
            return true;
        }

        /// <summary>One value, stripped of anything that would break the grammar.</summary>
        private static string Field(string[] values, int index)
        {
            if (values == null || index >= values.Length) return "";

            string v = values[index];
            if (string.IsNullOrEmpty(v)) return "";

            var sb = new StringBuilder(v.Length);
            foreach (char c in v)
                if (char.IsDigit(c) || c == '.' || c == '-') sb.Append(c);
            return sb.ToString();
        }

        private static string Sanitise(string text)
        {
            if (string.IsNullOrEmpty(text)) return "NA";

            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
                if (char.IsLetterOrDigit(c) || c == '-' || c == '_') sb.Append(c);
            return sb.Length == 0 ? "NA" : sb.ToString();
        }

        /// <summary>
        /// Splits on row boundaries where it can, so a single part still parses
        /// as whole hours if only one code was scanned.
        /// </summary>
        private static List<string> Split(string body, int budget)
        {
            var chunks = new List<string>();
            if (body.Length <= budget)
            {
                chunks.Add(body);
                return chunks;
            }

            int start = 0;
            while (start < body.Length)
            {
                int length = Math.Min(budget, body.Length - start);

                if (start + length < body.Length)
                {
                    int lastRow = body.LastIndexOf(';', start + length - 1, length);
                    if (lastRow > start) length = lastRow - start + 1;
                }

                chunks.Add(body.Substring(start, length));
                start += length;
            }
            return chunks;
        }

        // ── CRC-32 (IEEE 802.3, the polynomial zip and PNG use) ────────────

        private static readonly uint[] CrcTable = BuildCrcTable();

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++) c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                table[i] = c;
            }
            return table;
        }

        public static string Crc32Hex(string text)
        {
            byte[] bytes = new UTF8Encoding(false).GetBytes(text ?? "");

            uint crc = 0xFFFFFFFFu;
            foreach (byte b in bytes) crc = CrcTable[(crc ^ b) & 0xFF] ^ (crc >> 8);
            crc ^= 0xFFFFFFFFu;

            return crc.ToString("X8", CultureInfo.InvariantCulture);
        }
    }
}
