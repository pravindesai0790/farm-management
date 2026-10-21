using System.Globalization;
using System.Text;

namespace FarmManagement.Application.Common.Helpers;

public static class CsvExportHelper
{
    private static readonly char[] InjectionPrefixes = ['=', '+', '-', '@', '\t', '\r'];
    private static readonly byte[] Utf8Bom = [0xEF, 0xBB, 0xBF];

    public static string EscapeText(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        var sanitized = value;
        if (InjectionPrefixes.Contains(sanitized[0]))
        {
            sanitized = "'" + sanitized;
        }

        return EscapeRfc4180(sanitized);
    }

    public static string EscapeNumber(decimal? value)
    {
        if (!value.HasValue)
        {
            return string.Empty;
        }

        return value.Value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    public static string EscapeDate(DateTimeOffset? date) =>
        date?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? string.Empty;

    public static string EscapeDateTime(DateTimeOffset? dateTime) =>
        dateTime?.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture) ?? string.Empty;

    public static string EscapeRfc4180(string field)
    {
        if (string.IsNullOrEmpty(field))
        {
            return string.Empty;
        }

        var mustQuote = field.Contains(',') || field.Contains('"') || field.Contains('\r') || field.Contains('\n');
        if (!mustQuote)
        {
            return field;
        }

        return "\"" + field.Replace("\"", "\"\"") + "\"";
    }

    public static byte[] CreateCsvBytes(string csvText)
    {
        var contentBytes = Encoding.UTF8.GetBytes(csvText);
        var result = new byte[Utf8Bom.Length + contentBytes.Length];
        Buffer.BlockCopy(Utf8Bom, 0, result, 0, Utf8Bom.Length);
        Buffer.BlockCopy(contentBytes, 0, result, Utf8Bom.Length, contentBytes.Length);
        return result;
    }
}
