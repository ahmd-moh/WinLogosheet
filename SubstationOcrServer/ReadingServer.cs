using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Substation.Capture;
using Substation.Shared;

namespace SubstationOcrServer
{
    /// <summary>
    /// The socket half of the 132 kV node: a line-oriented JSON server that the
    /// 33 kV client pushes its hourly values to. Values cross the wire, never
    /// screen captures, so an hour costs about two kilobytes.
    /// </summary>
    public sealed class ReadingServer : IDisposable
    {
        private readonly ServerConfig _config;
        private readonly MergedStore _merged;
        private readonly HourStore _store;
        private readonly CaptureService _capture;
        private readonly NodeLog _log;

        private readonly CommandQueue _commands = new CommandQueue();
        private readonly HashSet<string> _polled = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private TcpListener _listener;
        private Thread _acceptThread;
        private volatile bool _running;

        public int ConnectionsServed { get; private set; }
        public DateTime LastPushLocal { get; private set; }

        /// <summary>
        /// A calibration a client node sent back. Raised on the connection's own
        /// thread — whoever shows it has to cross onto the UI thread first.
        /// </summary>
        public event Action<CalibrationShot> CalibrationReceived;

        public ReadingServer(ServerConfig config, MergedStore merged, HourStore store,
                             CaptureService capture, NodeLog log)
        {
            _config = config;
            _merged = merged;
            _store = store;
            _capture = capture;
            _log = log;
        }

        public void Start()
        {
            IPAddress address;
            if (!IPAddress.TryParse(_config.ListenAddress, out address)) address = IPAddress.Any;

            _listener = new TcpListener(address, _config.Port);
            _listener.Start();
            _running = true;

            _acceptThread = new Thread(AcceptLoop) { IsBackground = true, Name = "ReadingServer.Accept" };
            _acceptThread.Start();

            _log.Info(string.Format(CultureInfo.InvariantCulture,
                "Listening on {0}:{1} as '{2}'", address, _config.Port, _config.NodeId));

            if (string.IsNullOrEmpty(_config.SharedSecret))
                _log.Warn("No sharedSecret configured — pushes are not authenticated. " +
                          "Set the same secret here and in client.config.json.");
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                TcpClient client;
                try
                {
                    client = _listener.AcceptTcpClient();
                }
                catch (SocketException)
                {
                    if (_running) Thread.Sleep(250);
                    continue;
                }
                catch (ObjectDisposedException)
                {
                    return;
                }

                TcpClient captured = client;
                new Thread(() => ServeClient(captured))
                {
                    IsBackground = true,
                    Name = "ReadingServer.Client"
                }.Start();
            }
        }

