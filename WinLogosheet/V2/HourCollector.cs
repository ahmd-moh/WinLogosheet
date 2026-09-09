using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Substation.Shared;

namespace WinLogosheet.V2
{
    public enum ColumnStatus
    {
        Ok,
        LowConfidence,
        OutOfRange,
        Missing,
        AgentUnreachable,
        NoBinding,
        KeptManualEdit
    }

    /// <summary>What one logsheet column ended up with, and where it came from.</summary>
    public sealed class ColumnResult
    {
        public int Column;
        public string Value = "";
        public string Label = "";
        public string SourceId = "";
        public string Channel = "";
        public double Confidence;
        public ColumnStatus Status = ColumnStatus.Missing;
        public string Note = "";

        public bool NeedsAttention
        {
            get { return Status != ColumnStatus.Ok && Status != ColumnStatus.KeptManualEdit; }
        }
    }

    /// <summary>The merge of both agents' frames into one logsheet row.</summary>
    public sealed class CollectResult
    {
        public string SessionDate = "";
        public int Hour;
        public DateTime CollectedLocal = DateTime.Now;

        /// <summary>Column 1-24 values, index 0 = column 1. Raw numeric text
        /// ("129.15", "-35.29"); the form applies its own display rounding.</summary>
        public string[] Values = new string[ColumnMap.ColumnCount];

        public List<ColumnResult> Columns = new List<ColumnResult>();

        /// <summary>Agent id to error text, for agents that did not answer.</summary>
        public Dictionary<string, string> AgentErrors = new Dictionary<string, string>();

        public bool AnyAgentReached;

        public int CountOk
        {
            get
            {
                int n = 0;
                foreach (ColumnResult c in Columns) if (c.Status == ColumnStatus.Ok) n++;
                return n;
            }
        }

        public int CountNeedingAttention
        {
            get
            {
                int n = 0;
                foreach (ColumnResult c in Columns) if (c.NeedsAttention) n++;
                return n;
            }
        }

        public string Summary()
        {
            var sb = new StringBuilder();
            sb.Append(string.Format(CultureInfo.InvariantCulture, "Hour {0:00}: {1}/{2} value(s) collected",
                                    Hour, CountOk, ColumnMap.ColumnCount));
            if (CountNeedingAttention > 0)
                sb.Append(", " + CountNeedingAttention + " need checking");
            foreach (var kv in AgentErrors)
                sb.Append(" | " + kv.Key + ": " + kv.Value);
            return sb.ToString();
        }

        /// <summary>Line-per-problem text for the collector report window.</summary>
        public List<string> Problems()
        {
            var problems = new List<string>();

            foreach (var kv in AgentErrors)
                problems.Add("Agent " + kv.Key + " unreachable — " + kv.Value);

            foreach (ColumnResult c in Columns)
            {
                if (!c.NeedsAttention) continue;

                string what;
                switch (c.Status)
                {
                    case ColumnStatus.LowConfidence:
                        what = string.Format(CultureInfo.InvariantCulture,
                                             "read as '{0}' at only {1:0}% confidence", c.Value, c.Confidence);
                        break;
                    case ColumnStatus.OutOfRange:
                        what = "read as '" + c.Value + "', outside the expected range for this measurement";
                        break;
                    case ColumnStatus.AgentUnreachable:
                        what = "its agent (" + c.SourceId + ") did not answer";
                        break;
                    case ColumnStatus.NoBinding:
                        what = "no column mapping is configured";
                        break;
                    default:
                        what = "no value was read";
                        break;
                }

                problems.Add(string.Format(CultureInfo.InvariantCulture, "Column {0} ({1}): {2}",
                                           c.Column, string.IsNullOrEmpty(c.Label) ? c.Channel : c.Label, what));
            }

            return problems;
        }
    }

    /// <summary>
    /// Pulls one hour from every configured agent and folds the answers into a
    /// single logsheet row.
    ///
    /// The agents are queried independently: one server being down costs its own
    /// columns and nothing else, and the operator gets a row with the reachable
    /// half filled in rather than an empty hour.
    /// </summary>
    public sealed class HourCollector
    {
        private readonly V2Settings _settings;

