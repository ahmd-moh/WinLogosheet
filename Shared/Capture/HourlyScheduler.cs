using System;
using System.Globalization;
using System.Threading;
using Substation.Shared;

namespace Substation.Capture
{
    /// <summary>
    /// Drives the hourly reading inside the 07:00 → 07:00 run window.
    ///
    /// The session is pinned when the node starts and never recomputed, so a
    /// process left running past 07:00 goes quiet rather than rolling silently
    /// into the next day. Starting the next session is a manual act.
    ///
    /// Inside the window it simply watches the clock: every 20 seconds it asks
    /// whether the current hour is due and not yet stored. A reading missed
    /// because the machine was busy is taken as soon as the node is running
    /// again, still filed under the hour it belongs to.
    /// </summary>
    public sealed class HourlyScheduler : IDisposable
    {
        private readonly NodeConfig _config;
        private readonly CaptureService _capture;
        private readonly HourStore _store;
        private readonly NodeLog _log;

        private Timer _timer;
        private int _busy;              // Interlocked flag: one capture at a time
        private string _lastSlot = "";
        private bool _sessionClosed;

        /// <summary>Raised after each stored reading. The client uses it to push
        /// the values on to the server node.</summary>
        public event Action<ReadingFrame> ReadingTaken;

        /// <summary>Raised once, when the 07:00 window closes.</summary>
        public event Action SessionEnded;

        public HourlyScheduler(NodeConfig config, CaptureService capture, HourStore store, NodeLog log)
        {
            _config = config;
            _capture = capture;
            _store = store;
            _log = log;

            SessionDate = SessionClock.SessionDateOf(DateTime.Now);
        }

        /// <summary>The session this node is serving, pinned at start-up.</summary>
        public DateTime SessionDate { get; private set; }

        public string SessionDateString { get { return SessionClock.Format(SessionDate); } }

        public bool SessionClosed { get { return _sessionClosed; } }

        public void Start()
        {
            _log.Info(string.Format(CultureInfo.InvariantCulture,
                "Session {0}: reading at :{1:00} of every hour from {2:HH:mm} until {3:yyyy-MM-dd HH:mm}",
                SessionDateString, _config.CaptureMinute,
                SessionClock.StartOf(SessionDate), SessionClock.EndOf(SessionDate)));

            _timer = new Timer(Tick, null, TimeSpan.FromSeconds(3), TimeSpan.FromSeconds(20));
        }

        /// <summary>Reads the current hour on demand, ignoring the slot guard.</summary>
        public ReadingFrame CaptureNow()
        {
            DateTime now = DateTime.Now;
            ReadingFrame frame = _capture.CaptureNow(SessionDateString, now.Hour);
            Announce(frame);
            return frame;
        }

        private void Tick(object state)
        {
            // Skip this tick if the previous capture is still running rather than
            // queueing behind it — OCR of a dozen boxes can outlast one tick.
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return;

            try
            {
                DateTime now = DateTime.Now;

                if (!SessionClock.IsInside(SessionDate, now))
                {
                    CloseSession();
                    return;
                }

                if (now.Minute < _config.CaptureMinute) return;

                int hour = now.Hour;
                string slot = SessionDateString + "/" + hour.ToString("00", CultureInfo.InvariantCulture);
                if (slot == _lastSlot) return;

                if (_store.Has(SessionDateString, hour, _config.NodeId)) { _lastSlot = slot; return; }

                ReadingFrame frame = _capture.CaptureNow(SessionDateString, hour);
                _lastSlot = slot;
                Announce(frame);

                // Housekeeping once per session, on the first reading of the day.
                if (hour == SessionClock.SessionStartHour)
                {
                    int removed = _store.Prune(_config.RetentionDays);
                    _log.Prune(_config.RetentionDays);
                    if (removed > 0) _log.Info("Pruned " + removed + " expired reading folder(s).");
                }
            }
            catch (Exception ex)
            {
                _log.Error("Scheduler tick failed: " + ex);
            }
            finally
            {
                Interlocked.Exchange(ref _busy, 0);
            }
        }

        private void CloseSession()
        {
            if (_sessionClosed) return;
            _sessionClosed = true;

            _log.Info("Session " + SessionDateString + " closed at 07:00. " +
                      "No further readings will be taken until this node is started again.");

            Action handler = SessionEnded;
            if (handler != null) handler();
        }

        private void Announce(ReadingFrame frame)
        {
            Action<ReadingFrame> handler = ReadingTaken;
            if (handler == null || frame == null) return;

            try { handler(frame); }
            catch (Exception ex) { _log.Error("Reading handler failed: " + ex.Message); }
        }

        public void Dispose()
        {
            if (_timer != null) { _timer.Dispose(); _timer = null; }
        }
    }
}
