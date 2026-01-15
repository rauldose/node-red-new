// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/function/15-change.js
// ============================================================
// Change node - modifies message properties.
// ============================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Function;

/// <summary>
/// Change node - sets, changes, deletes, or moves message properties.
/// Translated from: @node-red/nodes/core/function/15-change.js
/// </summary>
public class ChangeNode : Node
{
    // ============================================================
    // ORIGINAL CODE (15-change.js lines 15-20):
    // ------------------------------------------------------------
    // this.rules = n.rules;
    // ------------------------------------------------------------

    /// <summary>
    /// List of rules to apply.
    /// </summary>
    [JsonPropertyName("rules")]
    public List<ChangeRule> Rules { get; set; } = new();

    public ChangeNode()
    {
        Type = "change";
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
            foreach (var rule in Rules)
            {
                ApplyRule(msg, rule);
            }

            await SendAsync(msg);
        }
        catch (Exception ex)
        {
            Error(ex, msg);
        }
    }

    private void ApplyRule(FlowMessage msg, ChangeRule rule)
    {
        switch (rule.T)
        {
            case "set":
                ApplySetRule(msg, rule);
                break;
            case "change":
                ApplyChangeRule(msg, rule);
                break;
            case "delete":
                ApplyDeleteRule(msg, rule);
                break;
            case "move":
                ApplyMoveRule(msg, rule);
                break;
        }
    }

    private void ApplySetRule(FlowMessage msg, ChangeRule rule)
    {
        var value = GetRuleValue(msg, rule);
        
        if (rule.Pt == "msg")
        {
            NodeRed.Util.Util.SetMessageProperty(msg, rule.P, value);
        }
        // flow and global context would be handled similarly
    }

    private void ApplyChangeRule(FlowMessage msg, ChangeRule rule)
    {
        var currentValue = GetPropertyValue(msg, rule.P, rule.Pt);
        
        if (currentValue is string str)
        {
            var searchValue = GetRuleValue(msg, rule);
            var replaceValue = GetRuleValue(msg, new ChangeRule 
            { 
                To = rule.To, 
                Tot = rule.Tot 
            });

            string newValue;
            
            if (rule.Re)
            {
                // Regex replace
                try
                {
                    newValue = Regex.Replace(str, searchValue?.ToString() ?? "", 
                        replaceValue?.ToString() ?? "");
                }
                catch
                {
                    newValue = str;
                }
            }
            else
            {
                // Simple string replace
                newValue = str.Replace(searchValue?.ToString() ?? "", 
                    replaceValue?.ToString() ?? "");
            }

            NodeRed.Util.Util.SetMessageProperty(msg, rule.P, newValue);
        }
    }

    private void ApplyDeleteRule(FlowMessage msg, ChangeRule rule)
    {
        if (rule.Pt == "msg")
        {
            NodeRed.Util.Util.SetMessageProperty(msg, rule.P, null);
        }
    }

    private void ApplyMoveRule(FlowMessage msg, ChangeRule rule)
    {
        var value = GetPropertyValue(msg, rule.P, rule.Pt);
        
        // Delete from source
        ApplyDeleteRule(msg, rule);
        
        // Set at destination
        if (rule.Tot == "msg")
        {
            NodeRed.Util.Util.SetMessageProperty(msg, rule.To ?? "", value);
        }
    }

    private object? GetPropertyValue(FlowMessage msg, string? property, string? propertyType)
    {
        if (string.IsNullOrEmpty(property)) return null;

        return propertyType switch
        {
            "msg" => NodeRed.Util.Util.GetMessageProperty(msg, property),
            "flow" => null, // Would get from flow context
            "global" => null, // Would get from global context
            "env" => Environment.GetEnvironmentVariable(property),
            _ => NodeRed.Util.Util.GetMessageProperty(msg, property)
        };
    }

    private object? GetRuleValue(FlowMessage msg, ChangeRule rule)
    {
        return rule.Tot switch
        {
            "str" => rule.To,
            "num" => double.TryParse(rule.To, out var num) ? num : 0,
            "bool" => rule.To?.ToLower() == "true",
            "json" => !string.IsNullOrEmpty(rule.To) 
                ? JsonSerializer.Deserialize<object>(rule.To) 
                : null,
            "date" => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            "msg" => NodeRed.Util.Util.GetMessageProperty(msg, rule.To ?? ""),
            "flow" => null,
            "global" => null,
            "env" => Environment.GetEnvironmentVariable(rule.To ?? ""),
            _ => rule.To
        };
    }
}

/// <summary>
/// A rule for the change node.
/// </summary>
public class ChangeRule
{
    /// <summary>
    /// Rule type: set, change, delete, move.
    /// </summary>
    [JsonPropertyName("t")]
    public string T { get; set; } = "set";

    /// <summary>
    /// Property path to modify.
    /// </summary>
    [JsonPropertyName("p")]
    public string P { get; set; } = "payload";

    /// <summary>
    /// Property type (msg, flow, global).
    /// </summary>
    [JsonPropertyName("pt")]
    public string Pt { get; set; } = "msg";

    /// <summary>
    /// Value or destination property.
    /// </summary>
    [JsonPropertyName("to")]
    public string? To { get; set; }

    /// <summary>
    /// Value type (str, num, bool, json, msg, etc.).
    /// </summary>
    [JsonPropertyName("tot")]
    public string? Tot { get; set; }

    /// <summary>
    /// Whether to use regex for change operations.
    /// </summary>
    [JsonPropertyName("re")]
    public bool Re { get; set; }

    /// <summary>
    /// Search value for change operations.
    /// </summary>
    [JsonPropertyName("from")]
    public string? From { get; set; }

    /// <summary>
    /// Search value type.
    /// </summary>
    [JsonPropertyName("fromt")]
    public string? Fromt { get; set; }
}
