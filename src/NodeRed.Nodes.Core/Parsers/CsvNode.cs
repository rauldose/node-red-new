// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/parsers/70-CSV.js
// ============================================================
// CSV node - parses and generates CSV data.
// ============================================================

using System.Text;
using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Parsers;

/// <summary>
/// CSV node - converts between CSV strings and objects/arrays.
/// Translated from: @node-red/nodes/core/parsers/70-CSV.js
/// </summary>
public class CsvNode : Node
{
    /// <summary>
    /// Column names (comma separated).
    /// </summary>
    [JsonPropertyName("temp")]
    public string Temp { get; set; } = "";

    /// <summary>
    /// Separator character.
    /// </summary>
    [JsonPropertyName("sep")]
    public string Sep { get; set; } = ",";

    /// <summary>
    /// Whether to output as object (true) or array (false).
    /// </summary>
    [JsonPropertyName("hdrin")]
    public bool HdrIn { get; set; }

    /// <summary>
    /// Whether to include header row in output.
    /// </summary>
    [JsonPropertyName("hdrout")]
    public string HdrOut { get; set; } = "none";

    /// <summary>
    /// Multi-row handling: "one" (one message per row), "mult" (array).
    /// </summary>
    [JsonPropertyName("multi")]
    public string Multi { get; set; } = "one";

    /// <summary>
    /// Return type: "obj" or "array".
    /// </summary>
    [JsonPropertyName("ret")]
    public string Ret { get; set; } = "obj";

    /// <summary>
    /// Whether to skip empty rows.
    /// </summary>
    [JsonPropertyName("skip")]
    public string Skip { get; set; } = "0";

    /// <summary>
    /// Whether strings should be quoted.
    /// </summary>
    [JsonPropertyName("strings")]
    public bool Strings { get; set; } = true;

    /// <summary>
    /// Whether to include empty strings.
    /// </summary>
    [JsonPropertyName("include_empty_strings")]
    public bool IncludeEmptyStrings { get; set; }

    /// <summary>
    /// Whether to include null values.
    /// </summary>
    [JsonPropertyName("include_null_values")]
    public bool IncludeNullValues { get; set; }

    public CsvNode()
    {
        Type = "csv";
    }

    public override Task InitializeAsync()
    {
        OnInput(HandleInputAsync);
        return base.InitializeAsync();
    }

    private async Task HandleInputAsync(FlowMessage msg)
    {
        try
        {
            if (msg.Payload is string str)
            {
                // Parse CSV to objects
                await ParseCsvAsync(msg, str);
            }
            else
            {
                // Convert object(s) to CSV
                await GenerateCsvAsync(msg);
            }
        }
        catch (Exception ex)
        {
            Error(ex, msg);
        }
    }

    private async Task ParseCsvAsync(FlowMessage msg, string csv)
    {
        var separator = Sep.Length > 0 ? Sep[0] : ',';
        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var skipRows = int.TryParse(Skip, out var s) ? s : 0;

        // Get column names
        string[]? columns = null;
        var startRow = 0;

        if (!string.IsNullOrEmpty(Temp))
        {
            columns = Temp.Split(',').Select(c => c.Trim()).ToArray();
        }
        else if (HdrIn && lines.Length > 0)
        {
            columns = ParseCsvLine(lines[0], separator);
            startRow = 1;
        }

        var results = new List<object>();

        for (int i = startRow + skipRows; i < lines.Length; i++)
        {
            var values = ParseCsvLine(lines[i], separator);
            
            if (values.All(string.IsNullOrWhiteSpace))
            {
                continue; // Skip empty rows
            }

            if (Ret == "obj" && columns is not null)
            {
                var obj = new Dictionary<string, object?>();
                for (int j = 0; j < columns.Length && j < values.Length; j++)
                {
                    var value = ParseValue(values[j]);
                    if (value is not null || IncludeNullValues)
                    {
                        if (value is string strVal && string.IsNullOrEmpty(strVal) && !IncludeEmptyStrings)
                        {
                            continue;
                        }
                        obj[columns[j]] = value;
                    }
                }
                results.Add(obj);
            }
            else
            {
                results.Add(values.Select(ParseValue).ToArray());
            }
        }

        if (Multi == "one")
        {
            // Send one message per row
            foreach (var row in results)
            {
                var rowMsg = NodeRed.Util.Util.CloneMessage(msg);
                rowMsg.Payload = row;
                await SendAsync(rowMsg);
            }
        }
        else
        {
            // Send array of all rows
            msg.Payload = results;
            await SendAsync(msg);
        }
    }

    private async Task GenerateCsvAsync(FlowMessage msg)
    {
        var separator = Sep.Length > 0 ? Sep[0] : ',';
        var sb = new StringBuilder();

        // Get column names
        string[]? columns = null;
        if (!string.IsNullOrEmpty(Temp))
        {
            columns = Temp.Split(',').Select(c => c.Trim()).ToArray();
        }

        // Get data rows
        var rows = msg.Payload is IEnumerable<object> list 
            ? list.ToList() 
            : new List<object> { msg.Payload! };

        // Output header if needed
        if (HdrOut == "once" || HdrOut == "all")
        {
            if (columns is not null)
            {
                sb.AppendLine(string.Join(separator.ToString(), columns.Select(c => QuoteIfNeeded(c, separator))));
            }
            else if (rows.Count > 0 && rows[0] is Dictionary<string, object> firstRow)
            {
                columns = firstRow.Keys.ToArray();
                sb.AppendLine(string.Join(separator.ToString(), columns.Select(c => QuoteIfNeeded(c, separator))));
            }
        }

        // Output data rows
        foreach (var row in rows)
        {
            if (row is Dictionary<string, object> dict)
            {
                var values = columns is not null
                    ? columns.Select(c => dict.TryGetValue(c, out var v) ? v : null)
                    : dict.Values;
                
                sb.AppendLine(string.Join(separator.ToString(), 
                    values.Select(v => QuoteIfNeeded(v?.ToString() ?? "", separator))));
            }
            else if (row is object[] arr)
            {
                sb.AppendLine(string.Join(separator.ToString(), 
                    arr.Select(v => QuoteIfNeeded(v?.ToString() ?? "", separator))));
            }
        }

        msg.Payload = sb.ToString();
        await SendAsync(msg);
    }

    private static string[] ParseCsvLine(string line, char separator)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            var c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == separator && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(c);
            }
        }

        values.Add(current.ToString());
        return values.ToArray();
    }

    private static object? ParseValue(string value)
    {
        value = value.Trim();

        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        if (double.TryParse(value, out var num))
        {
            return num;
        }

        if (bool.TryParse(value, out var b))
        {
            return b;
        }

        return value;
    }

    private string QuoteIfNeeded(string value, char separator)
    {
        if (!Strings)
        {
            return value;
        }

        if (value.Contains(separator) || value.Contains('"') || value.Contains('\n'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }

        return value;
    }
}
