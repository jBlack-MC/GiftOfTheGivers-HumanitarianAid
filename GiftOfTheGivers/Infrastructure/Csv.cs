using System.Text;

namespace GiftOfTheGivers.Infrastructure;

/// <summary>Minimal RFC 4180 CSV writer with spreadsheet formula-injection protection.</summary>
public static class Csv
{
    public static string Build(IEnumerable<string> header, IEnumerable<IEnumerable<string?>> rows)
    {
        var builder = new StringBuilder();
        builder.AppendLine(string.Join(",", header.Select(Escape)));
        foreach (var row in rows)
        {
            builder.AppendLine(string.Join(",", row.Select(Escape)));
        }

        return builder.ToString();
    }

    public static string Escape(string? value)
    {
        value ??= string.Empty;

        // A leading =, +, - or @ makes Excel/Sheets evaluate the cell as a formula.
        if (value.Length > 0 && "=+-@\t\r".Contains(value[0]))
        {
            value = "'" + value;
        }

        return value.IndexOfAny([',', '"', '\n', '\r']) >= 0
            ? "\"" + value.Replace("\"", "\"\"") + "\""
            : value;
    }
}
