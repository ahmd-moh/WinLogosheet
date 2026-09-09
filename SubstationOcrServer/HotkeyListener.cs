using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace SubstationOcrServer
{
    /// <summary>
    /// Watches for Ctrl+Shift+7+8+9 anywhere on the desktop.
    ///
    /// Windows' RegisterHotKey binds modifiers plus exactly ONE key, so a
    /// three-digit combination cannot be registered that way. This uses a
    /// low-level keyboard hook instead and tracks which keys are down.
    ///
    /// It accepts the combination two ways, because "7+8+9 at once" is not
    /// reliable on every keyboard — many number rows ghost the third
    /// simultaneous digit:
    ///
    ///   * all three digits held down together while Ctrl+Shift are held, or
    ///   * 7 then 8 then 9 pressed in order while Ctrl+Shift stay held.
    ///
    /// Either way the operator holds Ctrl+Shift and touches 7, 8, 9. Releasing
    /// a modifier resets the sequence, so a stray 7 in normal typing cannot
    /// creep toward a match.
    /// </summary>
    public sealed class HotkeyListener : IDisposable
    {
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;

        // The hook callback must not be collected while Windows holds a pointer
        // to it — keeping the delegate in a field is what prevents that.
        private readonly LowLevelKeyboardProc _callback;
        private IntPtr _hook = IntPtr.Zero;

        private readonly HashSet<int> _down = new HashSet<int>();
        private readonly List<int> _sequence = new List<int>();
        private DateTime _lastFired = DateTime.MinValue;

        /// <summary>The digits, in the order they must be pressed.</summary>
        private static readonly int[] Digits = { (int)Keys.D7, (int)Keys.D8, (int)Keys.D9 };

        /// <summary>Ignore a repeat within this window, so holding the keys down
        /// does not re-trigger while the QR is still on screen.</summary>
        private static readonly TimeSpan Debounce = TimeSpan.FromSeconds(2);

        public event Action Triggered;

        public HotkeyListener()
        {
            _callback = HookCallback;
        }

        public void Start()
        {
            using (Process process = Process.GetCurrentProcess())
            using (ProcessModule module = process.MainModule)
                _hook = SetWindowsHookEx(WH_KEYBOARD_LL, _callback, GetModuleHandle(module.ModuleName), 0);

            if (_hook == IntPtr.Zero)
                throw new InvalidOperationException(
                    "Could not install the keyboard hook (error " + Marshal.GetLastWin32Error() + ").");
        }

        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int message = wParam.ToInt32();
                int key = Marshal.ReadInt32(lParam);   // vkCode is the first field of KBDLLHOOKSTRUCT

                if (message == WM_KEYDOWN || message == WM_SYSKEYDOWN) OnKeyDown(key);
                else if (message == WM_KEYUP || message == WM_SYSKEYUP) OnKeyUp(key);
            }

            // Never swallow the keystroke: this is a SCADA desktop, and a hook
            // that eats input would be far worse than a missed QR code.
            return CallNextHookEx(_hook, nCode, wParam, lParam);
        }

        private void OnKeyDown(int key)
        {
            _down.Add(key);

            if (!ModifiersHeld())
            {
                _sequence.Clear();
                return;
            }

            if (Array.IndexOf(Digits, key) < 0) return;

            // Sequence form: 7, then 8, then 9, in order.
            int expected = _sequence.Count < Digits.Length ? Digits[_sequence.Count] : -1;
            if (key == expected) _sequence.Add(key);
            else if (key == Digits[0]) { _sequence.Clear(); _sequence.Add(key); }
            else _sequence.Clear();

            bool sequenceComplete = _sequence.Count == Digits.Length;
            bool allHeldTogether = AllDigitsDown();

            if (sequenceComplete || allHeldTogether) Fire();
        }

        private void OnKeyUp(int key)
        {
            _down.Remove(key);
            if (!ModifiersHeld()) _sequence.Clear();
        }

        private bool ModifiersHeld()
        {
            bool ctrl = _down.Contains((int)Keys.LControlKey) || _down.Contains((int)Keys.RControlKey) ||
                        _down.Contains((int)Keys.ControlKey);
            bool shift = _down.Contains((int)Keys.LShiftKey) || _down.Contains((int)Keys.RShiftKey) ||
                         _down.Contains((int)Keys.ShiftKey);
            return ctrl && shift;
        }

        private bool AllDigitsDown()
        {
            foreach (int digit in Digits)
                if (!_down.Contains(digit)) return false;
            return true;
        }

        private void Fire()
        {
            _sequence.Clear();

            DateTime now = DateTime.UtcNow;
            if (now - _lastFired < Debounce) return;
            _lastFired = now;

            Action handler = Triggered;
            if (handler == null) return;

            // The hook runs on the low-level input thread; anything slow here
            // stalls the whole desktop's keyboard, so hand off and return.
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { handler(); }
                catch { /* the caller logs; the hook must never throw */ }
            });
        }

        public void Dispose()
        {
            if (_hook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hook);
                _hook = IntPtr.Zero;
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn,
                                                      IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
    }
}
