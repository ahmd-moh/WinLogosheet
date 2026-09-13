using System;
using System.Collections.Generic;
using Substation.Shared;

namespace SubstationOcrServer
{
    /// <summary>Where an instruction has got to, for the seat that asked.</summary>
    public enum CommandState
    {
        /// <summary>Never queued here, or long enough ago to have been forgotten.</summary>
        Unknown,

        /// <summary>Waiting: the client node has not asked for work since.</summary>
        Waiting,

        /// <summary>The client node took it and has not answered yet.</summary>
        Collected,

        /// <summary>Nobody came for it in time and it was dropped.</summary>
        Expired
    }

    /// <summary>
    /// Instructions parked for the client node, and what became of them.
    ///
    /// Only the client opens sockets, so this is the whole of the server's reach
    /// into it: an instruction waits here until the client's next poll. It waits
    /// for ten minutes and no longer — a calibration queued while the 33 kV node
    /// was switched off should not fire at three in the morning and leave a
    /// picture nobody asked for.
    /// </summary>
    internal sealed class CommandQueue
    {
        public const int TtlSeconds = 600;

        /// <summary>How long a collected instruction is remembered, so the seat
        /// that asked can be told the difference between "never picked up" and
        /// "picked up and never answered".</summary>
        private const int HistorySeconds = 1800;

        private readonly object _gate = new object();
        private readonly List<RemoteCommand> _waiting = new List<RemoteCommand>();
        private readonly Dictionary<string, CommandState> _seen = new Dictionary<string, CommandState>();
        private readonly Dictionary<string, DateTime> _seenAt = new Dictionary<string, DateTime>();

        /// <summary>
        /// Parks one instruction. A second identical request while the first is
        /// still waiting returns the first — an operator clicking twice should
        /// get one picture back, not two.
        /// </summary>
        public RemoteCommand Queue(string command, string target, int maxWidth)
        {
            lock (_gate)
            {
                Prune();

                foreach (RemoteCommand existing in _waiting)
                    if (string.Equals(existing.Command, command, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(existing.Target, target ?? "", StringComparison.OrdinalIgnoreCase))
                        return existing;

                RemoteCommand queued = RemoteCommand.New(command, target, maxWidth);
                _waiting.Add(queued);
                Remember(queued.Id, CommandState.Waiting);
                return queued;
            }
        }

        /// <summary>Hands the asking node everything addressed to it, once.</summary>
        public List<RemoteCommand> Take(string nodeId)
        {
            var due = new List<RemoteCommand>();

            lock (_gate)
            {
                Prune();

                for (int i = _waiting.Count - 1; i >= 0; i--)
                {
                    if (!_waiting[i].IsFor(nodeId)) continue;

                    due.Add(_waiting[i]);
                    Remember(_waiting[i].Id, CommandState.Collected);
                    _waiting.RemoveAt(i);
                }
            }

            due.Reverse(); // oldest first
            return due;
        }

        public CommandState StateOf(string id)
        {
            lock (_gate)
            {
                Prune();

                CommandState state;
                return _seen.TryGetValue(id ?? "", out state) ? state : CommandState.Unknown;
            }
        }

        /// <summary>Called when the answer arrives, so the id stops being open.</summary>
        public void Answered(string id)
        {
            if (string.IsNullOrEmpty(id)) return;

            lock (_gate)
            {
                _seen.Remove(id);
                _seenAt.Remove(id);
            }
        }

        public int WaitingCount { get { lock (_gate) return _waiting.Count; } }

        private void Remember(string id, CommandState state)
        {
            _seen[id] = state;
            _seenAt[id] = DateTime.UtcNow;
        }

        private void Prune()
        {
            DateTime now = DateTime.UtcNow;

            for (int i = _waiting.Count - 1; i >= 0; i--)
            {
                if ((now - _waiting[i].QueuedUtc).TotalSeconds <= TtlSeconds) continue;
                Remember(_waiting[i].Id, CommandState.Expired);
                _waiting.RemoveAt(i);
            }

            var stale = new List<string>();
            foreach (var kv in _seenAt)
                if ((now - kv.Value).TotalSeconds > HistorySeconds) stale.Add(kv.Key);

            foreach (string id in stale)
            {
                _seen.Remove(id);
                _seenAt.Remove(id);
            }
        }
    }
}
