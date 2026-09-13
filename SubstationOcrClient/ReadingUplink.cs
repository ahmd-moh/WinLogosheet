using System;
using System.Collections.Generic;
using System.Threading;
using Substation.Capture;
using Substation.Shared;

namespace SubstationOcrClient
{
    /// <summary>
    /// Pushes each hour's values up to the 132 kV server node, and is the only
    /// path the other direction too.
    ///
    /// A push that fails is not lost: the reading is already on this node's own
    /// disk, and the hour is queued. Every retry interval the uplink drains
    /// whatever the server has not acknowledged, so a link that comes back at
    /// 03:00 still delivers the whole night.
    ///
    /// Between drains it polls. Only this node can open a socket, so anything
    /// the 132 kV seat wants done here — today that is "show me your boxes" —
    /// waits on the server until the poll collects it.
    /// </summary>
    public sealed class ReadingUplink : IDisposable
    {
        private readonly ClientConfig _config;
        private readonly HourStore _store;
        private readonly NodeLog _log;

        private readonly object _gate = new object();
        private readonly HashSet<int> _pending = new HashSet<int>();
        private readonly HashSet<int> _delivered = new HashSet<int>();

        private Timer _retryTimer;
        private Timer _pollTimer;
        private string _sessionDate = "";
        private int _busy;
        private int _polling;

        /// <summary>Turned off for the rest of the run when the server turns out
        /// not to speak the command half of the protocol.</summary>
        private bool _pollSupported = true;

        private string _lastPollError = "";
        private DateTime _lastPollErrorAt;

        public ReadingUplink(ClientConfig config, HourStore store, NodeLog log)
        {
            _config = config;
            _store = store;
            _log = log;
        }

        /// <summary>
        /// One instruction collected from the server node. Raised on a worker
        /// thread, and one at a time: the poll that collected it is still
        /// running, so a long job here simply delays the next poll.
        /// </summary>
        public event Action<RemoteCommand> CommandReceived;

        public DateTime LastDeliveryLocal { get; private set; }
        public string LastError { get; private set; }

        public int PendingCount { get { lock (_gate) return _pending.Count; } }

        public void Start(string sessionDate)
        {
            _sessionDate = sessionDate;

            // Anything already on disk but never acknowledged goes into the queue,
            // so a node restarted mid-session still delivers what it missed.
            foreach (ReadingFrame frame in _store.LoadDay(sessionDate, _config.NodeId))
                lock (_gate) _pending.Add(frame.Hour);

            _retryTimer = new Timer(_ => Drain(), null,
                                    TimeSpan.FromSeconds(5),
                                    TimeSpan.FromSeconds(_config.RetrySeconds));

            _log.Info("Uplink to " + _config.ServerHost + ":" + _config.ServerPort +
                      " armed, retrying every " + _config.RetrySeconds + " s.");

            if (_config.PollSeconds <= 0)
            {
                _pollSupported = false;
                _log.Info("pollSeconds is 0 — this node will not collect commands from the server, " +
                          "so calibration cannot be requested from the 132 kV seat.");
                return;
            }

            _pollTimer = new Timer(_ => Poll(), null,
                                   TimeSpan.FromSeconds(8),
                                   TimeSpan.FromSeconds(_config.PollSeconds));

            _log.Info("Command channel armed: asking the server node for work every " +
                      _config.PollSeconds + " s.");
        }

        /// <summary>Queue an hour and try to deliver it straight away.</summary>
        public void Send(ReadingFrame frame)
        {
            if (frame == null) return;

            lock (_gate)
            {
                _delivered.Remove(frame.Hour);
                _pending.Add(frame.Hour);
            }
            Drain();
        }

        /// <summary>Delivers every queued hour in one connection, then asks for work.</summary>
        public void Drain()
        {
            if (Interlocked.CompareExchange(ref _busy, 1, 0) != 0) return;

            try
            {
                List<int> hours;
                lock (_gate)
                {
                    if (_pending.Count == 0) return;
                    hours = new List<int>(_pending);
                }
                hours.Sort((a, b) => SessionClock.Order(a).CompareTo(SessionClock.Order(b)));

                var frames = new List<object>();
                var offered = new List<int>();
                foreach (int hour in hours)
                {
                    ReadingFrame frame = _store.Load(_sessionDate, hour, _config.NodeId);
                    if (frame == null) { lock (_gate) _pending.Remove(hour); continue; }
                    frames.Add(frame.ToJson());
                    offered.Add(hour);
                }
                if (frames.Count == 0) return;

                List<int> accepted = PushBatch(frames);

                lock (_gate)
                {
                    foreach (int hour in accepted)
                    {
                        _pending.Remove(hour);
                        _delivered.Add(hour);
                    }
                }

                LastDeliveryLocal = DateTime.Now;
                LastError = "";

                _log.Info("Delivered " + accepted.Count + " of " + offered.Count +
                          " queued hour(s) to the server node.");
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                _log.Warn("Uplink failed (" + PendingCount + " hour(s) still queued): " + ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _busy, 0);
            }
        }

