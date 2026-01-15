// ============================================================
// SOURCE: packages/node_modules/@node-red/runtime/lib/nodes/Node.js
// ============================================================
// Base Node class that all nodes inherit from.
// ============================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core;

/// <summary>
/// Base class for all Node-RED nodes.
/// Translated from: @node-red/runtime/lib/nodes/Node.js
/// </summary>
public abstract class Node
{
    private readonly List<Func<FlowMessage, Task>> _inputHandlers = new();
    private readonly object _lock = new();
    private bool _closing;

    /// <summary>
    /// Unique identifier for this node instance.
    /// </summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = "";

    /// <summary>
    /// The type of node (e.g., "inject", "debug", "function").
    /// </summary>
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    /// <summary>
    /// User-defined name for this node.
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// The flow (tab) this node belongs to.
    /// </summary>
    [JsonPropertyName("z")]
    public string FlowId { get; set; } = "";

    /// <summary>
    /// X position on the canvas.
    /// </summary>
    [JsonPropertyName("x")]
    public int X { get; set; }

    /// <summary>
    /// Y position on the canvas.
    /// </summary>
    [JsonPropertyName("y")]
    public int Y { get; set; }

    /// <summary>
    /// Wire connections from this node's outputs.
    /// </summary>
    [JsonPropertyName("wires")]
    public List<List<string>> Wires { get; set; } = new();

    /// <summary>
    /// Whether this node is disabled.
    /// </summary>
    [JsonPropertyName("d")]
    public bool Disabled { get; set; }

    /// <summary>
    /// Reference to connected output nodes.
    /// </summary>
    [JsonIgnore]
    public List<List<Node>> OutputNodes { get; set; } = new();

    /// <summary>
    /// Current status of the node.
    /// </summary>
    [JsonIgnore]
    public NodeStatus? Status { get; private set; }

    /// <summary>
    /// Event raised when the node's status changes.
    /// </summary>
    public event EventHandler<NodeStatus>? StatusChanged;

    /// <summary>
    /// Event raised when an error occurs in the node.
    /// </summary>
    public event EventHandler<NodeError>? ErrorOccurred;

    /// <summary>
    /// Event raised when the node sends a message.
    /// </summary>
    public event EventHandler<FlowMessage>? MessageSent;

    /// <summary>
    /// Initializes the node. Called after all nodes are created and wired.
    /// </summary>
    public virtual Task InitializeAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Called when the node is being closed/stopped.
    /// </summary>
    public virtual Task CloseAsync(bool removed)
    {
        _closing = true;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Register an input handler for incoming messages.
    /// Translated from: Node.prototype.on('input', ...)
    /// </summary>
    public void OnInput(Func<FlowMessage, Task> handler)
    {
        lock (_lock)
        {
            _inputHandlers.Add(handler);
        }
    }

    /// <summary>
    /// Receive a message into this node.
    /// </summary>
    public async Task ReceiveAsync(FlowMessage msg)
    {
        if (_closing || Disabled) return;

        List<Func<FlowMessage, Task>> handlers;
        lock (_lock)
        {
            handlers = _inputHandlers.ToList();
        }

        foreach (var handler in handlers)
        {
            try
            {
                await handler(msg);
            }
            catch (Exception ex)
            {
                Error(ex, msg);
            }
        }
    }

    /// <summary>
    /// Send a message to connected nodes.
    /// Translated from: Node.prototype.send
    /// </summary>
    public async Task SendAsync(FlowMessage? msg)
    {
        if (msg is null || _closing) return;

        await SendAsync(new[] { msg });
    }

    /// <summary>
    /// Send messages to multiple outputs.
    /// </summary>
    public async Task SendAsync(FlowMessage?[] msgs)
    {
        if (_closing) return;

        for (int i = 0; i < msgs.Length && i < OutputNodes.Count; i++)
        {
            var msg = msgs[i];
            if (msg is null) continue;

            // Clone the message for each output node
            var outputNodes = OutputNodes[i];
            for (int j = 0; j < outputNodes.Count; j++)
            {
                var targetNode = outputNodes[j];
                var msgToSend = j == outputNodes.Count - 1 ? msg : NodeRed.Util.Util.CloneMessage(msg);
                
                MessageSent?.Invoke(this, msgToSend);
                
                // Send asynchronously
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await targetNode.ReceiveAsync(msgToSend);
                    }
                    catch (Exception ex)
                    {
                        Error(ex, msgToSend);
                    }
                });
            }
        }
    }

    /// <summary>
    /// Update the node's status display.
    /// Translated from: Node.prototype.status
    /// </summary>
    public void SetStatus(NodeStatus status)
    {
        Status = status;
        StatusChanged?.Invoke(this, status);
    }

    /// <summary>
    /// Clear the node's status.
    /// </summary>
    public void ClearStatus()
    {
        Status = null;
        StatusChanged?.Invoke(this, new NodeStatus());
    }

    /// <summary>
    /// Log a warning message.
    /// </summary>
    public void Warn(string message)
    {
        NodeRed.Util.Log.LogWarn($"[{Type}:{Id}] {message}");
    }

    /// <summary>
    /// Log an error and optionally pass it to catch nodes.
    /// Translated from: Node.prototype.error
    /// </summary>
    public void Error(string message, FlowMessage? msg = null)
    {
        NodeRed.Util.Log.LogError($"[{Type}:{Id}] {message}");
        ErrorOccurred?.Invoke(this, new NodeError(message, msg));
    }

    /// <summary>
    /// Log an exception error.
    /// </summary>
    public void Error(Exception ex, FlowMessage? msg = null)
    {
        Error(ex.Message, msg);
    }

    /// <summary>
    /// Log a debug message.
    /// </summary>
    public void Debug(string message)
    {
        NodeRed.Util.Log.LogDebug($"[{Type}:{Id}] {message}");
    }

    /// <summary>
    /// Log a trace message.
    /// </summary>
    public void Trace(string message)
    {
        NodeRed.Util.Log.LogTrace($"[{Type}:{Id}] {message}");
    }

    /// <summary>
    /// Log a general message.
    /// </summary>
    public void LogMessage(string message)
    {
        NodeRed.Util.Log.LogInfo($"[{Type}:{Id}] {message}");
    }

    /// <summary>
    /// Get the display name for this node.
    /// </summary>
    public string GetDisplayName()
    {
        return Name ?? Type;
    }
}

/// <summary>
/// Represents a node's status display.
/// </summary>
public class NodeStatus
{
    [JsonPropertyName("fill")]
    public string? Fill { get; set; }

    [JsonPropertyName("shape")]
    public string? Shape { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

/// <summary>
/// Represents an error that occurred in a node.
/// </summary>
public class NodeError
{
    public string Message { get; }
    public FlowMessage? OriginalMessage { get; }

    public NodeError(string message, FlowMessage? originalMessage = null)
    {
        Message = message;
        OriginalMessage = originalMessage;
    }
}