        private void ServeClient(TcpClient client)
        {
            string peer = "unknown";
            try
            {
                var endpoint = client.Client.RemoteEndPoint as IPEndPoint;
                if (endpoint != null) peer = endpoint.Address.ToString();

                if (!IsAllowed(peer))
                {
                    _log.Warn("Refused connection from " + peer + " (not in allowedClients).");
                    return;
                }

                ConnectionsServed++;
                client.NoDelay = true;
                client.ReceiveTimeout = 60000;
                client.SendTimeout = 60000;

                using (NetworkStream raw = client.GetStream())
                using (var stream = new BufferedStream(raw, 64 * 1024))
                {
                    while (true)
                    {
                        Dictionary<string, object> request;
                        try
                        {
                            request = AgentProtocol.ReadMessage(stream, _config.SharedSecret);
                        }
                        catch (IOException ex)
                        {
                            _log.Warn("Dropping " + peer + ": " + ex.Message);
                            return;
                        }

                        if (request == null) return; // peer closed cleanly

                        AgentProtocol.WriteMessage(stream, Handle(request, peer), _config.SharedSecret);
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Warn("Connection from " + peer + " ended: " + ex.Message);
            }
            finally
            {
                try { client.Close(); } catch { }
            }
        }

        private bool IsAllowed(string peer)
        {
            if (_config.AllowedClients == null || _config.AllowedClients.Count == 0) return true;

            foreach (string allowed in _config.AllowedClients)
                if (string.Equals(allowed.Trim(), peer, StringComparison.OrdinalIgnoreCase)) return true;
            return false;
        }

        // ── Command dispatch ───────────────────────────────────────────────

        private Dictionary<string, object> Handle(Dictionary<string, object> request, string peer)
        {
            string command = Json.Str(request, "cmd").ToLowerInvariant();

            try
            {
                switch (command)
                {
                    case AgentProtocol.CmdHello: return Hello();
                    case AgentProtocol.CmdStatus: return Status();
                    case AgentProtocol.CmdPush: return Push(request, peer);
                    case AgentProtocol.CmdPushBatch: return PushBatch(request, peer);
                    case AgentProtocol.CmdHistory: return History(request);
                    case AgentProtocol.CmdPoll: return Poll(request, peer);
                    case AgentProtocol.CmdCalibration: return Calibration(request, peer);
                    default: return AgentProtocol.Fail("Unknown command '" + command + "'.");
                }
            }
            catch (Exception ex)
            {
                _log.Error("Command '" + command + "' from " + peer + " failed: " + ex);
                return AgentProtocol.Fail(ex.Message);
            }
        }

        private Dictionary<string, object> Hello()
        {
            RoiConfig rois = _capture.Rois;

            var inventory = new List<object>();
            foreach (RoiDefinition roi in rois.Rois)
            {
                var channels = new List<object>();
                foreach (string key in roi.ChannelKeys()) channels.Add(key);

                inventory.Add(new Dictionary<string, object>
                {
                    { "id", roi.Id }, { "label", roi.Label }, { "enabled", roi.Enabled },
                    { "rows", roi.Rows }, { "channels", channels }
                });
            }

            return AgentProtocol.Ok(new Dictionary<string, object>
            {
                { "serverId", _config.NodeId },
                { "displayName", _config.DisplayName },
                { "agentVersion", Program.Version },
                { "machine", Environment.MachineName },
                { "captureScreen", ScreenGrabber.Describe(_config.CaptureScreen) },
                { "captureMinute", _config.CaptureMinute },
                { "referenceWidth", rois.ReferenceWidth },
                { "referenceHeight", rois.ReferenceHeight },
                { "rois", inventory }
            });
        }

        private Dictionary<string, object> Status()
        {
            return AgentProtocol.Ok(new Dictionary<string, object>
            {
                { "serverId", _config.NodeId },
                { "agentVersion", Program.Version },
                { "localTime", DateTime.Now.ToString("o", CultureInfo.InvariantCulture) },
                { "lastCapture", _capture.LastCaptureLocal == default(DateTime)
                                 ? "" : _capture.LastCaptureLocal.ToString("o", CultureInfo.InvariantCulture) },
                { "lastPush", LastPushLocal == default(DateTime)
                              ? "" : LastPushLocal.ToString("o", CultureInfo.InvariantCulture) },
                { "lastError", _capture.LastError ?? "" },
                { "connectionsServed", ConnectionsServed },
                { "commandsWaiting", _commands.WaitingCount }
            });
        }

        private Dictionary<string, object> Push(Dictionary<string, object> request, string peer)
        {
            ReadingFrame frame = ReadingFrame.FromJson(Json.Dict(request, "frame"));
            int accepted = Store(frame, peer) ? 1 : 0;

            return AgentProtocol.Ok(new Dictionary<string, object>
            {
                { "accepted", accepted == 1 ? new List<object> { frame.Hour } : new List<object>() }
            });
        }

        private Dictionary<string, object> PushBatch(Dictionary<string, object> request, string peer)
        {
            var accepted = new List<object>();

            foreach (object node in Json.List(request, "frames"))
            {
                ReadingFrame frame = ReadingFrame.FromJson(Json.Dict(node));
                if (Store(frame, peer)) accepted.Add(frame.Hour);
            }

            if (accepted.Count > 0)
            {
                LastPushLocal = DateTime.Now;
                _log.Info("Accepted " + accepted.Count + " hour(s) from " + peer + ".");
            }

            return AgentProtocol.Ok(new Dictionary<string, object> { { "accepted", accepted } });
        }

        /// <summary>
        /// A node may only write its own readings. Rejecting a frame that claims
        /// to be from this node keeps a misconfigured client from overwriting the
        /// 132 kV values with its own.
        /// </summary>
        private bool Store(ReadingFrame frame, string peer)
        {
            if (frame == null || string.IsNullOrEmpty(frame.ServerId) || string.IsNullOrEmpty(frame.SessionDate))
            {
                _log.Warn("Discarded a frame from " + peer + " with no node id or session date.");
                return false;
            }

            if (string.Equals(frame.ServerId, _config.NodeId, StringComparison.OrdinalIgnoreCase))
            {
                _log.Warn("Discarded a frame from " + peer + " claiming to be this node ('" +
                          frame.ServerId + "'). Check nodeId in client.config.json.");
                return false;
            }

            if (SessionClock.Order(frame.Hour) < 0)
            {
                _log.Warn("Discarded a frame from " + peer + " for hour " + frame.Hour + ".");
                return false;
            }

            _merged.Accept(frame);
            return true;
        }

        // -- The reverse channel --------------------------------------------

        /// <summary>
        /// Parks a calibration request for the client node. Returns the id to
        /// watch, so the seat that asked can be told what became of it.
        /// </summary>
        public RemoteCommand RequestCalibration(string targetNodeId, int maxWidth)
        {
            RemoteCommand queued = _commands.Queue(AgentProtocol.CmdCalibrate, targetNodeId, maxWidth);

            _log.Info("Calibration requested from '" + targetNodeId + "' (request " + queued.Id +
                      "). It is collected on that node's next poll.");
            return queued;
        }

        public CommandState StateOf(string requestId)
        {
            return _commands.StateOf(requestId);
        }

        /// <summary>
        /// A client node asking whether there is anything for it. This runs every
        /// few seconds on an idle link, so it says nothing to the log — except
        /// the first time a node appears, which is the line that tells an
        /// engineer the two nodeIds actually match.
        /// </summary>
        private Dictionary<string, object> Poll(Dictionary<string, object> request, string peer)
        {
            string nodeId = Json.Str(request, "nodeId");

            lock (_polled)
                if (_polled.Add(nodeId))
                    _log.Info("Node '" + nodeId + "' at " + peer + " is asking for commands.");

            var payload = new List<object>();
            foreach (RemoteCommand command in _commands.Take(nodeId))
            {
                payload.Add(command.ToJson());
                _log.Info("Handed '" + command.Command + "' (request " + command.Id + ") to '" + nodeId + "'.");
            }

            return AgentProtocol.Ok(new Dictionary<string, object> { { "commands", payload } });
        }

        /// <summary>
        /// A calibration coming back from the client node. It is kept beside
        /// this node's own shots and handed to whoever is watching, which on the
        /// 132 kV node means the window in front of the engineer who asked.
        /// </summary>
        private Dictionary<string, object> Calibration(Dictionary<string, object> request, string peer)
        {
            CalibrationShot shot = CalibrationShot.FromJson(Json.Dict(request, "shot"));
            string requestId = Json.Str(request, "requestId");

            if (string.IsNullOrEmpty(shot.NodeId))
                return AgentProtocol.Fail("The calibration carries no node id.");

            if (string.Equals(shot.NodeId, _config.NodeId, StringComparison.OrdinalIgnoreCase))
                return AgentProtocol.Fail("That calibration claims to be from this node ('" +
                                          shot.NodeId + "'). Check nodeId in client.config.json.");

            _commands.Answered(requestId);

            string saved = "";
            if (shot.Image != null && shot.Image.Length > 0)
            {
                try
                {
                    saved = shot.SaveTo(_config.ResolvePath("Calibration"));
                }
                catch (Exception ex)
                {
                    // Worth a line, not a refusal: the picture is in memory and
                    // about to go on screen, which is what was asked for.
                    _log.Warn("Could not keep the calibration from " + shot.NodeId + ": " + ex.Message);
                }
            }

            _log.Info("Calibration received from " + shot.NodeId + " at " + peer + ": " + shot.Summary() +
                      (saved.Length == 0 ? "" : " -> " + saved));

            Action<CalibrationShot> handler = CalibrationReceived;
            if (handler != null) handler(shot);

            return AgentProtocol.Ok(new Dictionary<string, object> { { "saved", saved } });
        }

        private Dictionary<string, object> History(Dictionary<string, object> request)
        {
            string sessionDate = Json.Str(request, "sessionDate");
            if (string.IsNullOrEmpty(sessionDate)) sessionDate = SessionClock.SessionDateStringOf(DateTime.Now);

            var frames = new List<object>();
            foreach (ReadingFrame frame in _store.LoadDay(sessionDate)) frames.Add(frame.ToJson());

            return AgentProtocol.Ok(new Dictionary<string, object>
            {
                { "sessionDate", sessionDate },
                { "frames", frames }
            });
        }

        public void Stop()
        {
            _running = false;
            try { if (_listener != null) _listener.Stop(); } catch { }
        }

        public void Dispose() { Stop(); }
    }
}
