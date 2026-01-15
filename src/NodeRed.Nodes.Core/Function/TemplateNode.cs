// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/function/80-template.js
// ============================================================
// Template node - generates output using mustache templates.
// ============================================================

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Function;

/// <summary>
/// Template node - generates output based on a template.
/// Translated from: @node-red/nodes/core/function/80-template.js
/// Note: Uses simple mustache-like syntax. Full Mustache would need a library.
/// </summary>
public class TemplateNode : Node
{
    /// <summary>
    /// The template string.
    /// </summary>
    [JsonPropertyName("template")]
    public string Template { get; set; } = "";

    /// <summary>
    /// Property to set with result.
    /// </summary>
    [JsonPropertyName("field")]
    public string Field { get; set; } = "payload";

    /// <summary>
    /// Output format: "str", "json", "yaml".
    /// </summary>
    [JsonPropertyName("format")]
    public string Format { get; set; } = "str";

    /// <summary>
    /// Template syntax: "mustache", "plain".
    /// </summary>
    [JsonPropertyName("syntax")]
    public string Syntax { get; set; } = "mustache";

    /// <summary>
    /// Field type (msg, flow, global).
    /// </summary>
    [JsonPropertyName("fieldType")]
    public string FieldType { get; set; } = "msg";

    public TemplateNode()
    {
        Type = "template";
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
            string result;

            if (Syntax == "mustache")
            {
                result = ProcessMustache(Template, msg);
            }
            else
            {
                result = Template;
            }

            // Parse output if JSON format
            object outputValue = Format switch
            {
                "json" => System.Text.Json.JsonSerializer.Deserialize<object>(result) ?? result,
                _ => result
            };

            if (FieldType == "msg")
            {
                NodeRed.Util.Util.SetMessageProperty(msg, Field, outputValue);
            }

            await SendAsync(msg);
        }
        catch (Exception ex)
        {
            Error(ex, msg);
        }
    }

    /// <summary>
    /// Simple mustache-like template processing.
    /// Supports {{property}} and {{payload.nested.value}} syntax.
    /// </summary>
    private string ProcessMustache(string template, FlowMessage msg)
    {
        // Match {{property}} patterns
        var result = Regex.Replace(template, @"\{\{([^}]+)\}\}", match =>
        {
            var path = match.Groups[1].Value.Trim();
            
            // Handle special cases
            if (path == "payload")
            {
                return msg.Payload?.ToString() ?? "";
            }
            
            if (path == "topic")
            {
                return msg.Topic ?? "";
            }
            
            if (path == "_msgid")
            {
                return msg.MsgId;
            }

            // Handle nested paths like "payload.value" or "msg.payload"
            if (path.StartsWith("msg."))
            {
                path = path[4..]; // Remove "msg." prefix
            }

            var value = NodeRed.Util.Util.GetMessageProperty(msg, path);
            
            if (value is null)
            {
                return "";
            }
            
            if (value is string str)
            {
                return str;
            }
            
            try
            {
                return System.Text.Json.JsonSerializer.Serialize(value);
            }
            catch
            {
                return value.ToString() ?? "";
            }
        });

        return result;
    }
}
