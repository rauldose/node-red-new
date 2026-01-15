// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/common/21-debug.js
// ============================================================
// Debug node - outputs messages to the debug sidebar.
// ============================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Common;

/// <summary>
/// Debug node - outputs messages to the debug sidebar.
/// Translated from: @node-red/nodes/core/common/21-debug.js
/// </summary>
public class DebugNode : Node
{
    // ============================================================
    // ORIGINAL CODE (21-debug.js lines 15-30):
    // ------------------------------------------------------------
    // this.name = n.name;
    // this.complete = (n.complete||"payload").toString();
    // this.console = n.console;
    // this.tostatus = n.tostatus;
    // this.statusType = n.statusType || "auto";
    // this.statusVal = n.statusVal;
    // this.tosidebar = n.tosidebar;
    // this.active = (n.active === null || typeof n.active === "undefined") || n.active;
    // ------------------------------------------------------------

    /// <summary>
    /// What to output: "payload", "true" (full message), or property path.
    /// </summary>
    [JsonPropertyName("complete")]
    public string Complete { get; set; } = "payload";

    /// <summary>
    /// Whether to also output to system console.
    /// </summary>
    [JsonPropertyName("console")]
    public bool Console { get; set; }

    /// <summary>
    /// Whether to output to node status.
    /// </summary>
    [JsonPropertyName("tostatus")]
    public bool ToStatus { get; set; }

    /// <summary>
    /// Status type: "auto", "msg", "jsonata".
    /// </summary>
    [JsonPropertyName("statusType")]
    public string StatusType { get; set; } = "auto";

    /// <summary>
    /// Status value expression.
    /// </summary>
    [JsonPropertyName("statusVal")]
    public string? StatusVal { get; set; }

    /// <summary>
    /// Whether to output to sidebar.
    /// </summary>
    [JsonPropertyName("tosidebar")]
    public bool ToSidebar { get; set; } = true;

    /// <summary>
    /// Whether this debug node is active.
    /// </summary>
    [JsonPropertyName("active")]
    public bool Active { get; set; } = true;

    /// <summary>
    /// Event raised when a debug message is generated.
    /// </summary>
    public static event EventHandler<DebugMessage>? DebugMessageReceived;

    public DebugNode()
    {
        Type = "debug";
    }

    public override Task InitializeAsync()
    {
        OnInput(HandleInputAsync);
        return base.InitializeAsync();
    }

    // ============================================================
    // ORIGINAL CODE (21-debug.js lines 80-150):
    // ------------------------------------------------------------
    // node.on("input", function(msg, send, done) {
    //     if (node.active) {
    //         sendDebug({id:node.id, z:node.z, name:node.name, topic:msg.topic, 
    //                    msg:msg, _path:msg._path});
    //     }
    //     done();
    // });
    // ------------------------------------------------------------
    private async Task HandleInputAsync(FlowMessage msg)
    {
        if (!Active) return;

        try
        {
            object? output;

            if (Complete == "true" || Complete == "full")
            {
                // Full message
                output = msg;
            }
            else if (Complete == "false" || Complete == "payload")
            {
                // Just payload
                output = msg.Payload;
            }
            else
            {
                // Specific property
                output = NodeRed.Util.Util.GetMessageProperty(msg, Complete);
            }

            var debugMsg = new DebugMessage
            {
                Id = Id,
                Z = FlowId,
                Name = Name ?? Type,
                Topic = msg.Topic,
                Property = Complete,
                Msg = FormatOutput(output),
                Timestamp = DateTime.UtcNow
            };

            // Send to debug sidebar
            if (ToSidebar)
            {
                DebugMessageReceived?.Invoke(this, debugMsg);
            }

            // Output to console
            if (Console)
            {
                var consoleOutput = $"[{DateTime.Now:HH:mm:ss.fff}] [{Name ?? Id}] {debugMsg.Msg}";
                System.Console.WriteLine(consoleOutput);
            }

            // Update node status
            if (ToStatus)
            {
                var statusText = GetStatusText(output);
                SetStatus(new NodeStatus
                {
                    Fill = "grey",
                    Shape = "ring",
                    Text = statusText
                });
            }
        }
        catch (Exception ex)
        {
            Error(ex, msg);
        }
    }

    private string FormatOutput(object? value)
    {
        if (value is null)
        {
            return "null";
        }

        if (value is string str)
        {
            return str;
        }

        if (value is byte[] bytes)
        {
            return $"buffer[{bytes.Length}]";
        }

        try
        {
            return JsonSerializer.Serialize(value, new JsonSerializerOptions
            {
                WriteIndented = false,
                MaxDepth = 10
            });
        }
        catch
        {
            return value.ToString() ?? "";
        }
    }

    private string GetStatusText(object? value)
    {
        if (StatusType == "msg" && !string.IsNullOrEmpty(StatusVal))
        {
            // Get value from message property
            return StatusVal;
        }

        // Auto format
        var formatted = FormatOutput(value);
        
        // Truncate if too long
        if (formatted.Length > 32)
        {
            formatted = formatted[..29] + "...";
        }

        return formatted;
    }

    /// <summary>
    /// Enable or disable this debug node.
    /// </summary>
    public void SetActive(bool active)
    {
        Active = active;
    }
}

/// <summary>
/// Debug message sent to the sidebar.
/// </summary>
public class DebugMessage
{
    public string Id { get; set; } = "";
    public string Z { get; set; } = "";
    public string? Name { get; set; }
    public string? Topic { get; set; }
    public string Property { get; set; } = "payload";
    public string Msg { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
