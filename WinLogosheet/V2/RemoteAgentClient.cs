using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using Substation.Shared;

namespace WinLogosheet.V2
{
    /// <summary>Identity and inventory an agent reports from "hello".</summary>
    public sealed class AgentIdentity
    {
        public string ServerId = "";
        public string DisplayName = "";
        public string AgentVersion = "";
        public string Machine = "";
        public bool AllowRoiImages;
        public List<string> Channels = new List<string>();
        public int EnabledRois;
        public int TotalRois;
    }

    /// <summary>
    /// The client half of the socket link. One short-lived connection per
    /// operation: the agents are on the same substation LAN, a connect costs
    /// almost nothing, and nothing is left holding a socket open overnight.
    /// </summary>
    public sealed class RemoteAgentClient
    {
        private readonly AgentEndpoint _endpoint;
        private readonly string _secret;
        private readonly int _timeoutMs;

        public RemoteAgentClient(AgentEndpoint endpoint, string secret, int timeoutMs)
        {
            _endpoint = endpoint;
            _secret = secret;
            _timeoutMs = Math.Max(2000, timeoutMs);
        }

        public AgentEndpoint Endpoint { get { return _endpoint; } }

        private Dictionary<string, object> Call(Dictionary<string, object> request)
        {
            using (var client = new TcpClient())
            {
                IAsyncResult connecting = client.BeginConnect(_endpoint.Host, _endpoint.Port, null, null);
                if (!connecting.AsyncWaitHandle.WaitOne(_timeoutMs))
                    throw new TimeoutException("No answer from " + _endpoint.Host + ":" + _endpoint.Port +
                                               " within " + (_timeoutMs / 1000) + " s.");
                client.EndConnect(connecting);

                client.NoDelay = true;
                client.ReceiveTimeout = _timeoutMs;
                client.SendTimeout = _timeoutMs;

                using (NetworkStream raw = client.GetStream())
                using (var stream = new BufferedStream(raw, 64 * 1024))
                {
                    AgentProtocol.WriteMessage(stream, request, _secret);

                    Dictionary<string, object> response = AgentProtocol.ReadMessage(stream, _secret);
                    if (response == null) throw new IOException("The agent closed the connection without answering.");

                    if (!Json.Bool(response, "ok", false))
                        throw new InvalidOperationException(Json.Str(response, "error", "The agent refused the request."));

                    return response;
                }
            }
        }

        public AgentIdentity Hello()
        {
            Dictionary<string, object> response = Call(AgentProtocol.Request(AgentProtocol.CmdHello));

            var identity = new AgentIdentity
            {
                ServerId = Json.Str(response, "serverId"),
                DisplayName = Json.Str(response, "displayName"),
                AgentVersion = Json.Str(response, "agentVersion"),
                Machine = Json.Str(response, "machine"),
                AllowRoiImages = Json.Bool(response, "allowRoiImages", false)
            };

            foreach (object node in Json.List(response, "rois"))
            {
                var roi = Json.Dict(node);
                identity.TotalRois++;
                if (Json.Bool(roi, "enabled", true)) identity.EnabledRois++;
                foreach (object channel in Json.List(roi, "channels"))
                    identity.Channels.Add(Convert.ToString(channel));
            }

            return identity;
        }

        /// <summary>
        /// Fetches one hour. fresh=false returns what the agent stored at :02;
        /// fresh=true makes it read the display again, which is what the
        /// operator's "Re-read" does.
        /// </summary>
        public ReadingFrame Read(string sessionDate, int hour, bool fresh)
        {
            Dictionary<string, object> request = AgentProtocol.Request(AgentProtocol.CmdRead);
            request["sessionDate"] = sessionDate;
            request["hour"] = hour;
            request["fresh"] = fresh;

            Dictionary<string, object> response = Call(request);
            return ReadingFrame.FromJson(Json.Dict(response, "frame"));
        }

        /// <summary>Every hour the agent holds for a session date — used to
        /// backfill after the link was down.</summary>
        public List<ReadingFrame> History(string sessionDate)
        {
            Dictionary<string, object> request = AgentProtocol.Request(AgentProtocol.CmdHistory);
            request["sessionDate"] = sessionDate;

            Dictionary<string, object> response = Call(request);

            var frames = new List<ReadingFrame>();
            foreach (object node in Json.List(response, "frames"))
                frames.Add(ReadingFrame.FromJson(Json.Dict(node)));
            return frames;
        }

        /// <summary>Asks the agent to write an ROI overlay on its own host.
        /// Returns the path on that machine — no image crosses the link.</summary>
        public string Calibrate()
        {
            Dictionary<string, object> response = Call(AgentProtocol.Request(AgentProtocol.CmdCalibrate));
            return Json.Str(response, "overlayPath");
        }

        /// <summary>
        /// On-demand PNG of one ROI, for chasing a bad reading. Agents ship with
        /// this disabled; the hourly path never calls it.
        /// </summary>
        public byte[] RoiImage(string roiId, bool prepared)
        {
            Dictionary<string, object> request = AgentProtocol.Request(AgentProtocol.CmdRoiImage);
            request["roi"] = roiId;
            request["prepared"] = prepared;

            Dictionary<string, object> response = Call(request);
            return Convert.FromBase64String(Json.Str(response, "png"));
        }
    }
}
