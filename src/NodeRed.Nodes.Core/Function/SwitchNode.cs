// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/function/10-switch.js
// ============================================================
// Switch node - routes messages based on rules.
// ============================================================

using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Function;

/// <summary>
/// Switch node - routes messages based on property values.
/// Translated from: @node-red/nodes/core/function/10-switch.js
/// </summary>
public class SwitchNode : Node
{
    // ============================================================
    // ORIGINAL CODE (10-switch.js lines 15-25):
    // ------------------------------------------------------------
    // this.property = n.property;
    // this.propertyType = n.propertyType || "msg";
    // this.rules = n.rules || [];
    // this.checkall = n.checkall;
    // this.repair = n.repair;
    // this.outputs = n.outputs || n.rules.length;
    // ------------------------------------------------------------

    /// <summary>
    /// Property to evaluate (e.g., "payload", "topic").
    /// </summary>
    [JsonPropertyName("property")]
    public string Property { get; set; } = "payload";

    /// <summary>
    /// Type of property (msg, flow, global, env).
    /// </summary>
    [JsonPropertyName("propertyType")]
    public string PropertyType { get; set; } = "msg";

    /// <summary>
    /// List of rules to evaluate.
    /// </summary>
    [JsonPropertyName("rules")]
    public List<SwitchRule> Rules { get; set; } = new();

    /// <summary>
    /// Whether to check all rules or stop at first match.
    /// </summary>
    [JsonPropertyName("checkall")]
    public string Checkall { get; set; } = "true";

    /// <summary>
    /// Whether to repair message sequences.
    /// </summary>
    [JsonPropertyName("repair")]
    public bool Repair { get; set; }

