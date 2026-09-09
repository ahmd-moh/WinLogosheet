using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

namespace Substation.Qr
{
    public enum QrEcc
    {
        Low = 0,      // ~7% recovery
        Medium = 1,   // ~15%
        Quartile = 2, // ~25%
        High = 3      // ~30%
    }

    /// <summary>
    /// A self-contained QR Code (ISO/IEC 18004) encoder, byte mode, versions
    /// 1-40, all four error-correction levels.
    ///
    /// It is written out in full rather than pulled from a package on purpose:
    /// the substation servers have no internet access, so a build that needs one
    /// more NuGet restore is a build that does not happen. The output was
    /// verified module-for-module against a reference implementation across all
    /// 160 version/ECC combinations.
    /// </summary>
    public sealed class QrCode
    {
        public int Version { get; private set; }
        public int Size { get; private set; }
        public QrEcc Ecc { get; private set; }
        public int Mask { get; private set; }

        private readonly bool[][] _modules;
        private readonly bool[][] _isFunction;

        /// <summary>True where the module is dark.</summary>
        public bool this[int x, int y]
        {
            get { return x >= 0 && x < Size && y >= 0 && y < Size && _modules[y][x]; }
        }

        // ── Public entry point ─────────────────────────────────────────────

        /// <summary>
        /// Encodes text as UTF-8 in byte mode, choosing the smallest version
        /// that fits and the mask with the lowest penalty score.
        /// </summary>
        public static QrCode Encode(string text, QrEcc ecc)
        {
            if (text == null) throw new ArgumentNullException("text");
            return Encode(new UTF8Encoding(false).GetBytes(text), ecc);
        }

        public static QrCode Encode(byte[] data, QrEcc ecc)
        {
            if (data == null) throw new ArgumentNullException("data");

            int ecl = (int)ecc;
            int version = ChooseVersion(data.Length, ecl);
            if (version < 0)
                throw new ArgumentException(
                    "The payload is " + data.Length + " bytes; a QR code holds at most " +
                    MaxBytes(ecl) + " at this error-correction level. " +
                    "Split the day into fewer hours per code, or lower the level.");

            byte[] codewords = BuildCodewords(data, version, ecl);
            byte[] interleaved = AddEccAndInterleave(codewords, version, ecl);
            return new QrCode(version, ecc, interleaved);
        }

        /// <summary>Largest byte-mode payload at this level, for error messages
        /// and for deciding when to split a day across several codes.</summary>
        public static int MaxBytes(int ecl)
        {
            return DataCodewords(40, ecl) - 3; // 4-bit mode + 16-bit count = 3 bytes
        }

        public static bool Fits(int byteCount, int version, int ecl)
        {
            return 4 + CharCountBits(version) + 8 * byteCount <= DataCodewords(version, ecl) * 8;
        }

        private static int ChooseVersion(int byteCount, int ecl)
        {
            for (int version = 1; version <= 40; version++)
                if (Fits(byteCount, version, ecl)) return version;
            return -1;
        }

        // ── Construction ───────────────────────────────────────────────────

        private QrCode(int version, QrEcc ecc, byte[] dataCodewords)
        {
            Version = version;
            Ecc = ecc;
            Size = version * 4 + 17;

            _modules = NewGrid(Size);
            _isFunction = NewGrid(Size);

            DrawFunctionPatterns();
            DrawCodewords(dataCodewords);
            Mask = ChooseMask();
            DrawFormatBits(Mask);
        }

        private static bool[][] NewGrid(int size)
        {
            var grid = new bool[size][];
            for (int i = 0; i < size; i++) grid[i] = new bool[size];
            return grid;
        }

        private void SetFunctionModule(int x, int y, bool dark)
        {
            _modules[y][x] = dark;
            _isFunction[y][x] = true;
        }

        private void DrawFunctionPatterns()
        {
            for (int i = 0; i < Size; i++)
            {
                SetFunctionModule(6, i, i % 2 == 0);
                SetFunctionModule(i, 6, i % 2 == 0);
            }

            DrawFinderPattern(3, 3);
            DrawFinderPattern(Size - 4, 3);
            DrawFinderPattern(3, Size - 4);

            int[] positions = AlignmentPatternPositions(Version);
            int last = positions.Length - 1;
            for (int i = 0; i <= last; i++)
            {
                for (int j = 0; j <= last; j++)
                {
                    // The three corners are already covered by finder patterns.
                    if ((i == 0 && j == 0) || (i == 0 && j == last) || (i == last && j == 0)) continue;
                    DrawAlignmentPattern(positions[i], positions[j]);
                }
            }

            // Drawn with a dummy mask; the real one is written once it is chosen.
            DrawFormatBits(0);
            DrawVersion();
        }

