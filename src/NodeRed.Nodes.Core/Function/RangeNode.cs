// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/function/16-range.js
// ============================================================
// Range node - scales a numeric value.
// ============================================================

using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Function;

/// <summary>
/// Range node - maps a numeric value from one range to another.
/// Translated from: @node-red/nodes/core/function/16-range.js
/// </summary>
public class RangeNode : Node
{
    /// <summary>
    /// Minimum input value.
    /// </summary>
    [JsonPropertyName("minin")]
    public double MinIn { get; set; }

    /// <summary>
    /// Maximum input value.
    /// </summary>
    [JsonPropertyName("maxin")]
    public double MaxIn { get; set; } = 100;

    /// <summary>
    /// Minimum output value.
    /// </summary>
    [JsonPropertyName("minout")]
    public double MinOut { get; set; }

    /// <summary>
    /// Maximum output value.
    /// </summary>
    [JsonPropertyName("maxout")]
    public double MaxOut { get; set; } = 100;

    /// <summary>
    /// Action when value is out of range: "scale", "clamp", "roll", "drop".
    /// </summary>
    [JsonPropertyName("action")]
    public string Action { get; set; } = "scale";

    /// <summary>
    /// Whether to round to integer.
    /// </summary>
    [JsonPropertyName("round")]
    public bool Round { get; set; }

    /// <summary>
    /// Property to read/write.
    /// </summary>
    [JsonPropertyName("property")]
    public string Property { get; set; } = "payload";

    public RangeNode()
    {
        Type = "range";
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
            
            if (!TryGetDouble(value, out var inputValue))
            {
                Warn($"Property {Property} is not a number");
                return;
            }

            double? result = Action switch
            {
                "clamp" => ScaleAndClamp(inputValue),
                "roll" => ScaleAndRoll(inputValue),
                "drop" => IsInRange(inputValue) ? Scale(inputValue) : null,
                _ => Scale(inputValue) // scale
            };

            if (result.HasValue)
            {
                var finalValue = Round ? Math.Round(result.Value) : result.Value;
                NodeRed.Util.Util.SetMessageProperty(msg, Property, finalValue);
                await SendAsync(msg);
            }
            // If drop action and out of range, don't send
        }
        catch (Exception ex)
        {
            Error(ex, msg);
        }
    }

    private double Scale(double value)
    {
        // Linear interpolation from input range to output range
        var ratio = (value - MinIn) / (MaxIn - MinIn);
        return MinOut + ratio * (MaxOut - MinOut);
    }

    private double ScaleAndClamp(double value)
    {
        var scaled = Scale(value);
        return Math.Clamp(scaled, Math.Min(MinOut, MaxOut), Math.Max(MinOut, MaxOut));
    }

    private double ScaleAndRoll(double value)
    {
        var scaled = Scale(value);
        var range = MaxOut - MinOut;
        
        if (range == 0) return MinOut;
        
        // Wrap around
        var offset = (scaled - MinOut) % range;
        if (offset < 0) offset += range;
        return MinOut + offset;
    }

    private bool IsInRange(double value)
    {
        var min = Math.Min(MinIn, MaxIn);
        var max = Math.Max(MinIn, MaxIn);
        return value >= min && value <= max;
    }

    private static bool TryGetDouble(object? value, out double result)
    {
        result = 0;
        if (value is null) return false;
        
        if (value is double d) { result = d; return true; }
        if (value is int i) { result = i; return true; }
        if (value is long l) { result = l; return true; }
        if (value is float f) { result = f; return true; }
        if (value is decimal dec) { result = (double)dec; return true; }
        
        return double.TryParse(value.ToString(), out result);
    }
}
