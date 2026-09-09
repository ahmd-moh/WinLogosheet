using System;
using System.Globalization;
using System.Threading;

namespace SubstationOcrAgent
{
    /// <summary>
    /// Fires the hourly reading from inside the agent.
    ///
    /// V1 leaned on a Windows Task Scheduler job that had to be re-registered
    /// each day and silently stopped capturing when its end boundary passed. The
    /// agent is already resident, so it simply watches the clock: every 20
    /// seconds it asks whether the current hour is due and not yet stored. A
    /// reading missed because the machine was asleep or busy is taken as soon as
    /// the agent is running again, still filed under the hour it belongs to.
    /// </summary>
    public sealed class HourlyScheduler : IDisposable
    {
        private readonly AgentConfig _config;
        private readonly CaptureService _capture;
        private readonly HourStore _store;
        private readonly AgentLog _log;

        private Timer _timer;
        private int _busy;              // Interlocked flag: one capture at a time
        private string _lastSlot = "";  // "yyyy-MM-dd/HH" already handled

        public HourlyScheduler(AgentConfig config, CaptureService capture, HourStore store, AgentLog log)
        {
            _config = config;
            _capture = capture;
            _store = store;
            _log = log;
        }

        public void Start()
        {
            _timer = new Timer(Tick, null, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(20));
            _log.Info("Hourly scheduler armed for minute :" +
                      _config.CaptureMinute.ToString("00", CultureInfo.InvariantCulture) +
                      (_config.AutoCaptureEnabled ? "" : " (auto capture is switched off)"));
        }

        private void Tick(object state)
        {
            if (!_config.AutoCaptureEnabled) return;

            // Skip this tick if the previous capture is still running rather than
            // queueing up behind it — OCR of a dozen boxes can outlast one tick.
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return;

            try
            {
                DateTime now = DateTime.Now;
                if (now.Minute < _config.CaptureMinute) return;

                string sessionDate = LogsheetHours.SessionDate(now, _config.WorkdayStartHour);
                int hour = LogsheetHours.FromClock(now);
                string slot = sessionDate + "/" + hour.ToString("00", CultureInfo.InvariantCulture);

                if (slot == _lastSlot) return;
                if (_store.Has(sessionDate, hour)) { _lastSlot = slot; return; }

                _capture.CaptureNow(sessionDate, hour);
                _lastSlot = slot;

                // Housekeeping once a day, on the first capture of the workday.
                if (hour == _config.WorkdayStartHour)
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

        public void Dispose()
        {
            if (_timer != null) { _timer.Dispose(); _timer = null; }
        }
    }
}