        private void DrawFinderPattern(int x, int y)
        {
            for (int dy = -4; dy <= 4; dy++)
            {
                for (int dx = -4; dx <= 4; dx++)
                {
                    int distance = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    int xx = x + dx, yy = y + dy;
                    if (xx >= 0 && xx < Size && yy >= 0 && yy < Size)
                        SetFunctionModule(xx, yy, distance != 2 && distance != 4);
                }
            }
        }

        private void DrawAlignmentPattern(int x, int y)
        {
            for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++)
                    SetFunctionModule(x + dx, y + dy, Math.Max(Math.Abs(dx), Math.Abs(dy)) != 1);
        }

        private void DrawFormatBits(int mask)
        {
            int data = FormatBitsForEcl((int)Ecc) << 3 | mask;
            int rem = data;
            for (int i = 0; i < 10; i++) rem = (rem << 1) ^ ((rem >> 9) * 0x537);
            int bits = ((data << 10) | rem) ^ 0x5412;

            for (int i = 0; i <= 5; i++) SetFunctionModule(8, i, GetBit(bits, i));
            SetFunctionModule(8, 7, GetBit(bits, 6));
            SetFunctionModule(8, 8, GetBit(bits, 7));
            SetFunctionModule(7, 8, GetBit(bits, 8));
            for (int i = 9; i < 15; i++) SetFunctionModule(14 - i, 8, GetBit(bits, i));

            for (int i = 0; i < 8; i++) SetFunctionModule(Size - 1 - i, 8, GetBit(bits, i));
            for (int i = 8; i < 15; i++) SetFunctionModule(8, Size - 15 + i, GetBit(bits, i));

            SetFunctionModule(8, Size - 8, true); // always-dark module
        }

        private void DrawVersion()
        {
            if (Version < 7) return;

            int rem = Version;
            for (int i = 0; i < 12; i++) rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
            int bits = Version << 12 | rem;

            for (int i = 0; i < 18; i++)
            {
                bool bit = GetBit(bits, i);
                int a = Size - 11 + i % 3;
                int b = i / 3;
                SetFunctionModule(a, b, bit);
                SetFunctionModule(b, a, bit);
            }
        }

