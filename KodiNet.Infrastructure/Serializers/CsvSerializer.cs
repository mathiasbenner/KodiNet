using System.Text;

namespace KodiNet.Infrastructure.Serializers;

public static class CsvSerializer
{
    // ── Deserialize ───────────────────────────────────────────────────────────

    public static async Task<string[]> ReadLinesAsync(Stream stream)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync();
        return content.ReplaceLineEndings("\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
    }

    public static string[] SplitCsv(string line)
    {
        // Handles fields enclosed in quotes
        var result = new List<string>();
        var inQuote = false;
        var field   = new StringBuilder();
        foreach (var c in line)
        {
            if (c == '"') { inQuote = !inQuote; continue; }
            if (c == ',' && !inQuote) { result.Add(field.ToString()); field.Clear(); continue; }
            field.Append(c);
        }
        result.Add(field.ToString());
        return result.ToArray();
    }

    // ── Serialize ─────────────────────────────────────────────────────────────

    public static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
