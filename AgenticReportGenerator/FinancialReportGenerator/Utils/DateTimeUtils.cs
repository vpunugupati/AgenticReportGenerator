namespace FinancialReportGenerator.Utils
{
    /// <summary>
    /// Utility methods for working with DateTime values
    /// </summary>
    public static class DateTimeUtils
    {
        /// <summary>
        /// Helper method to parse DateTime from metadata object
        /// </summary>
        public static DateTime GetDateTimeFromMetadata(object dateTimeObj)
        {
            if (dateTimeObj is DateTime dateTime)
                return dateTime;

            if (dateTimeObj is DateTimeOffset dateTimeOffset)
                return dateTimeOffset.DateTime;

            // Try to parse from string if it's not already a DateTime
            if (dateTimeObj != null && DateTime.TryParse(dateTimeObj.ToString(), out DateTime parsedDateTime))
                return parsedDateTime;

            return DateTime.MinValue; // Fallback value if parsing fails
        }
    }
}
