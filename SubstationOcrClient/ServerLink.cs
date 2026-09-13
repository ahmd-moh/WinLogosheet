using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using Substation.Shared;

namespace SubstationOcrClient
{
    /// <summary>
    /// One connection to the 132 kV server node, for as long as the caller
    /// needs it.
    ///
    /// The server answers requests one at a time on the same socket, so a drain
    /// and the poll that follows it cost a single connection between them. Every
    /// failure — no answer, a refusal, a bad secret — arrives here as an
    /// exception carrying a sentence an engineer can act on.
    /// </summary>
    internal sealed class ServerLink : IDisposable
    {
        private readonly ClientConfig _config;
        private readonly TcpClient _client;
        private readonly NetworkStream _raw;
        private readonly BufferedStream _stream;

        private ServerLink(ClientConfig config, TcpClient client)
        {
            _config = config;
            _client = client;
            _raw = client.GetStream();
            _stream = new BufferedStream(_raw, 64 * 1024);
        }

        public static ServerLink Open(ClientConfig config)
        {
            var client = new TcpClient();
            try
            {
                IAsyncResult connecting = client.BeginConnect(config.ServerHost, config.ServerPort, null, null);
                if (!connecting.AsyncWaitHandle.WaitOne(config.TimeoutMs))
                    throw new TimeoutException("No answer from " + config.ServerHost + ":" +
                                               config.ServerPort + " within " +
                                               (config.TimeoutMs / 1000) + " s.");
                client.EndConnect(connecting);

                client.NoDelay = true;
                client.ReceiveTimeout = config.TimeoutMs;
                client.SendTimeout = config.TimeoutMs;

                return new ServerLink(config, client);
            }
            catch
            {
                client.Close();
                throw;
            }
        }

        /// <summary>
        /// Sends one request and returns the server's answer. Throws when the
        /// server refuses — every caller here treats a refusal as a failure.
        /// </summary>
        public Dictionary<string, object> Ask(Dictionary<string, object> request)
        {
            AgentProtocol.WriteMessage(_stream, request, _config.SharedSecret);

            Dictionary<string, object> response = AgentProtocol.ReadMessage(_stream, _config.SharedSecret);
            if (response == null)
                throw new IOException("The server node closed the connection without answering.");

            if (!Json.Bool(response, "ok", false))
                throw new InvalidOperationException(
                    Json.Str(response, "error", "The server node refused '" + Json.Str(request, "cmd") + "'."));

            return response;
        }

        public void Dispose()
        {
            try { _stream.Dispose(); } catch { }
            try { _raw.Dispose(); } catch { }
            try { _client.Close(); } catch { }
        }
    }
}
