// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/function/89-delay.js
// ============================================================
// Delay node - delays or rate limits messages.
// ============================================================

using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Function;

/// <summary>
/// Delay node - delays messages or rate limits them.
/// Translated from: @node-red/nodes/core/function/89-delay.js
/// </summary>
public class DelayNode : Node
{
    private readonly ConcurrentQueue<(FlowMessage msg, DateTime releaseTime)> _queue = new();
    private Timer? _processTimer;
    private DateTime _lastSendTime = DateTime.MinValue;
    private readonly object _lock = new();

    /// <summary>
    /// Pause type: "delay", "delayv", "rate", "queue", "timed", "random".
    /// </summary>
    [JsonPropertyName("pauseType")]
    public string PauseType { get; set; } = "delay";

    /// <summary>
    /// Delay or rate timeout value.
    /// </summary>
    [JsonPropertyName("timeout")]
    public string Timeout { get; set; } = "5";

    /// <summary>
    /// Timeout units: "milliseconds", "seconds", "minutes", "hours", "days".
    /// </summary>
    [JsonPropertyName("timeoutUnits")]
    public string TimeoutUnits { get; set; } = "seconds";

    /// <summary>
    /// Rate value for rate limiting.
    /// </summary>
    [JsonPropertyName("rate")]
    public string Rate { get; set; } = "1";

    /// <summary>
    /// Number of messages in a burst.
    /// </summary>
    [JsonPropertyName("nbRateUnits")]
    public string NbRateUnits { get; set; } = "1";

    /// <summary>
    /// Rate units: "second", "minute", "hour", "day".
    /// </summary>
    [JsonPropertyName("rateUnits")]
    public string RateUnits { get; set; } = "second";

    /// <summary>
    /// Random first value (for random delay).
    /// </summary>
    [JsonPropertyName("randomFirst")]
    public string RandomFirst { get; set; } = "1";

    /// <summary>
    /// Random last value (for random delay).
    /// </summary>
    [JsonPropertyName("randomLast")]
    public string RandomLast { get; set; } = "5";

    /// <summary>
    /// Random units.
    /// </summary>
    [JsonPropertyName("randomUnits")]
    public string RandomUnits { get; set; } = "seconds";

    /// <summary>
    /// Whether to drop intermediate messages.
    /// </summary>
    [JsonPropertyName("drop")]
    public bool Drop { get; set; }

    /// <summary>
    /// Whether to allow per-topic rate limiting.
    /// </summary>
    [JsonPropertyName("allowrate")]
    public bool AllowRate { get; set; }

    public DelayNode()
    {
        Type = "delay";
    }

    public override Task InitializeAsync()
    {
        OnInput(HandleInputAsync);

        // Start queue processor for rate limiting
        if (PauseType == "rate" || PauseType == "queue")
        {
            var intervalMs = (int)GetRateIntervalMs();
            _processTimer = new Timer(ProcessQueue, null, intervalMs, intervalMs);
        }

        return base.InitializeAsync();
    }

    public override async Task CloseAsync(bool removed)
    {
        _processTimer?.Dispose();
        _processTimer = null;
        await base.CloseAsync(removed);
    }

    private async Task HandleInputAsync(FlowMessage msg)
    {
        try
        {
            switch (PauseType)
            {
                case "delay":
                    await HandleDelayAsync(msg);
                    break;
                case "delayv":
                    await HandleVariableDelayAsync(msg);
                    break;
                case "rate":
                    HandleRateLimit(msg);
                    break;
                case "queue":
                    HandleQueue(msg);
                    break;
                case "random":
                    await HandleRandomDelayAsync(msg);
                    break;
                default:
                    await SendAsync(msg);
                    break;
            }
        }
        catch (Exception ex)
        {
            Error(ex, msg);
        }
    }

    private async Task HandleDelayAsync(FlowMessage msg)
    {
        var delayMs = GetDelayMs();
        await Task.Delay((int)delayMs);
        await SendAsync(msg);
    }

    private async Task HandleVariableDelayAsync(FlowMessage msg)
    {
        // Get delay from message property
        var delayValue = NodeRed.Util.Util.GetMessageProperty(msg, "delay");
        
        if (delayValue is not null && double.TryParse(delayValue.ToString(), out var delayMs))
        {
            await Task.Delay((int)delayMs);
        }
        
        await SendAsync(msg);
    }

    private async Task HandleRandomDelayAsync(FlowMessage msg)
    {
        var minMs = ParseTimeValue(RandomFirst, RandomUnits);
        var maxMs = ParseTimeValue(RandomLast, RandomUnits);
        
        var random = new Random();
        var delayMs = random.NextDouble() * (maxMs - minMs) + minMs;
        
        await Task.Delay((int)delayMs);
        await SendAsync(msg);
    }

    private void HandleRateLimit(FlowMessage msg)
    {
        if (Drop)
        {
            // Only send if enough time has passed
            var intervalMs = GetRateIntervalMs();
            var now = DateTime.UtcNow;
            
            lock (_lock)
            {
                if ((now - _lastSendTime).TotalMilliseconds >= intervalMs)
                {
                    _lastSendTime = now;
                    _ = SendAsync(msg);
                }
                // Else drop the message
            }
        }
        else
        {
            // Queue the message
            _queue.Enqueue((msg, DateTime.UtcNow));
        }
    }

    private void HandleQueue(FlowMessage msg)
    {
        _queue.Enqueue((msg, DateTime.UtcNow));
    }

    private void ProcessQueue(object? state)
    {
        if (_queue.TryDequeue(out var item))
        {
            _ = SendAsync(item.msg);
        }
    }

    private double GetDelayMs()
    {
        if (!double.TryParse(Timeout, out var value))
        {
            return 1000;
        }

        return ParseTimeValue(Timeout, TimeoutUnits);
    }

    private double GetRateIntervalMs()
    {
        if (!double.TryParse(Rate, out var rate) || rate <= 0)
        {
            rate = 1;
        }

        if (!double.TryParse(NbRateUnits, out var nbUnits) || nbUnits <= 0)
        {
            nbUnits = 1;
        }

        var periodMs = RateUnits switch
        {
            "second" => 1000.0,
            "minute" => 60000.0,
            "hour" => 3600000.0,
            "day" => 86400000.0,
            _ => 1000.0
        };

        return periodMs / rate * nbUnits;
    }

    private static double ParseTimeValue(string value, string units)
    {
        if (!double.TryParse(value, out var numValue))
        {
            numValue = 1;
        }

        return units switch
        {
            "milliseconds" => numValue,
            "seconds" => numValue * 1000,
            "minutes" => numValue * 60000,
            "hours" => numValue * 3600000,
            "days" => numValue * 86400000,
            _ => numValue * 1000
        };
    }
}
