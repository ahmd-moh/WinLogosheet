using System;
using System.Threading;
using System.Windows.Forms;

namespace WinLogosheet
{
    internal static class Program
    {
        // Held for the whole process lifetime so a second copy exits
        // immediately instead of fighting over the global hotkeys.
        private static Mutex _singleInstance;

        /// <summary>
        /// The main entry point for the application.
        /// HEADLESS — no window is ever shown. Capture + OCR keep running in
        /// the background and Ctrl+Shift+7,8,9 toggles a fullscreen QR code on
        /// the second screen (TV).
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            _singleInstance = new Mutex(true,
                "WinLogosheet_Headless_SingleInstance", out bool createdNew);
            if (!createdNew)
            {
                // Leave a trace: with logon autostart an old copy is often
                // already running, and this exit is otherwise invisible.
                try
                {
                    System.IO.File.AppendAllText(
                        System.IO.Path.Combine(Application.StartupPath, "headless.log"),
                        $"{DateTime.Now:yyyy-MM-dd HH:mm:ss}  Start blocked — another " +
                        "instance is already running (kill it in Task Manager first)\r\n");
                }
                catch { }
                return;
            }

            // Create the engine form but never show it. Forcing the handle is
            // required: the global hotkeys and the background timers all hang
            // off this hidden window.
            var engine = new Form1(headless: true);
            IntPtr forceHandle = engine.Handle;
            GC.KeepAlive(forceHandle);

            // Message loop with no visible form; runs until the process is killed.
            Application.Run();
        }
    }
}
