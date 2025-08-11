using System;

namespace ea_Tracker.Services
{
    /// <summary>
    /// Centralized timezone conversion service for Turkey Istanbul time.
    /// Ensures consistent timezone handling across the entire system.
    /// </summary>
    public static class TimezoneService
    {
        /// <summary>
        /// Turkey Standard Time zone info - handles both standard time and daylight saving time automatically.
        /// </summary>
        private static readonly TimeZoneInfo TurkeyTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Turkey Standard Time");

        /// <summary>
        /// Converts UTC DateTime to Turkey local time.
        /// </summary>
        /// <param name="utcDateTime">UTC DateTime to convert</param>
        /// <returns>DateTime in Turkey timezone</returns>
        public static DateTime ConvertUtcToTurkeyTime(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                // Ensure we're working with UTC time
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            
            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, TurkeyTimeZone);
        }

        /// <summary>
        /// Gets current Turkey time as DateTimeOffset with proper offset.
        /// Use this for SignalR events and API responses.
        /// </summary>
        /// <returns>Current time in Turkey timezone with offset</returns>
        public static DateTimeOffset GetTurkeyDateTimeOffset()
        {
            var utcNow = DateTime.UtcNow;
            var turkeyTime = ConvertUtcToTurkeyTime(utcNow);
            var offset = TurkeyTimeZone.GetUtcOffset(utcNow);
            
            return new DateTimeOffset(turkeyTime, offset);
        }

        /// <summary>
        /// Gets current Turkey time as DateTime.
        /// Use this for display purposes.
        /// </summary>
        /// <returns>Current time in Turkey timezone</returns>
        public static DateTime GetTurkeyTime()
        {
            return ConvertUtcToTurkeyTime(DateTime.UtcNow);
        }

        /// <summary>
        /// Converts UTC DateTime to Turkey DateTimeOffset with proper offset.
        /// Use this for SignalR events and API responses that need timezone info.
        /// </summary>
        /// <param name="utcDateTime">UTC DateTime to convert</param>
        /// <returns>DateTimeOffset in Turkey timezone with offset</returns>
        public static DateTimeOffset ConvertUtcToTurkeyDateTimeOffset(DateTime utcDateTime)
        {
            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                // Ensure we're working with UTC time
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }
            
            var turkeyTime = ConvertUtcToTurkeyTime(utcDateTime);
            var offset = TurkeyTimeZone.GetUtcOffset(utcDateTime);
            
            return new DateTimeOffset(turkeyTime, offset);
        }

        /// <summary>
        /// Formats DateTime as Turkey time string for consistent display.
        /// </summary>
        /// <param name="utcDateTime">UTC DateTime to format</param>
        /// <returns>Formatted Turkey time string</returns>
        public static string FormatAsTurkeyTime(DateTime utcDateTime)
        {
            var turkeyTime = ConvertUtcToTurkeyTime(utcDateTime);
            return turkeyTime.ToString("dd.MM.yyyy HH:mm:ss");
        }
    }
}