using System;
using System.Globalization;

namespace Substation.Capture
{
    /// <summary>
    /// The run window and the hour numbering. Both PCs run the same code, so
    /// their two QR codes can never disagree about which day a reading belongs
    /// to — and the phone merges the two scans by that day.
    ///
    /// A session runs from 07:00 on its session date to 07:00 the next morning —
    /// 24 hourly readings, taken at :02 past each hour. When the window closes
    /// the nodes stop capturing and stay stopped: starting the next session is a
    /// deliberate manual act, not something that happens overnight on its own.
    /// </summary>
    public static class SessionClock
    {
        /// <summary>Hour of the day a session opens and the previous one closes.</summary>
        public const int SessionStartHour = 7;

        /// <summary>Minute past each hour at which the screen is read.</summary>
        public const int DefaultCaptureMinute = 2;

        /// <summary>
        /// Capture order: 07, 08 … 23 on the session date, then 00 … 06 the
        /// next morning. Hours are plain clock hours; midnight is 0, not 24.
        /// </summary>
        public static readonly int[] Sequence =
        {
            7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23,
            0, 1, 2, 3, 4, 5, 6
        };

        /// <summary>Position in the session, or -1 for an hour outside it.</summary>
        public static int Order(int hour)
        {
            for (int i = 0; i < Sequence.Length; i++)
                if (Sequence[i] == hour) return i;
            return -1;
        }

        /// <summary>
        /// The session a wall-clock time belongs to. Anything before 07:00
        /// belongs to the session that opened the previous morning.
        /// </summary>
        public static DateTime SessionDateOf(DateTime now)
        {
            return now.Hour < SessionStartHour ? now.Date.AddDays(-1) : now.Date;
        }

        public static string SessionDateStringOf(DateTime now)
        {
            return Format(SessionDateOf(now));
        }

        public static string Format(DateTime sessionDate)
        {
            return sessionDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        public static bool TryParse(string text, out DateTime sessionDate)
        {
            return DateTime.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                                          DateTimeStyles.None, out sessionDate);
        }

        /// <summary>07:00 on the session date.</summary>
        public static DateTime StartOf(DateTime sessionDate)
        {
            return sessionDate.Date.AddHours(SessionStartHour);
        }

        /// <summary>07:00 the following morning — the first instant outside the session.</summary>
        public static DateTime EndOf(DateTime sessionDate)
        {
            return sessionDate.Date.AddDays(1).AddHours(SessionStartHour);
        }

        /// <summary>
        /// True while <paramref name="now"/> falls inside the run window of the
        /// session that was open when the node started.
        ///
        /// The node pins its session at start-up rather than recomputing it, so
        /// a process left running past 07:00 goes quiet instead of rolling
        /// silently into the next day's session.
        /// </summary>
        public static bool IsInside(DateTime sessionDate, DateTime now)
        {
            return now >= StartOf(sessionDate) && now < EndOf(sessionDate);
        }

        /// <summary>How long until this session closes; zero once it has.</summary>
        public static TimeSpan Remaining(DateTime sessionDate, DateTime now)
        {
            TimeSpan left = EndOf(sessionDate) - now;
            return left < TimeSpan.Zero ? TimeSpan.Zero : left;
        }

        /// <summary>Readings expected between the session start and now.</summary>
        public static int ExpectedReadings(DateTime sessionDate, DateTime now, int captureMinute)
        {
            DateTime start = StartOf(sessionDate);
            DateTime cutoff = now > EndOf(sessionDate) ? EndOf(sessionDate) : now;
            if (cutoff <= start) return 0;

            int expected = 0;
            for (DateTime slot = start.AddMinutes(captureMinute);
                 slot <= cutoff && expected < Sequence.Length;
                 slot = slot.AddHours(1))
                expected++;

            return expected;
        }
    }
}
