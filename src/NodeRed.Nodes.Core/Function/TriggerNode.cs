// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/function/89-trigger.js
// ============================================================
// Trigger node - sends messages on trigger events.
// ============================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Function;

/// <summary>
/// Trigger node - sends messages on trigger events with optional reset.
/// Translated from: @node-red/nodes/core/function/89-trigger.js
/// </summary>
public class TriggerNode : Node
{
    private CancellationTokenSource? _timerCts;
    private bool _isTriggered;
    private readonly object _lock = new();

    /// <summary>
    /// First output value.
    /// </summary>
    [JsonPropertyName("op1")]
    public string? Op1 { get; set; } = "1";

    /// <summary>
    /// First output value type.
    /// </summary>
    [JsonPropertyName("op1type")]
    public string Op1Type { get; set; } = "str";

    /// <summary>
    /// Second output value.
    /// </summary>
    [JsonPropertyName("op2")]
    public string? Op2 { get; set; } = "0";

    /// <summary>
    /// Second output value type.
    /// </summary>
    [JsonPropertyName("op2type")]
    public string Op2Type { get; set; } = "str";

    /// <summary>
    /// Duration value.
    /// </summary>
    [JsonPropertyName("duration")]
    public string Duration { get; set; } = "250";

    /// <summary>
    /// Duration units.
    /// </summary>
    [JsonPropertyName("units")]
    public string Units { get; set; } = "ms";

    /// <summary>
    /// Whether to extend timer on new messages.
    /// </summary>
    [JsonPropertyName("extend")]
    public bool Extend { get; set; }

    /// <summary>
    /// Whether to override timer with new trigger.
    /// </summary>
    [JsonPropertyName("overrideDelay")]
    public bool OverrideDelay { get; set; }

    /// <summary>
    /// Reset value/property.
    /// </summary>
    [JsonPropertyName("reset")]
    public string? Reset { get; set; }

    /// <summary>
    /// What triggers the second output: "block", "extend", "nothing".
    /// </summary>
    [JsonPropertyName("bytopic")]
    public string ByTopic { get; set; } = "all";

    /// <summary>
    /// Topic type for topic-based triggering.
    /// </summary>
    [JsonPropertyName("topic")]
    public string? TopicValue { get; set; }

    /// <summary>
    /// Number of outputs.
    /// </summary>
    [JsonPropertyName("outputs")]
    public new int Outputs { get; set; } = 1;

    public TriggerNode()
    {
        Type = "trigger";
    }

    public override Task InitializeAsync()
    {
        OnInput(HandleInputAsync);
        return base.InitializeAsync();
    }

    public override async Task CloseAsync(bool removed)
    {
        lock (_lock)
        {
            _timerCts?.Cancel();
            _timerCts = null;
        }
        await base.CloseAsync(removed);
    }

    private async Task HandleInputAsync(FlowMessage msg)
    {
        try
        {
            // Check for reset
            if (!string.IsNullOrEmpty(Reset))
            {
                var resetValue = NodeRed.Util.Util.GetMessageProperty(msg, "reset");
                if (resetValue is not null)
                {
                    await DoResetAsync();
                    return;
                }
            }

            lock (_lock)
            {
                if (_isTriggered)
                {
                    if (Extend)
                    {
                        // Cancel and restart timer
                        _timerCts?.Cancel();
                        _ = StartTimerAsync(msg);
                    }
                    else if (OverrideDelay)
                    {
                        // Cancel and restart with new message
                        _timerCts?.Cancel();
                        _ = StartTimerAsync(msg);
                    }
                    // Otherwise ignore (block)
                    return;
                }

                _isTriggered = true;
            }

            // Send first output
            var firstValue = GetOutputValue(Op1, Op1Type, msg);
            NodeRed.Util.Util.SetMessageProperty(msg, "payload", firstValue);
            await SendAsync(NodeRed.Util.Util.CloneMessage(msg));

            // Start timer for second output
            _ = StartTimerAsync(msg);
        }
        catch (Exception ex)
        {
            Error(ex, msg);
        }
    }

    private async Task StartTimerAsync(FlowMessage msg)
    {
        var durationMs = GetDurationMs();
        
        CancellationToken token;
        lock (_lock)
        {
            _timerCts?.Cancel();
            _timerCts = new CancellationTokenSource();
            token = _timerCts.Token;
        }

        try
        {
            await Task.Delay((int)durationMs, token);

            if (!token.IsCancellationRequested)
            {
                // Send second output
                if (Op2Type != "nul")
                {
                    var secondValue = GetOutputValue(Op2, Op2Type, msg);
                    NodeRed.Util.Util.SetMessageProperty(msg, "payload", secondValue);
                    await SendAsync(msg);
                }

                lock (_lock)
                {
                    _isTriggered = false;
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Timer was cancelled - that's ok
        }
    }

    private async Task DoResetAsync()
    {
        lock (_lock)
        {
            _timerCts?.Cancel();
            _timerCts = null;
            _isTriggered = false;
        }
        await Task.CompletedTask;
    }

    private object? GetOutputValue(string? value, string type, FlowMessage msg)
    {
        return type switch
        {
            "str" => value,
            "num" => double.TryParse(value, out var num) ? num : 0,
            "bool" => value?.ToLower() == "true",
            "json" => !string.IsNullOrEmpty(value) 
                ? JsonSerializer.Deserialize<object>(value) 
                : null,
            "date" => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            "msg" => NodeRed.Util.Util.GetMessageProperty(msg, value ?? "payload"),
            "pay" => msg.Payload,
            "nul" => null,
            _ => value
        };
    }

    private double GetDurationMs()
    {
        if (!double.TryParse(Duration, out var value))
        {
            value = 250;
        }

        return Units switch
        {
            "ms" => value,
            "s" => value * 1000,
            "min" => value * 60000,
            "hr" => value * 3600000,
            _ => value
        };
    }
}
