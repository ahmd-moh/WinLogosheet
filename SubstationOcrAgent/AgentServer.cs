using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Substation.Shared;

namespace SubstationOcrAgent
{
    /// <summary>
    /// The socket half of the agent: a small line-oriented JSON server that
    /// answers WinLogosheet's requests for readings. Values cross the wire, not
    /// screenshots, so an hour costs roughly two kilobytes instead of a megabyte
    /// and the substation link stays usable.
    /// </summary>
    public sealed class AgentServer : IDisposable
    {
        private readonly AgentConfig _config;
        private readonly CaptureService _capture;
        private readonly HourStore _store;
        private readonly AgentLog _log;

        private TcpListener _listener;
        private Thread _acceptThread;
        private volatile bool _running;

        public int ConnectionsServed { get; private set; }
        public DateTime LastClientLocal { get; private set; }

        public AgentServer(AgentConfig config, CaptureService capture, HourStore store, AgentLog log)
        {
            _config = config;
            _capture = capture;
            _store = store;
            _log = log;
        }

        public void Start()
        {
            IPAddress address;
            if (!IPAddress.TryParse(_config.ListenAddress, out address)) address = IPAddress.Any;

            _listener = new TcpListener(address, _config.Port);
            _listener.Start();
            _running = true;

            _acceptThread = new Thread(AcceptLoop)
            {
                IsBackground = true,
                Name = "AgentServer.Accept"
            };
            _acceptThread.Start();

            _log.Info(string.Format(CultureInfo.InvariantCulture,
                "Listening on {0}:{1} as '{2}'", address, _config.Port, _config.ServerId));

            if (string.IsNullOrEmpty(_config.SharedSecret))
                _log.Warn("No sharedSecret configured — requests are not authenticated. " +
                          "Set the same secret here and in winlogosheet.v2.json.");
        }

        private void AcceptLoop()
        {
            while (_running)
            {
                TcpClient client = null;
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
                var worker = new Thread(() => ServeClient(captured))
                {
                    IsBackground = true,
                    Name = "AgentServer.Client"
                };
                worker.Start();
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
                LastClientLocal = DateTime.Now;

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

                        Dictionary<string, object> response = Handle(request, peer);
                        AgentProtocol.WriteMessage(stream, response, _config.SharedSecret);
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
                    case AgentProtocol.CmdRead: return Read(request);
                    case AgentProtocol.CmdHistory: return History(request);
                    case AgentProtocol.CmdCalibrate: return Calibrate();
                    case AgentProtocol.CmdRoiImage: return RoiImage(request);
                    default:
                        return AgentProtocol.Fail("Unknown command '" + command + "'.");
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
                    { "id", roi.Id },
                    { "label", roi.Label },
                    { "enabled", roi.Enabled },
                    { "rows", roi.Rows },
                    { "channels", channels }
                });
            }

            return AgentProtocol.Ok(new Dictionary<string, object>
            {
                { "serverId", _config.ServerId },
                { "displayName", _config.DisplayName },
                { "agentVersion", AgentInfo.Version },
                { "machine", Environment.MachineName },
                { "workdayStartHour", _config.WorkdayStartHour },
                { "captureMinute", _config.CaptureMinute },
                { "autoCapture", _config.AutoCaptureEnabled },
                { "allowRoiImages", _config.AllowRoiImages },
                { "referenceWidth", rois.ReferenceWidth },
                { "referenceHeight", rois.ReferenceHeight },
                { "rois", inventory }
            });
        }

        private Dictionary<string, object> Status()
        {
            return AgentProtocol.Ok(new Dictionary<string, object>
            {
                { "serverId", _config.ServerId },
                { "agentVersion", AgentInfo.Version },
                { "localTime", DateTime.Now.ToString("o", CultureInfo.InvariantCulture) },
                { "lastCapture", _capture.LastCaptureLocal == default(DateTime)
                                 ? "" : _capture.LastCaptureLocal.ToString("o", CultureInfo.InvariantCulture) },
                { "lastError", _capture.LastError ?? "" },
                { "connectionsServed", ConnectionsServed }
            });
        }

        private Dictionary<string, object> Read(Dictionary<string, object> request)
        {
            string sessionDate = Json.Str(request, "sessionDate");
            if (string.IsNullOrEmpty(sessionDate))
                sessionDate = LogsheetHours.SessionDate(DateTime.Now, _config.WorkdayStartHour);

            int hour = Json.Int(request, "hour", LogsheetHours.FromClock(DateTime.Now));
            bool fresh = Json.Bool(request, "fresh", false);

            // Default path: hand back what was read at :02. "fresh" re-reads the
            // display right now, which is what the operator's Re-read button does
            // after a value was obviously mis-captured.
            ReadingFrame frame = fresh ? null : _store.Load(sessionDate, hour);
            if (frame == null) frame = _capture.CaptureNow(sessionDate, hour);

            return AgentProtocol.Ok(new Dictionary<string, object>
            {
                { "frame", frame.ToJson() },
                { "cached", !fresh && frame.CapturedUtc < DateTime.UtcNow.AddSeconds(-5) }
            });
        }

        private Dictionary<string, object> History(Dictionary<string, object> request)
        {
            string sessionDate = Json.Str(request, "sessionDate");
            if (string.IsNullOrEmpty(sessionDate))
                sessionDate = LogsheetHours.SessionDate(DateTime.Now, _config.WorkdayStartHour);

            var frames = new List<object>();
            foreach (ReadingFrame frame in _store.LoadDay(sessionDate)) frames.Add(frame.ToJson());

            return AgentProtocol.Ok(new Dictionary<string, object>
            {
                { "sessionDate", sessionDate },
                { "frames", frames }
            });
        }

        private Dictionary<string, object> Calibrate()
        {
            string path = _capture.WriteCalibrationOverlay();
            return AgentProtocol.Ok(new Dictionary<string, object>
            {
                { "overlayPath", path },
                { "machine", Environment.MachineName }
            });
        }

        private Dictionary<string, object> RoiImage(Dictionary<string, object> request)
        {
            if (!_config.AllowRoiImages)
                return AgentProtocol.Fail("ROI images are disabled on this agent. " +
                                          "Set allowRoiImages to true in agent.config.json to troubleshoot.");

            string roiId = Json.Str(request, "roi");
            bool prepared = Json.Bool(request, "prepared", true);
            byte[] png = _capture.RoiEvidencePng(roiId, prepared);

            return AgentProtocol.Ok(new Dictionary<string, object>
            {
                { "roi", roiId },
                { "prepared", prepared },
                { "png", Convert.ToBase64String(png) }
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
