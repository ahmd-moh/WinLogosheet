using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SubstationOcr
{
    /// <summary>
    /// The global QR hotkey: hold Ctrl+Shift and press 7, then 8, then 9.
    ///
    /// This is the mechanism the previous version proved on site, kept as-is.
    /// RegisterHotKey binds modifiers plus exactly one key, so the combination
    /// is registered as three separate hotkeys and turned into a sequence: 7
    /// arms, 8 confirms, 9 fires, each press within three seconds of the last.
    /// 7 always restarts the sequence; anything out of order resets it.
    ///
    /// That is deliberately better than a low-level keyboard hook here — a hook
    /// sits in the input path of the whole SCADA desktop, and this does not.
    ///
    /// Ctrl rather than Alt on purpose: Alt+Shift is the input-language toggle
    /// on Arabic systems.
    ///
    /// The window is created but never shown; it exists only to own the hotkey
    /// registrations and receive WM_HOTKEY.
    /// </summary>
    public sealed class HotkeySink : Form
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_CONTROL = 0x0002, MOD_SHIFT = 0x0004, MOD_NOREPEAT = 0x4000;
        private const int WM_HOTKEY = 0x0312;
        private const int HK_ID_BASE = 0x4C00;   // hotkey ids are base + virtual key

        /// <summary>Top-row 7/8/9 and numpad 7/8/9 both drive the sequence.</summary>
        private static readonly uint[] Keys = { 0x37, 0x38, 0x39, 0x67, 0x68, 0x69 };

        /// <summary>Each press must land within this long of the previous one.</summary>
        private const double StepSeconds = 3;

        private int _stage;
        private DateTime _last;

        /// <summary>Raised when 7 → 8 → 9 completes.</summary>
        public event Action Completed;

        /// <summary>Diagnostics — every press, and why a sequence reset.</summary>
        public event Action<string> Trace;

        /// <summary>True when 7, 8 and 9 each have at least one key registered —
        /// without all three the sequence can never complete.</summary>
        public bool Armed { get; private set; }

        public HotkeySink()
        {
            // No chrome, no taskbar entry, zero size: this is a message sink.
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            Location = new System.Drawing.Point(-32000, -32000);
            Size = new System.Drawing.Size(1, 1);
        }

        /// <summary>The sink must never become visible, whoever calls Show().</summary>
        protected override void SetVisibleCore(bool value)
        {
            base.SetVisibleCore(false);
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Register();   // re-registers if Windows ever recreates the handle
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            Unregister();
            base.OnHandleDestroyed(e);
        }

        private void Register()
        {
            var digits = new HashSet<int>();

            foreach (uint vk in Keys)
            {
                if (RegisterHotKey(Handle, HK_ID_BASE + (int)vk, MOD_CONTROL | MOD_SHIFT | MOD_NOREPEAT, vk))
                {
                    digits.Add(DigitOf((int)vk));
                    continue;
                }

                Report("RegisterHotKey failed for virtual key 0x" + vk.ToString("X2") +
                       " — another program may already own Ctrl+Shift+that key.");
            }

            Armed = digits.Count == 3;
        }

        /// <summary>Numpad or top row, 0x67 and 0x37 are both 7.</summary>
        private static int DigitOf(int vk)
        {
            return vk >= 0x60 ? vk - 0x60 : vk - 0x30;
        }

        private void Unregister()
        {
            foreach (uint vk in Keys) UnregisterHotKey(Handle, HK_ID_BASE + (int)vk);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_HOTKEY) Advance(m.WParam.ToInt32());
            base.WndProc(ref m);
        }

        private void Advance(int id)
        {
            int digit = DigitOf(id - HK_ID_BASE);

            bool timely = (DateTime.UtcNow - _last).TotalSeconds <= StepSeconds;
            _last = DateTime.UtcNow;

            int before = _stage;
            if (digit == 7) _stage = 1;
            else if (digit == 8 && before == 1 && timely) _stage = 2;
            else if (digit == 9 && before == 2 && timely) _stage = 3;
            else _stage = 0;

            string note = "";
            if (_stage == 0 && before > 0)
                note = timely ? "  (out of order — restart from 7)"
                              : "  (too slow, over " + StepSeconds + " s — restart from 7)";

            Report("Hotkey Ctrl+Shift+" + digit + "  stage " + before + " to " + _stage + note);

            if (_stage != 3) return;

            _stage = 0;
            Action handler = Completed;
            if (handler != null) handler();
        }

        private void Report(string message)
        {
            Action<string> handler = Trace;
            if (handler != null) handler(message);
        }
    }
}
