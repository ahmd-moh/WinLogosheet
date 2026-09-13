using System;
using System.Globalization;
using System.Text;

namespace Substation.Shared
{
    /// <summary>
    /// Turns one line of Tesseract output into a numeric string.
    ///
    /// The SCADA panels use a condensed seven-segment-ish face where OCR reliably
    /// confuses a handful of glyph pairs. Correcting those before parsing recovers
    /// most of the readings that V1 dropped as "n/a".
    /// </summary>
    public static class NumberFormat
    {
        /// <summary>
        /// Extracts the first signed decimal number from OCR text, applying
        /// letter-to-digit corrections. Returns "" when nothing numeric is found.
        /// </summary>
        public static string Normalize(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";

            var sb = new StringBuilder(raw.Length);
            foreach (char c in raw)
            {
                switch (c)
                {
                    // Unicode dashes the OCR emits for the minus sign.
                    case '‐': case '‑': case '‒':
                    case '–': case '—': case '−':
                        sb.Append('-'); break;

                    // Digit look-alikes.
                    case 'O': case 'o': case 'Q': case 'D': sb.Append('0'); break;
                    case 'l': case 'I': case '|': case 'i': sb.Append('1'); break;
                    case 'Z': case 'z': sb.Append('2'); break;
                    case 'S': case 's': sb.Append('5'); break;
                    case 'G': sb.Append('6'); break;
                    case 'T': sb.Append('7'); break;
                    case 'B': sb.Append('8'); break;
                    case 'g': case 'q': sb.Append('9'); break;

                    // Decimal separator variants.
                    case ',': case '·': sb.Append('.'); break;

                    default: sb.Append(c); break;
                }
            }

            string text = sb.ToString();

            // Pick the LONGEST numeric run rather than the first one. A border
            // artefact ahead of the value ("S 200.00 A" -> "5 200.00 A") would
            // otherwise win and log a single stray digit as the reading.
            string best = "";
            int i = 0;
            while (i < text.Length)
            {
                if (!IsNumberStart(text, i)) { i++; continue; }

                var run = new StringBuilder();
                bool seenDot = false, seenDigit = false;

                if (text[i] == '-' || text[i] == '+')
                {
                    if (text[i] == '-') run.Append('-');
                    i++;
                }

                for (; i < text.Length; i++)
                {
                    char c = text[i];
                    if (char.IsDigit(c)) { run.Append(c); seenDigit = true; continue; }
                    if (c == '.' && !seenDot && seenDigit) { run.Append('.'); seenDot = true; continue; }
                    break;
                }

                if (!seenDigit) continue;

                // "12." -> "12"
                string candidate = run.ToString().TrimEnd('.');
                if (candidate.Length > best.Length) best = candidate;
            }

            if (best.Length == 0 || best == "-") return "";

            double probe;
            if (!double.TryParse(best, NumberStyles.Float, CultureInfo.InvariantCulture, out probe)) return "";

            return best;
        }

        private static bool IsNumberStart(string text, int i)
        {
            char c = text[i];
            if (char.IsDigit(c)) return true;
            // A sign only starts a number when a digit follows it.
            if ((c == '-' || c == '+') && i + 1 < text.Length && char.IsDigit(text[i + 1])) return true;
            return false;
        }

        /// <summary>
        /// Sanity band per measurement row. A value outside the band is still
        /// reported, but flagged so the collector can prefer another source or
        /// ask the operator instead of silently logging a mis-read digit.
        /// </summary>
        public static bool InExpectedRange(string row, string busClass, double value)
        {
            switch ((row ?? "").ToUpperInvariant())
            {
                case "KV":
                    if (busClass == "132") return value >= 100 && value <= 160;
                    if (busClass == "33") return value >= 25 && value <= 40;
                    return value >= 0 && value <= 800;
                case "A":
                    return value >= -4000 && value <= 4000;
                case "MW":
                case "MVAR":
                    return value >= -300 && value <= 300;
                case "HZ":
                    return value >= 45 && value <= 55;
                default:
                    return true;
            }
        }
    }
}
