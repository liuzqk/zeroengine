using System;

namespace ZeroEngine.Utils
{
    public static class DateUtils
    {
        /// <summary>
        /// Timestamp origin (1970-01-01)
        /// </summary>
        private static readonly DateTime OriginTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        public const string Format_yyyy_MM_dd_HH_mm_ss = "yyyy-MM-dd HH:mm:ss";

        /// <summary>
        /// Get current local timestamp (13 digits, milliseconds)
        /// </summary>
        public static long GetCurrentTimeMillis()
        {
            return (long)(DateTime.Now - OriginTime.ToLocalTime()).TotalMilliseconds;
        }

        /// <summary>
        /// Get current local timestamp (10 digits, seconds)
        /// </summary>
        public static long GetCurrentTimeSeconds()
        {
            return (long)(DateTime.Now - OriginTime.ToLocalTime()).TotalSeconds;
        }

        /// <summary>
        /// Get current UTC0 timestamp (seconds)
        /// </summary>
        public static int GetUTC0TimeStamp()
        {
            return (int)(DateTime.UtcNow - OriginTime).TotalSeconds;
        }

        /// <summary>
        /// Convert Unix timestamp (seconds) to DateTime
        /// </summary>
        public static DateTime Unix2DateTime(long timestampSeconds)
        {
            return OriginTime.ToLocalTime().AddSeconds(timestampSeconds);
        }

        /// <summary>
        /// Convert DateTime to Unix timestamp (seconds)
        /// </summary>
        public static int DateTime2Unix(DateTime time)
        {
            return (int)(time - OriginTime.ToLocalTime()).TotalSeconds;
        }

        public static string FormatSecondsToHHMMSS(long seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:D2}:{1:D2}:{2:D2}", t.Hours, t.Minutes, t.Seconds);
        }

        public static string FormatSecondsToMMSS(long seconds)
        {
            TimeSpan t = TimeSpan.FromSeconds(seconds);
            return string.Format("{0:D2}:{1:D2}", t.Minutes, t.Seconds);
        }

        public static bool IsToday(DateTime dt)
        {
            return dt.Date == DateTime.Today;
        }
        
        /// <summary>
        /// Get remaining seconds of today
        /// </summary>
        public static long GetDayRemainingSeconds() 
        {
            DateTime nextDay = DateTime.Today.AddDays(1);
            return (long)(nextDay - DateTime.Now).TotalSeconds;
        }
    }
}
