// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/common/20-inject.js
// ============================================================
// Inject node - triggers flow execution on timer or button press.
// ============================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Timers;
using NodeRed.Util;
using Timer = System.Timers.Timer;

namespace NodeRed.Nodes.Core.Common;

/// <summary>
/// Inject node - triggers flow execution.
/// Translated from: @node-red/nodes/core/common/20-inject.js
/// </summary>
public class InjectNode : Node
{
    private Timer? _timer;
    private Timer? _cronTimer;
    private bool _initialized;

    // ============================================================
    // ORIGINAL CODE (20-inject.js lines 15-35):
    // ------------------------------------------------------------
    // this.props = n.props || [];
    // this.repeat = n.repeat;
    // this.crontab = n.crontab;
    // this.once = n.once;
    // this.onceDelay = (n.onceDelay || 0.1) * 1000;
    // this.topic = n.topic;
    // this.payload = n.payload;
    // this.payloadType = n.payloadType || "date";
    // ------------------------------------------------------------

    /// <summary>
    /// Properties to inject into the message.
    /// </summary>
    [JsonPropertyName("props")]
    public List<InjectProperty> Props { get; set; } = new();

    /// <summary>
    /// Repeat interval in seconds.
    /// </summary>
    [JsonPropertyName("repeat")]
    public string? Repeat { get; set; }

    /// <summary>
    /// Cron expression for scheduling.
    /// </summary>
    [JsonPropertyName("crontab")]
    public string? Crontab { get; set; }

    /// <summary>
    /// Whether to inject once on startup.
    /// </summary>
    [JsonPropertyName("once")]
    public bool Once { get; set; }

    /// <summary>
    /// Delay in seconds before initial inject.
    /// </summary>
    [JsonPropertyName("onceDelay")]
    public double OnceDelay { get; set; } = 0.1;

    /// <summary>
    /// Message topic.
    /// </summary>
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    /// <summary>
    /// Payload value.
    /// </summary>
    [JsonPropertyName("payload")]
    public object? Payload { get; set; }

    /// <summary>
    /// Payload type (str, num, bool, json, date, etc.).
    /// </summary>
    [JsonPropertyName("payloadType")]
    public string PayloadType { get; set; } = "date";

    public InjectNode()
    {
        Type = "inject";
    }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        if (Once)
        {
            // Inject once after delay
            var delayMs = OnceDelay * 1000;
            _ = Task.Run(async () =>
            {
                await Task.Delay((int)delayMs);
                await DoInjectAsync();
            });
        }

        if (!string.IsNullOrEmpty(Repeat) && double.TryParse(Repeat, out var repeatSeconds) && repeatSeconds > 0)
        {
            _timer = new Timer(repeatSeconds * 1000);
            _timer.Elapsed += async (s, e) => await DoInjectAsync();
            _timer.AutoReset = true;
            _timer.Start();
        }

        _initialized = true;
    }

    public override async Task CloseAsync(bool removed)
    {
        _timer?.Stop();
        _timer?.Dispose();
        _timer = null;

        _cronTimer?.Stop();
        _cronTimer?.Dispose();
        _cronTimer = null;

        await base.CloseAsync(removed);
    }

    /// <summary>
    /// Manually trigger injection (called from editor button).
    /// </summary>
    public async Task InjectAsync()
    {
        await DoInjectAsync();
    }

    // ============================================================
    // ORIGINAL CODE (20-inject.js lines 98-130):
    // ------------------------------------------------------------
    // function doInject() {
    //     var msg = { topic: node.topic };
    //     ...
    //     node.send(msg);
    // }
    // ------------------------------------------------------------
    private async Task DoInjectAsync()
    {
        try
        {
            var msg = new FlowMessage
            {
                MsgId = NodeRed.Util.Util.GenerateId(),
                Topic = Topic
            };

            // Set payload based on type
            msg.Payload = PayloadType switch
            {
                "date" => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                "str" => Payload?.ToString() ?? "",
                "num" => double.TryParse(Payload?.ToString(), out var num) ? num : 0,
                "bool" => Payload?.ToString()?.ToLower() == "true",
                "json" => Payload is string jsonStr 
                    ? JsonSerializer.Deserialize<object>(jsonStr) 
                    : Payload,
                "bin" => Payload is string base64Str 
                    ? Convert.FromBase64String(base64Str) 
                    : Array.Empty<byte>(),
                "env" => Environment.GetEnvironmentVariable(Payload?.ToString() ?? ""),
                _ => Payload
            };

            // Process additional properties
            foreach (var prop in Props)
            {
                var value = GetPropertyValue(prop);
                NodeRed.Util.Util.SetMessageProperty(msg, prop.P, value);
            }

            await SendAsync(msg);
        }
        catch (Exception ex)
        {
            Error(ex);
        }
    }

    private object? GetPropertyValue(InjectProperty prop)
    {
        return prop.Vt switch
        {
            "date" => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            "str" => prop.V,
            "num" => double.TryParse(prop.V, out var num) ? num : 0,
            "bool" => prop.V?.ToLower() == "true",
            "json" => !string.IsNullOrEmpty(prop.V) 
                ? JsonSerializer.Deserialize<object>(prop.V) 
                : null,
            "bin" => !string.IsNullOrEmpty(prop.V) 
                ? Convert.FromBase64String(prop.V) 
                : Array.Empty<byte>(),
            "env" => Environment.GetEnvironmentVariable(prop.V ?? ""),
            _ => prop.V
        };
    }
}

/// <summary>
/// Property to inject into a message.
/// </summary>
public class InjectProperty
{
    /// <summary>
    /// Property path (e.g., "payload", "topic", "msg.custom").
    /// </summary>
    [JsonPropertyName("p")]
    public string P { get; set; } = "";

    /// <summary>
    /// Value to inject.
    /// </summary>
    [JsonPropertyName("v")]
    public string? V { get; set; }

    /// <summary>
    /// Value type (str, num, bool, json, date, etc.).
    /// </summary>
    [JsonPropertyName("vt")]
    public string Vt { get; set; } = "str";
}
