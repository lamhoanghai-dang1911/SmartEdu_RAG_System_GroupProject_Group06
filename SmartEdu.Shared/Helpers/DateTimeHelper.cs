using System;

namespace SmartEdu.Shared.Helpers
{
    /// <summary>
    /// Helper methods to convert UTC DateTime values to Vietnam time (SE Asia Standard Time, UTC+7).
    /// This class only affects presentation layer conversions. Do NOT use it to change stored values or business logic comparisons.
    /// </summary>
    public static class DateTimeHelper
    {
        private const string VietnamTimeZoneId = "SE Asia Standard Time";

        // Cached TimeZoneInfo for SE Asia Standard Time. Created once for reuse.
        private static readonly TimeZoneInfo VietnamTimeZone = InitVietnamTimeZone();

        private static TimeZoneInfo InitVietnamTimeZone()
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(VietnamTimeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                // Fallback: create a fixed-offset time zone for UTC+7 if the named zone is not available on the system
                return TimeZoneInfo.CreateCustomTimeZone("UTC+7", TimeSpan.FromHours(7), "UTC+7", "UTC+7");
            }
            catch (InvalidTimeZoneException)
            {
                return TimeZoneInfo.CreateCustomTimeZone("UTC+7", TimeSpan.FromHours(7), "UTC+7", "UTC+7");
            }
        }

        /// <summary>
        /// Convert a UTC DateTime (or an Unspecified-kind DateTime that should be treated as UTC)
        /// to Vietnam local time (UTC+7) for display purposes.
        /// If the input's Kind is not Utc, it will be treated as Utc via DateTime.SpecifyKind.
        /// </summary>
        /// <param name="utcDateTime">A DateTime value representing UTC time or an Unspecified time that should be treated as UTC.</param>
        /// <returns>DateTime converted to Vietnam time (same date/time fields adjusted). Returned DateTime.Kind is the kind returned by TimeZoneInfo.ConvertTimeFromUtc (unspecified).</returns>
        public static DateTime ToVietnamTime(this DateTime utcDateTime)
        {
            // Keep MinValue/MaxValue unchanged
            if (utcDateTime == DateTime.MinValue || utcDateTime == DateTime.MaxValue)
                return utcDateTime;

            if (utcDateTime.Kind != DateTimeKind.Utc)
            {
                utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            }

            return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, VietnamTimeZone);
        }

        /// <summary>
        /// Nullable overload: returns null if input is null, otherwise converts value to Vietnam time.
        /// </summary>
        public static DateTime? ToVietnamTime(this DateTime? utcDateTime)
        {
            if (!utcDateTime.HasValue)
                return null;

            return utcDateTime.Value.ToVietnamTime();
        }
    }
}