        private List<int> PushBatch(List<object> frames)
        {
            using (ServerLink link = ServerLink.Open(_config))
            {
                Dictionary<string, object> request = AgentProtocol.Request(AgentProtocol.CmdPushBatch);
                request["nodeId"] = _config.NodeId;
                request["sessionDate"] = _sessionDate;
                request["frames"] = frames;

                Dictionary<string, object> response = link.Ask(request);

                var accepted = new List<int>();
                foreach (object hour in Json.List(response, "accepted"))
                {
                    int parsed;
                    if (int.TryParse(Convert.ToString(hour), out parsed)) accepted.Add(parsed);
                }

                // The connection is open and the server is listening: ask for
                // work here rather than paying for a second one seconds later.
                //
                // Separately guarded, because the hours have already been
                // accepted at this point: a command channel that fails must not
                // make the caller queue them all again.
                try { Collect(link); }
                catch (Exception ex) { NotePollFailure(ex.Message); }

                return accepted;
            }
        }

        // -- The reverse channel --------------------------------------------

        /// <summary>Asks the server node whether it has anything for this node.</summary>
        public void Poll()
        {
            if (!_pollSupported) return;
            if (Interlocked.CompareExchange(ref _polling, 1, 0) != 0) return;

            try
            {
                using (ServerLink link = ServerLink.Open(_config))
                    Collect(link);

                if (_lastPollError.Length > 0)
                {
                    _log.Info("Command channel back: the server node is answering again.");
                    _lastPollError = "";
                }
            }
            catch (InvalidOperationException ex)
            {
                // A refusal, not a broken link. An older server that has never
                // heard of "poll" would say so every interval for the rest of
                // the night, so it is said once and the channel stood down.
                if (ex.Message.IndexOf("Unknown command", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    _pollSupported = false;
                    _log.Warn("The server node does not accept 'poll' (" + ex.Message +
                              ") — it is running an older build. Calibration cannot be requested " +
                              "from the 132 kV seat until both nodes are updated.");
                    return;
                }
                NotePollFailure(ex.Message);
            }
            catch (Exception ex)
            {
                NotePollFailure(ex.Message);
            }
            finally
            {
                Interlocked.Exchange(ref _polling, 0);
            }
        }

        /// <summary>
        /// Collects whatever the server parked for this node and runs it. The
        /// link is handed in because the caller may already have one open.
        /// </summary>
        private void Collect(ServerLink link)
        {
            if (!_pollSupported) return;

            Dictionary<string, object> request = AgentProtocol.Request(AgentProtocol.CmdPoll);
            request["nodeId"] = _config.NodeId;

            Dictionary<string, object> response = link.Ask(request);

            var commands = new List<RemoteCommand>();
            foreach (object node in Json.List(response, "commands"))
                commands.Add(RemoteCommand.FromJson(Json.Dict(node)));

            if (commands.Count == 0) return;

            Action<RemoteCommand> handler = CommandReceived;
            foreach (RemoteCommand command in commands)
            {
                _log.Info("The server node asked for '" + command.Command + "' (request " + command.Id + ").");

                if (handler == null)
                {
                    _log.Warn("Nothing on this node handles '" + command.Command + "'.");
                    continue;
                }

                try { handler(command); }
                catch (Exception ex) { _log.Error("Command '" + command.Command + "' failed here: " + ex); }
            }
        }

        /// <summary>
        /// Sends a calibration shot up to the server node, where the engineer
        /// who asked for it is sitting.
        /// </summary>
        public void SendCalibration(CalibrationShot shot, string requestId)
        {
            if (shot == null) return;

            try
            {
                using (ServerLink link = ServerLink.Open(_config))
                {
                    Dictionary<string, object> request = AgentProtocol.Request(AgentProtocol.CmdCalibration);
                    request["nodeId"] = _config.NodeId;
                    request["requestId"] = requestId ?? "";
                    request["shot"] = shot.ToJson();

                    link.Ask(request);
                }

                _log.Info("Calibration sent to the server node (" +
                          (shot.Image == null ? 0 : shot.Image.Length / 1024) + " KB " +
                          shot.ImageFormat + "): " + shot.Summary());
            }
            catch (Exception ex)
            {
                _log.Warn("Could not send the calibration to the server node: " + ex.Message +
                          " The shot is still on this node under Calibration\\.");
            }
        }

        /// <summary>
        /// Keeps a link that is down from filling the day's log. The first
        /// failure is worth a line; the same failure ten seconds later is not.
        /// </summary>
        private void NotePollFailure(string message)
        {
            bool sameAsLast = string.Equals(message, _lastPollError, StringComparison.Ordinal);
            if (sameAsLast && DateTime.Now - _lastPollErrorAt < TimeSpan.FromMinutes(10)) return;

            _lastPollError = message;
            _lastPollErrorAt = DateTime.Now;
            _log.Warn("Command channel: " + message);
        }

        public void Dispose()
        {
            if (_retryTimer != null) { _retryTimer.Dispose(); _retryTimer = null; }
            if (_pollTimer != null) { _pollTimer.Dispose(); _pollTimer = null; }
        }
    }
}