    public SwitchNode()
    {
        Type = "switch";
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
            // Get the value to test
            var value = GetPropertyValue(msg);
            
            // Initialize output array
            var outputs = new FlowMessage?[Rules.Count];
            var matched = false;

            for (int i = 0; i < Rules.Count; i++)
            {
                var rule = Rules[i];
                
                if (EvaluateRule(value, rule, msg))
                {
                    outputs[i] = matched ? NodeRed.Util.Util.CloneMessage(msg) : msg;
                    matched = true;

                    // If not checking all rules, stop here
                    if (Checkall != "true")
                    {
                        break;
                    }
                }
            }

            await SendAsync(outputs);
        }
        catch (Exception ex)
        {
            Error(ex, msg);
        }
    }

    private object? GetPropertyValue(FlowMessage msg)
    {
        return PropertyType switch
        {
            "msg" => NodeRed.Util.Util.GetMessageProperty(msg, Property),
            "flow" => null, // Would get from flow context
            "global" => null, // Would get from global context
            "env" => Environment.GetEnvironmentVariable(Property),
            _ => NodeRed.Util.Util.GetMessageProperty(msg, Property)
        };
    }

    private bool EvaluateRule(object? value, SwitchRule rule, FlowMessage msg)
    {
        var compareValue = GetRuleValue(rule, msg);

        return rule.T switch
        {
            "eq" => Equals(value, compareValue),
            "neq" => !Equals(value, compareValue),
            "lt" => Compare(value, compareValue) < 0,
            "lte" => Compare(value, compareValue) <= 0,
            "gt" => Compare(value, compareValue) > 0,
            "gte" => Compare(value, compareValue) >= 0,
            "btwn" => IsBetween(value, compareValue, GetRuleValue2(rule, msg)),
            "cont" => Contains(value, compareValue),
            "regex" => MatchesRegex(value, rule.V),
            "true" => IsTruthy(value),
            "false" => !IsTruthy(value),
            "null" => value is null,
            "nnull" => value is not null,
            "istype" => IsType(value, rule.V),
            "empty" => IsEmpty(value),
            "nempty" => !IsEmpty(value),
            "head" => true, // First N messages - would need sequence tracking
            "tail" => true, // Last N messages - would need sequence tracking
            "index" => true, // Message at index - would need sequence tracking
            "jsonata_exp" => true, // JSONata expression - would need JSONata engine
            "else" => true, // Always match as fallback
            _ => false
        };
    }

    private object? GetRuleValue(SwitchRule rule, FlowMessage msg)
    {
        return rule.Vt switch
        {
            "msg" => NodeRed.Util.Util.GetMessageProperty(msg, rule.V ?? ""),
            "flow" => null,
            "global" => null,
            "env" => Environment.GetEnvironmentVariable(rule.V ?? ""),
            "str" => rule.V,
            "num" => double.TryParse(rule.V, out var num) ? num : 0,
            "bool" => rule.V?.ToLower() == "true",
            _ => rule.V
        };
    }

    private object? GetRuleValue2(SwitchRule rule, FlowMessage msg)
    {
        return rule.V2t switch
        {
            "msg" => NodeRed.Util.Util.GetMessageProperty(msg, rule.V2 ?? ""),
            "flow" => null,
            "global" => null,
            "env" => Environment.GetEnvironmentVariable(rule.V2 ?? ""),
            "str" => rule.V2,
            "num" => double.TryParse(rule.V2, out var num) ? num : 0,
            _ => rule.V2
        };
    }

    private static int Compare(object? a, object? b)
    {
        if (a is null && b is null) return 0;
        if (a is null) return -1;
        if (b is null) return 1;

        if (a is IComparable ca && b is IComparable cb)
        {
            try
            {
                return ca.CompareTo(cb);
            }
            catch
            {
                return string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);
            }
        }

        return string.Compare(a.ToString(), b.ToString(), StringComparison.Ordinal);
    }

    private static bool IsBetween(object? value, object? min, object? max)
    {
        return Compare(value, min) >= 0 && Compare(value, max) <= 0;
    }

    private static bool Contains(object? haystack, object? needle)
    {
        if (haystack is null || needle is null) return false;
        
        if (haystack is string str)
        {
            return str.Contains(needle.ToString() ?? "", StringComparison.OrdinalIgnoreCase);
        }

        if (haystack is IEnumerable<object> list)
        {
            return list.Any(item => Equals(item, needle));
        }

        return false;
    }

    private static bool MatchesRegex(object? value, string? pattern)
    {
        if (value is null || string.IsNullOrEmpty(pattern)) return false;
        
        try
        {
            return Regex.IsMatch(value.ToString() ?? "", pattern);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsTruthy(object? value)
    {
        if (value is null) return false;
        if (value is bool b) return b;
        if (value is int i) return i != 0;
        if (value is long l) return l != 0;
        if (value is double d) return d != 0;
        if (value is string s) return !string.IsNullOrEmpty(s);
        return true;
    }

    private static bool IsType(object? value, string? typeName)
    {
        return typeName switch
        {
            "string" => value is string,
            "number" => value is int or long or float or double or decimal,
            "boolean" => value is bool,
            "array" => value is Array or System.Collections.IList,
            "object" => value is not null && value is not string && value is not ValueType,
            "buffer" => value is byte[],
            "null" => value is null,
            "undefined" => value is null,
            _ => false
        };
    }

    private static bool IsEmpty(object? value)
    {
        if (value is null) return true;
        if (value is string s) return string.IsNullOrEmpty(s);
        if (value is Array arr) return arr.Length == 0;
        if (value is System.Collections.ICollection col) return col.Count == 0;
        return false;
    }
}

/// <summary>
/// A rule for the switch node.
/// </summary>
public class SwitchRule
{
    /// <summary>
    /// Rule type (eq, neq, lt, gt, etc.).
    /// </summary>
    [JsonPropertyName("t")]
    public string T { get; set; } = "eq";

    /// <summary>
    /// Value to compare against.
    /// </summary>
    [JsonPropertyName("v")]
    public string? V { get; set; }

    /// <summary>
    /// Value type (str, num, msg, etc.).
    /// </summary>
    [JsonPropertyName("vt")]
    public string Vt { get; set; } = "str";

    /// <summary>
    /// Second value (for between, etc.).
    /// </summary>
    [JsonPropertyName("v2")]
    public string? V2 { get; set; }

    /// <summary>
    /// Second value type.
    /// </summary>
    [JsonPropertyName("v2t")]
    public string? V2t { get; set; }

    /// <summary>
    /// Case sensitivity flag.
    /// </summary>
    [JsonPropertyName("case")]
    public bool Case { get; set; }
}