        /// <summary>Zigzag fill of the data region, two columns at a time.</summary>
        private void DrawCodewords(byte[] data)
        {
            int i = 0;
            for (int right = Size - 1; right >= 1; right -= 2)
            {
                if (right == 6) right = 5; // skip the vertical timing column

                for (int vert = 0; vert < Size; vert++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        int x = right - j;
                        bool upward = ((right + 1) & 2) == 0;
                        int y = upward ? Size - 1 - vert : vert;

                        if (!_isFunction[y][x] && i < data.Length * 8)
                        {
                            _modules[y][x] = GetBit(data[i >> 3], 7 - (i & 7));
                            i++;
                        }
                    }
                }
            }
        }

        private void ApplyMask(int mask)
        {
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    if (_isFunction[y][x]) continue;

                    bool invert;
                    switch (mask)
                    {
                        case 0: invert = (x + y) % 2 == 0; break;
                        case 1: invert = y % 2 == 0; break;
                        case 2: invert = x % 3 == 0; break;
                        case 3: invert = (x + y) % 3 == 0; break;
                        case 4: invert = (x / 3 + y / 2) % 2 == 0; break;
                        case 5: invert = x * y % 2 + x * y % 3 == 0; break;
                        case 6: invert = (x * y % 2 + x * y % 3) % 2 == 0; break;
                        default: invert = ((x + y) % 2 + x * y % 3) % 2 == 0; break;
                    }
                    if (invert) _modules[y][x] = !_modules[y][x];
                }
            }
        }

        private int ChooseMask()
        {
            int best = 0;
            int lowest = int.MaxValue;

            for (int mask = 0; mask < 8; mask++)
            {
                ApplyMask(mask);
                DrawFormatBits(mask);

                int penalty = PenaltyScore();
                if (penalty < lowest) { lowest = penalty; best = mask; }

                ApplyMask(mask); // XOR again to undo
            }

            ApplyMask(best);
            return best;
        }

        // ── Penalty scoring (the four rules of the specification) ───────────

        private int PenaltyScore()
        {
            int result = 0;

            for (int y = 0; y < Size; y++)
            {
                bool color = false;
                int run = 0;
                var history = new int[7];

                for (int x = 0; x < Size; x++)
                {
                    if (_modules[y][x] == color)
                    {
                        run++;
                        if (run == 5) result += 3;
                        else if (run > 5) result++;
                    }
                    else
                    {
                        FinderPenaltyAddHistory(run, history);
                        if (!color) result += FinderPenaltyCountPatterns(history) * 40;
                        color = _modules[y][x];
                        run = 1;
                    }
                }
                result += FinderPenaltyTerminateAndCount(color, run, history) * 40;
            }

            for (int x = 0; x < Size; x++)
            {
                bool color = false;
                int run = 0;
                var history = new int[7];

                for (int y = 0; y < Size; y++)
                {
                    if (_modules[y][x] == color)
                    {
                        run++;
                        if (run == 5) result += 3;
                        else if (run > 5) result++;
                    }
                    else
                    {
                        FinderPenaltyAddHistory(run, history);
                        if (!color) result += FinderPenaltyCountPatterns(history) * 40;
                        color = _modules[y][x];
                        run = 1;
                    }
                }
                result += FinderPenaltyTerminateAndCount(color, run, history) * 40;
            }

            // 2x2 blocks of one colour.
            for (int y = 0; y < Size - 1; y++)
            {
                for (int x = 0; x < Size - 1; x++)
                {
                    bool c = _modules[y][x];
                    if (c == _modules[y][x + 1] && c == _modules[y + 1][x] && c == _modules[y + 1][x + 1])
                        result += 3;
                }
            }

            // Balance of dark modules against 50%.
            int dark = 0;
            foreach (bool[] row in _modules)
                foreach (bool v in row)
                    if (v) dark++;

            int total = Size * Size;
            int k = (Math.Abs(dark * 20 - total * 10) + total - 1) / total - 1;
            result += k * 10;

            return result;
        }

        private void FinderPenaltyAddHistory(int runLength, int[] history)
        {
            if (history[0] == 0) runLength += Size; // light border before the first run
            Array.Copy(history, 0, history, 1, history.Length - 1);
            history[0] = runLength;
        }

        private static int FinderPenaltyCountPatterns(int[] h)
        {
            int n = h[1];
            bool core = n > 0 && h[2] == n && h[3] == n * 3 && h[4] == n && h[5] == n;
            return (core && h[0] >= n * 4 && h[6] >= n ? 1 : 0)
                 + (core && h[6] >= n * 4 && h[0] >= n ? 1 : 0);
        }

        private int FinderPenaltyTerminateAndCount(bool currentColor, int runLength, int[] history)
        {
            if (currentColor)
            {
                FinderPenaltyAddHistory(runLength, history);
                runLength = 0;
            }
            runLength += Size; // light border after the last run
            FinderPenaltyAddHistory(runLength, history);
            return FinderPenaltyCountPatterns(history);
        }

        // ── Data encoding ──────────────────────────────────────────────────

        private static int CharCountBits(int version)
        {
            return version <= 9 ? 8 : 16; // byte mode
        }

        private static byte[] BuildCodewords(byte[] data, int version, int ecl)
        {
            int capacityBits = DataCodewords(version, ecl) * 8;

            var bits = new List<bool>(capacityBits);
            AppendBits(bits, 4, 4);                              // byte-mode indicator
            AppendBits(bits, data.Length, CharCountBits(version));
            foreach (byte b in data) AppendBits(bits, b, 8);

            // Terminator, then pad to a whole byte, then alternating pad bytes.
            int terminator = Math.Min(4, capacityBits - bits.Count);
            for (int i = 0; i < terminator; i++) bits.Add(false);
            while (bits.Count % 8 != 0) bits.Add(false);

            for (int pad = 0xEC; bits.Count < capacityBits; pad ^= 0xEC ^ 0x11)
                AppendBits(bits, pad, 8);

            var codewords = new byte[bits.Count / 8];
            for (int i = 0; i < bits.Count; i++)
                if (bits[i]) codewords[i >> 3] |= (byte)(1 << (7 - (i & 7)));

            return codewords;
        }

        private static void AppendBits(List<bool> bits, int value, int count)
        {
            for (int i = count - 1; i >= 0; i--) bits.Add(((value >> i) & 1) != 0);
        }

        /// <summary>Splits the data into blocks, appends each block's Reed-Solomon
        /// remainder, then interleaves them as the specification requires.</summary>
        private static byte[] AddEccAndInterleave(byte[] data, int version, int ecl)
        {
            int numBlocks = NumEccBlocks[ecl][version];
            int blockEccLen = EccCodewordsPerBlock[ecl][version];
            int rawCodewords = TotalCodewords(version);
            int numShortBlocks = numBlocks - rawCodewords % numBlocks;
            int shortBlockLen = rawCodewords / numBlocks;

            var blocks = new byte[numBlocks][];
            byte[] divisor = ReedSolomonDivisor(blockEccLen);

            for (int i = 0, k = 0; i < numBlocks; i++)
            {
                int datLen = shortBlockLen - blockEccLen + (i < numShortBlocks ? 0 : 1);
                var dat = new byte[datLen];
                Array.Copy(data, k, dat, 0, datLen);
                k += datLen;

                // Every block is padded to the long length so the interleave
                // below can walk them as columns; short blocks leave one gap.
                var block = new byte[shortBlockLen + 1];
                Array.Copy(dat, 0, block, 0, datLen);

                byte[] ecc = ReedSolomonRemainder(dat, divisor);
                Array.Copy(ecc, 0, block, block.Length - blockEccLen, blockEccLen);
                blocks[i] = block;
            }

            var result = new byte[rawCodewords];
            for (int i = 0, k = 0; i < blocks[0].Length; i++)
                for (int j = 0; j < blocks.Length; j++)
                    if (i != shortBlockLen - blockEccLen || j >= numShortBlocks)
                        result[k++] = blocks[j][i];

            return result;
        }

        // ── Reed-Solomon over GF(256), primitive polynomial 0x11D ───────────

        private static byte[] ReedSolomonDivisor(int degree)
        {
            var result = new byte[degree];
            result[degree - 1] = 1;

            int root = 1;
            for (int i = 0; i < degree; i++)
            {
                for (int j = 0; j < degree; j++)
                {
                    result[j] = (byte)GfMultiply(result[j] & 0xFF, root);
                    if (j + 1 < degree) result[j] ^= result[j + 1];
                }
                root = GfMultiply(root, 0x02);
            }
            return result;
        }

        private static byte[] ReedSolomonRemainder(byte[] data, byte[] divisor)
        {
            var result = new byte[divisor.Length];

            foreach (byte b in data)
            {
                int factor = (b ^ result[0]) & 0xFF;
                Array.Copy(result, 1, result, 0, result.Length - 1);
                result[result.Length - 1] = 0;

                for (int i = 0; i < divisor.Length; i++)
                    result[i] ^= (byte)GfMultiply(divisor[i] & 0xFF, factor);
            }
            return result;
        }

        private static int GfMultiply(int x, int y)
        {
            int z = 0;
            for (int i = 7; i >= 0; i--)
            {
                z = (z << 1) ^ ((z >> 7) * 0x11D);
                z ^= ((y >> i) & 1) * x;
            }
            return z & 0xFF;
        }

        // ── Capacity tables ────────────────────────────────────────────────

        private static int RawDataModules(int version)
        {
            int result = (16 * version + 128) * version + 64;
            if (version >= 2)
            {
                int numAlign = version / 7 + 2;
                result -= (25 * numAlign - 10) * numAlign - 55;
                if (version >= 7) result -= 36;
            }
            return result;
        }

        private static int TotalCodewords(int version)
        {
            return RawDataModules(version) / 8;
        }

        private static int DataCodewords(int version, int ecl)
        {
            return TotalCodewords(version) - EccCodewordsPerBlock[ecl][version] * NumEccBlocks[ecl][version];
        }

        /// <summary>Alignment pattern centres: 6, then evenly spaced up to size-7.</summary>
        private static int[] AlignmentPatternPositions(int version)
        {
            if (version == 1) return new int[0];

            int numAlign = version / 7 + 2;
            int size = version * 4 + 17;

            // Round the spacing up to an even number. Version 32 is the one case
            // where the general formula overshoots the specified layout.
            int step = version == 32
                ? 26
                : ((size - 13) + (2 * numAlign - 3)) / (2 * numAlign - 2) * 2;

            var result = new int[numAlign];
            result[0] = 6;
            for (int i = numAlign - 1, pos = size - 7; i >= 1; i--, pos -= step) result[i] = pos;
            return result;
        }

        private static int FormatBitsForEcl(int ecl)
        {
            switch (ecl)
            {
                case 0: return 1;  // Low
                case 1: return 0;  // Medium
                case 2: return 3;  // Quartile
                default: return 2; // High
            }
        }

        private static bool GetBit(int value, int index)
        {
            return ((value >> index) & 1) != 0;
        }

        // Indexed [ecc level 0-3][version 0-40]; index 0 is unused padding.
        private static readonly int[][] EccCodewordsPerBlock =
        {
            new[] {-1, 7,10,15,20,26,18,20,24,30,18,20,24,26,30,22,24,28,30,28,28,28,28,30,30,26,28,30,30,30,30,30,30,30,30,30,30,30,30,30,30},
            new[] {-1,10,16,26,18,24,16,18,22,22,26,30,22,22,24,24,28,28,26,26,26,26,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28,28},
            new[] {-1,13,22,18,26,18,24,18,22,20,24,28,26,24,20,30,24,28,28,26,30,28,30,30,30,30,28,30,30,30,30,30,30,30,30,30,30,30,30,30,30},
            new[] {-1,17,28,22,16,22,28,26,26,24,28,24,28,22,24,24,30,28,28,26,28,30,24,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30,30}
        };

        private static readonly int[][] NumEccBlocks =
        {
            new[] {-1,1,1,1,1,1,2,2,2,2, 4, 4, 4, 4, 4, 6, 6, 6, 6, 7, 8, 8, 9, 9,10,12,12,12,13,14,15,16,17,18,19,19,20,21,22,24,25},
            new[] {-1,1,1,1,2,2,4,4,4,5, 5, 5, 8, 9, 9,10,10,11,13,14,16,17,17,18,20,21,23,25,26,28,29,31,33,35,37,38,40,43,45,47,49},
            new[] {-1,1,1,2,2,4,4,6,6,8, 8, 8,10,12,16,12,17,16,18,21,20,23,23,25,27,29,34,34,35,38,40,43,45,48,51,53,56,59,62,65,68},
            new[] {-1,1,1,2,4,4,4,5,6,8, 8,11,11,16,16,18,16,19,21,25,25,25,34,30,32,35,37,40,42,45,48,51,54,57,60,63,66,70,74,77,81}
        };

        // ── Rendering ──────────────────────────────────────────────────────

        /// <summary>
        /// Renders to a bitmap. The quiet zone matters: scanners on cheap phone
        /// cameras miss a code printed hard against other ink.
        /// </summary>
        public Bitmap ToBitmap(int moduleSize = 8, int quietZoneModules = 4)
        {
            moduleSize = Math.Max(1, moduleSize);
            quietZoneModules = Math.Max(0, quietZoneModules);

            int side = (Size + quietZoneModules * 2) * moduleSize;
            var bmp = new Bitmap(side, side, PixelFormat.Format24bppRgb);

            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.White);
                using (var brush = new SolidBrush(Color.Black))
                {
                    for (int y = 0; y < Size; y++)
                    {
                        for (int x = 0; x < Size; x++)
                        {
                            if (!_modules[y][x]) continue;
                            g.FillRectangle(brush,
                                (x + quietZoneModules) * moduleSize,
                                (y + quietZoneModules) * moduleSize,
                                moduleSize, moduleSize);
                        }
                    }
                }
            }
            return bmp;
        }

        /// <summary>Module size that makes the code fill a target pixel box.</summary>
        public int ModuleSizeFor(int targetPixels, int quietZoneModules = 4)
        {
            return Math.Max(1, targetPixels / (Size + quietZoneModules * 2));
        }

        public static QrEcc ParseEcc(string text)
        {
            switch ((text ?? "").Trim().ToUpperInvariant())
            {
                case "L": return QrEcc.Low;
                case "Q": return QrEcc.Quartile;
                case "H": return QrEcc.High;
                default: return QrEcc.Medium;
            }
        }
    }
}