        public HourCollector(V2Settings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// existingValues may be null. When PreserveManualEdits is on, any column
        /// that already holds a value is left exactly as the operator left it —
        /// unless overwriteExisting is set, which is how the explicit "Collect
        /// hour" and "Re-read this hour" buttons replace a whole row on request.
        /// </summary>
        public CollectResult Collect(string sessionDate, int hour, bool fresh, string[] existingValues,
                                     bool overwriteExisting = false)
        {
            var result = new CollectResult { SessionDate = sessionDate, Hour = hour };

            // One request per agent, not per column.
            var frames = new Dictionary<string, ReadingFrame>(StringComparer.OrdinalIgnoreCase);

            foreach (AgentEndpoint endpoint in _settings.Agents)
            {
                if (!endpoint.Enabled) continue;

                try
                {
                    var client = new RemoteAgentClient(endpoint, _settings.SharedSecret, _settings.TimeoutMs);
                    ReadingFrame frame = client.Read(sessionDate, hour, fresh);

                    frames[endpoint.Id] = frame;
                    result.AnyAgentReached = true;

                    if (!string.IsNullOrEmpty(frame.Error))
                        result.AgentErrors[endpoint.Id] = frame.Error;
                }
                catch (Exception ex)
                {
                    result.AgentErrors[endpoint.Id] = ex.Message;
                }
            }

            for (int column = 1; column <= ColumnMap.ColumnCount; column++)
            {
                ColumnResult resolved = ResolveColumn(column, frames, existingValues, overwriteExisting);
                result.Columns.Add(resolved);
                result.Values[column - 1] = resolved.Value;
            }

            return result;
        }

        private ColumnResult ResolveColumn(int column, Dictionary<string, ReadingFrame> frames,
                                           string[] existingValues, bool overwriteExisting)
        {
            ColumnBinding binding = _settings.Columns.For(column);

            var outcome = new ColumnResult
            {
                Column = column,
                Label = binding == null ? "" : binding.Label,
                SourceId = binding == null ? "" : binding.ServerId,
                Channel = binding == null ? "" : binding.Channel
            };

            string manual = (existingValues != null && column - 1 < existingValues.Length)
                            ? existingValues[column - 1] : null;

            // A value already on the sheet was either typed by the operator or
            // collected earlier and left alone. Either way it wins, unless the
            // operator explicitly asked for this row to be replaced.
            if (!overwriteExisting && _settings.PreserveManualEdits && !string.IsNullOrEmpty(manual))
            {
                outcome.Value = manual;
                outcome.Status = ColumnStatus.KeptManualEdit;
                outcome.Note = "kept the value already on the sheet";
                return outcome;
            }

            if (binding == null)
            {
                outcome.Status = ColumnStatus.NoBinding;
                return outcome;
            }

            ChannelReading reading = Lookup(frames, binding.ServerId, binding.Channel);

            // Fall back to the secondary channel when the primary read nothing.
            if ((reading == null || !reading.Ok) && binding.HasFallback)
            {
                ChannelReading alternate = Lookup(frames, binding.FallbackServerId, binding.FallbackChannel);
                if (alternate != null && alternate.Ok)
                {
                    reading = alternate;
                    outcome.SourceId = binding.FallbackServerId;
                    outcome.Channel = binding.FallbackChannel;
                    outcome.Note = "read from the fallback channel";
                }
            }

            if (reading == null)
            {
                bool reachable = frames.ContainsKey(binding.ServerId);
                outcome.Status = reachable ? ColumnStatus.Missing : ColumnStatus.AgentUnreachable;
                if (reachable) outcome.Note = "the agent has no channel '" + binding.Channel + "'";
                return KeepIfNothingRead(outcome, manual);
            }

            outcome.Value = reading.Value ?? "";
            outcome.Confidence = reading.Confidence;

            if (!reading.Ok)
            {
                outcome.Status = ColumnStatus.Missing;
                outcome.Note = string.IsNullOrEmpty(reading.RawText) ? "" : "OCR returned '" + reading.RawText + "'";
                return KeepIfNothingRead(outcome, manual);
            }

            double numeric = reading.AsDouble();
            string row = ChannelRow(binding.Channel);

            if (!NumberFormat.InExpectedRange(row, binding.BusClass, numeric))
            {
                outcome.Status = ColumnStatus.OutOfRange;
                return outcome;
            }

            outcome.Status = reading.Confidence < _settings.LowConfidenceThreshold
                             ? ColumnStatus.LowConfidence
                             : ColumnStatus.Ok;
            return outcome;
        }

        /// <summary>
        /// A column that produced no value keeps whatever was already on the
        /// sheet. Without this, an explicit re-read while one server is down
        /// would wipe out that server's hand-typed columns.
        /// </summary>
        private static ColumnResult KeepIfNothingRead(ColumnResult outcome, string manual)
        {
            if (string.IsNullOrEmpty(manual)) return outcome;

            outcome.Note = string.IsNullOrEmpty(outcome.Note)
                ? "nothing read — kept the value already on the sheet"
                : outcome.Note + "; kept the value already on the sheet";
            outcome.Value = manual;
            outcome.Status = ColumnStatus.KeptManualEdit;
            return outcome;
        }

        private static ChannelReading Lookup(Dictionary<string, ReadingFrame> frames, string serverId, string channel)
        {
            ReadingFrame frame;
            if (!frames.TryGetValue(serverId ?? "", out frame)) return null;
            return frame.Find(channel);
        }

        /// <summary>"T1.MW" -> "MW".</summary>
        private static string ChannelRow(string channel)
        {
            if (string.IsNullOrEmpty(channel)) return "";
            int dot = channel.LastIndexOf('.');
            return dot < 0 ? channel : channel.Substring(dot + 1);
        }

        /// <summary>
        /// Pulls every hour both agents hold for a session date. Used by
        /// "Backfill day" after the link, or WinLogosheet itself, was down.
        /// </summary>
        public Dictionary<int, CollectResult> CollectDay(string sessionDate,
                                                         Dictionary<int, string[]> existingByHour)
        {
            var byAgent = new Dictionary<string, Dictionary<int, ReadingFrame>>(StringComparer.OrdinalIgnoreCase);
            var agentErrors = new Dictionary<string, string>();

            foreach (AgentEndpoint endpoint in _settings.Agents)
            {
                if (!endpoint.Enabled) continue;

                try
                {
                    var client = new RemoteAgentClient(endpoint, _settings.SharedSecret, _settings.TimeoutMs);
                    var hours = new Dictionary<int, ReadingFrame>();
                    foreach (ReadingFrame frame in client.History(sessionDate)) hours[frame.Hour] = frame;
                    byAgent[endpoint.Id] = hours;
                }
                catch (Exception ex)
                {
                    agentErrors[endpoint.Id] = ex.Message;
                }
            }

            // Every hour any agent has something for.
            var allHours = new List<int>();
            foreach (var agent in byAgent.Values)
                foreach (int hour in agent.Keys)
                    if (!allHours.Contains(hour)) allHours.Add(hour);
            allHours.Sort();

            var results = new Dictionary<int, CollectResult>();

            foreach (int hour in allHours)
            {
                var frames = new Dictionary<string, ReadingFrame>(StringComparer.OrdinalIgnoreCase);
                foreach (var agent in byAgent)
                {
                    ReadingFrame frame;
                    if (agent.Value.TryGetValue(hour, out frame)) frames[agent.Key] = frame;
                }

                string[] existing = null;
                if (existingByHour != null) existingByHour.TryGetValue(hour, out existing);

                var result = new CollectResult
                {
                    SessionDate = sessionDate,
                    Hour = hour,
                    AnyAgentReached = frames.Count > 0
                };
                foreach (var kv in agentErrors) result.AgentErrors[kv.Key] = kv.Value;

                for (int column = 1; column <= ColumnMap.ColumnCount; column++)
                {
                    // Backfill only ever fills gaps; it never replaces a row.
                    ColumnResult resolved = ResolveColumn(column, frames, existing, false);
                    result.Columns.Add(resolved);
                    result.Values[column - 1] = resolved.Value;
                }

                results[hour] = result;
            }

            return results;
        }
    }
}
