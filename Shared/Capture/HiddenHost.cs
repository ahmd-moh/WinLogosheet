using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Substation.Capture
{
    /// <summary>
    /// Runs a node with no window at all.
    ///
    /// Both nodes are meant to be invisible on the SCADA desktop: no form, no
    /// taskbar entry and, by default, no tray icon either. Everything they do
    /// goes to the day's log file. A tray icon can be switched on in the config
    /// while commissioning, when someone on site needs to force a reading or
    /// write the ROI overlay without opening a remote session.
    /// </summary>
    public sealed class HiddenHost : ApplicationContext
    {
        private readonly NodeConfig _config;
        private readonly NodeLog _log;
        private readonly string _product;

        private NotifyIcon _icon;

        /// <summary>Extra tray entries the node adds (label, action).</summary>
        public event Action<ContextMenuStrip> BuildingMenu;

        public HiddenHost(NodeConfig config, NodeLog log, string product)
        {
            _config = config;
            _log = log;
            _product = product;
        }

        /// <summary>
        /// Call after wiring BuildingMenu. The tray is built here rather than in
        /// the constructor so a node's own menu entries are in place first.
        /// </summary>
        public void Start()
        {
            if (_config.ShowTrayIcon) BuildTray();
        }

        private void BuildTray()
        {
            var menu = new ContextMenuStrip();

            Action<ContextMenuStrip> builder = BuildingMenu;
            if (builder != null) builder(menu);

            if (menu.Items.Count > 0) menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Open node folder", null, (s, e) => OpenFolder());
            menu.Items.Add("Open today's log", null, (s, e) => OpenLog());
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Exit", null, (s, e) => ExitThread());

            _icon = new NotifyIcon
            {
                Icon = SystemIcons.Application,
                Visible = true,
                ContextMenuStrip = menu,
                Text = Truncate(_product + " - " + _config.NodeId)
            };
        }

        // The shell caps the tray tooltip at 63 characters.
        private static string Truncate(string text)
        {
            return text.Length <= 63 ? text : text.Substring(0, 60) + "...";
        }

        public void Notify(string title, string text, bool error)
        {
            if (_icon == null) return;

            _icon.BalloonTipTitle = title;
            _icon.BalloonTipText = text;
            _icon.BalloonTipIcon = error ? ToolTipIcon.Error : ToolTipIcon.Info;
            _icon.ShowBalloonTip(5000);
        }

        private void OpenFolder()
        {
            try { Process.Start(_config.BaseFolder); }
            catch (Exception ex) { _log.Error("Could not open the node folder: " + ex.Message); }
        }

        private void OpenLog()
        {
            try
            {
                string path = System.IO.Path.Combine(_config.ResolvePath(_config.LogFolder),
                    "node-" + DateTime.Now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) + ".log");
                if (System.IO.File.Exists(path)) Process.Start(path);
            }
            catch (Exception ex) { _log.Error("Could not open the log: " + ex.Message); }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _icon != null)
            {
                _icon.Visible = false;
                _icon.Dispose();
                _icon = null;
            }
            base.Dispose(disposing);
        }
    }
}
