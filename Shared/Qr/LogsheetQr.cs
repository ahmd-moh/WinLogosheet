using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;

namespace Substation.Qr
{
    /// <summary>
    /// Builds the QR payload the Android companion app scans.
    ///
    /// THE FORMAT IS A CONTRACT. The phone rejects anything whose first token is
    /// not "LS1", so this must stay byte-for-byte what the app parses:
    ///
    ///     LS1 &lt;YYYYMMDD&gt;*&lt;row&gt;*&lt;row&gt;*...
    ///
    ///     row = [-]HH:c1.c2.c3. ... .c24
    ///           HH = two-digit hour; midnight is 24, never 00
    ///           -  = optional leading marker, "skip this hour"
    ///           cX = integer reading, or empty when there is no value
    ///                (so "12..7" means 12, empty, 7)
    ///
    /// Every character stays inside the QR alphanumeric table
    /// (0-9 A-Z space $ % * + - . / :) on purpose. That keeps the code in
    /// alphanumeric mode, which carries two characters per 11 bits instead of
    /// eight bits each — a full 24-hour session lands near version 30 rather
    /// than 37 at level M, and that is the difference between an easy scan and
    /// a fiddly one.
    ///
    /// All 24 cells are always written, trailing empties included, so column
    /// positions line up exactly with the desktop grid.
    /// </summary>
    public static class LogsheetQr
    {
        public const string FormatTag = "LS1";
        public const int ColumnCount = 24;

        private const char RowSep = '*';
        private const char CellSep = '.';
        private const char HourSep = ':';
        private const char SkipMark = '-';

        /// <summary>
        /// Serialises the session. <paramref name="hoursInOrder"/> drives the row
        /// order; hours with no values at all are left out, as the phone expects
        /// 1..24 rows rather than always 24.
        /// </summary>
        public static string BuildPayload(DateTime sessionDate, IEnumerable<int> hoursInOrder,
                                          Dictionary<int, string[]> hourValues,
                                          ICollection<int> skippedHours = null)
        {
            var sb = new StringBuilder(2048);
            sb.Append(FormatTag).Append(' ')
              .Append(sessionDate.ToString("yyyyMMdd", CultureInfo.InvariantCulture));

            foreach (int hour in hoursInOrder)
            {
                string[] values;
                if (!hourValues.TryGetValue(hour, out values)) continue;
                if (IsAllEmpty(values)) continue;

                sb.Append(RowSep);
                if (skippedHours != null && skippedHours.Contains(hour)) sb.Append(SkipMark);
                sb.Append(PayloadHour(hour).ToString("00", CultureInfo.InvariantCulture)).Append(HourSep);

                for (int col = 0; col < ColumnCount; col++)
                {
                    if (col > 0) sb.Append(CellSep);
                    AppendDigits(sb, col < values.Length ? values[col] : null);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Midnight is hour 24 on the sheet, never 00 — the phone's hour table
        /// runs 08..24 then 01..07 and has no entry for zero.
        /// </summary>
        public static int PayloadHour(int clockHour)
        {
            return clockHour == 0 ? 24 : clockHour;
        }

        /// <summary>
        /// Readings are whole numbers on the sheet, so only digits survive. A
        /// sign or a decimal point would also push the payload out of
        /// alphanumeric mode's digit run and cost capacity for nothing.
        /// </summary>
        private static void AppendDigits(StringBuilder sb, string raw)
        {
            if (string.IsNullOrEmpty(raw)) return;

            foreach (char c in raw)
                if (c >= '0' && c <= '9') sb.Append(c);
        }

        private static bool IsAllEmpty(string[] values)
        {
            if (values == null) return true;

            foreach (string v in values)
                if (!string.IsNullOrEmpty(v)) return false;
            return true;
        }

        /// <summary>
        /// Renders the payload as a single QR code.
        ///
        /// Prefers error-correction M, which is robust for a phone camera, and
        /// steps down to L only when the session will not fit. There is
        /// deliberately no splitting across several codes: the companion app
        /// parses one payload and has no notion of parts.
        /// </summary>
        public static QrCode Encode(string payload, out QrEcc eccUsed)
        {
            payload = payload ?? string.Empty;

            var levels = new[] { QrEcc.Medium, QrEcc.Low };
            ArgumentException lastFailure = null;

            foreach (QrEcc level in levels)
            {
                try
                {
                    QrCode code = QrCode.Encode(payload, level);
                    eccUsed = level;
                    return code;
                }
                catch (ArgumentException ex)
                {
                    lastFailure = ex;   // too long at this level; try the next
                }
            }

            throw new InvalidOperationException(
                "The session is too large to fit in a single QR code. " +
                (lastFailure == null ? "" : lastFailure.Message));
        }

        /// <summary>Payload plus bitmap in one call, for the display window.</summary>
        public static Bitmap Render(string payload, int pixelsPerModule, out QrEcc eccUsed, out int version)
        {
            QrCode code = Encode(payload, out eccUsed);
            version = code.Version;
            return code.ToBitmap(Math.Max(1, pixelsPerModule), 4);
        }
    }
}
