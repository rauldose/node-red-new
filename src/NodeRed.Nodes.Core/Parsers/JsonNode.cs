// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/parsers/70-JSON.js
// ============================================================
// JSON node - parses and stringifies JSON.
// ============================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Parsers;

/// <summary>
/// JSON node - converts between JSON strings and objects.
/// Translated from: @node-red/nodes/core/parsers/70-JSON.js
/// </summary>
public class JsonNode : Node
{
    /// <summary>
    /// Action: "obj" (to object), "str" (to string), "" (auto).
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    /// <summary>
    /// Property to convert.
    /// </summary>
    [JsonPropertyName("property")]
    public string Property { get; set; } = "payload";

    /// <summary>
    /// Whether to pretty print output.
    /// </summary>
    [JsonPropertyName("pretty")]
    public bool Pretty { get; set; }

    public JsonNode()
    {
        Type = "json";
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
            var value = NodeRed.Util.Util.GetMessageProperty(msg, Property);

            if (value is null)
            {
                await SendAsync(msg);
                return;
            }

            object? result;

            if (Action == "str")
            {
                // Always stringify
                result = Stringify(value);
            }
            else if (Action == "obj")
            {
                // Always parse
                result = Parse(value);
            }
            else
            {
                // Auto: toggle based on current type
                if (value is string str)
                {
                    result = Parse(str);
                }
                else
                {
                    result = Stringify(value);
                }
            }

            NodeRed.Util.Util.SetMessageProperty(msg, Property, result);
            await SendAsync(msg);
        }
        catch (JsonException ex)
        {
            Error($"JSON parse error: {ex.Message}", msg);
        }
        catch (Exception ex)
        {
            Error(ex, msg);
        }
    }

    private string Stringify(object value)
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = Pretty
        };
        return JsonSerializer.Serialize(value, options);
    }

    private object? Parse(object value)
    {
        var str = value is string s ? s : value.ToString();
        if (string.IsNullOrEmpty(str))
        {
            return null;
        }
        return JsonSerializer.Deserialize<object>(str);
    }
}
