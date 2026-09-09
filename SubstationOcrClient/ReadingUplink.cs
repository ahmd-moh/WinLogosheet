using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Substation.Capture;
using Substation.Shared;

namespace SubstationOcrClient
{
    /// <summary>
    /// Pushes each hour's values up to the 132 kV server node.
    ///
    /// A push that fails is not lost: the reading is already on this node's own
    /// disk, and the hour is queued. Every retry interval the uplink drains
    /// whatever the server has not acknowledged, so a link that comes back at
    /// 03:00 still delivers the whole night.
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
        private string _sessionDate = "";
        private int _busy;

        public ReadingUplink(ClientConfig config, HourStore store, NodeLog log)
        {
            _config = config;
            _store = store;
            _log = log;
        }

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

        /// <summary>Delivers every queued hour in one connection.</summary>
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
            using (var client = new TcpClient())
            {
                IAsyncResult connecting = client.BeginConnect(_config.ServerHost, _config.ServerPort, null, null);
                if (!connecting.AsyncWaitHandle.WaitOne(_config.TimeoutMs))
                    throw new TimeoutException("No answer from " + _config.ServerHost + ":" +
                                               _config.ServerPort + " within " +
                                               (_config.TimeoutMs / 1000) + " s.");
                client.EndConnect(connecting);

                client.NoDelay = true;
                client.ReceiveTimeout = _config.TimeoutMs;
                client.SendTimeout = _config.TimeoutMs;

                using (NetworkStream raw = client.GetStream())
                using (var stream = new BufferedStream(raw, 64 * 1024))
                {
                    Dictionary<string, object> request = AgentProtocol.Request(AgentProtocol.CmdPushBatch);
                    request["nodeId"] = _config.NodeId;
                    request["sessionDate"] = _sessionDate;
                    request["frames"] = frames;

                    AgentProtocol.WriteMessage(stream, request, _config.SharedSecret);

                    Dictionary<string, object> response = AgentProtocol.ReadMessage(stream, _config.SharedSecret);
                    if (response == null) throw new IOException("The server node closed the connection without answering.");
                    if (!Json.Bool(response, "ok", false))
                        throw new InvalidOperationException(Json.Str(response, "error", "The server node refused the push."));

                    var accepted = new List<int>();
                    foreach (object hour in Json.List(response, "accepted"))
                    {
                        int parsed;
                        if (int.TryParse(Convert.ToString(hour), out parsed)) accepted.Add(parsed);
                    }
                    return accepted;
                }
            }
        }

        public void Dispose()
        {
            if (_retryTimer != null) { _retryTimer.Dispose(); _retryTimer = null; }
        }
    }
}
