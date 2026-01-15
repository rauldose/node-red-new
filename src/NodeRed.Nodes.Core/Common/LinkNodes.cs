// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/common/60-link.js
// ============================================================
// Link nodes - virtual wires between flows.
// ============================================================

using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Common;

/// <summary>
/// Link In node - receives messages from link out nodes.
/// Translated from: @node-red/nodes/core/common/60-link.js
/// </summary>
public class LinkInNode : Node
{
    /// <summary>
    /// List of link out node IDs that can send to this node.
    /// </summary>
    [JsonPropertyName("links")]
    public List<string> Links { get; set; } = new();

    public LinkInNode()
    {
        Type = "link in";
    }

    /// <summary>
    /// Called when a link out node sends a message.
    /// </summary>
    public async Task OnLinkReceiveAsync(FlowMessage msg)
    {
        await SendAsync(NodeRed.Util.Util.CloneMessage(msg));
    }
}

/// <summary>
/// Link Out node - sends messages to link in nodes.
/// Translated from: @node-red/nodes/core/common/60-link.js
/// </summary>
public class LinkOutNode : Node
{
    /// <summary>
    /// Mode: "link" (specific targets) or "return" (return to caller).
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "link";

    /// <summary>
    /// List of link in node IDs to send to.
    /// </summary>
    [JsonPropertyName("links")]
    public List<string> Links { get; set; } = new();

    /// <summary>
    /// Reference to linked LinkIn nodes.
    /// </summary>
    [JsonIgnore]
    public List<LinkInNode> LinkedNodes { get; set; } = new();

    public LinkOutNode()
    {
        Type = "link out";
    }

    public override Task InitializeAsync()
    {
        OnInput(HandleInputAsync);
        return base.InitializeAsync();
    }

    private async Task HandleInputAsync(FlowMessage msg)
    {
        if (Mode == "return")
        {
            // Return mode - send back through link call stack
            if (msg.AdditionalProperties.TryGetValue("_linkSource", out var source))
            {
                // Would route back through link call mechanism
            }
        }
        else
        {
            // Send to all linked nodes
            foreach (var linkIn in LinkedNodes)
            {
                await linkIn.OnLinkReceiveAsync(msg);
            }
        }
    }
}

/// <summary>
/// Link Call node - calls a link in node and waits for response.
/// Translated from: @node-red/nodes/core/common/60-link.js
/// </summary>
public class LinkCallNode : Node
{
    /// <summary>
    /// List of link in node IDs to call.
    /// </summary>
    [JsonPropertyName("links")]
    public List<string> Links { get; set; } = new();

    /// <summary>
    /// Timeout in seconds.
    /// </summary>
    [JsonPropertyName("timeout")]
    public int Timeout { get; set; } = 30;

    /// <summary>
    /// Reference to linked LinkIn nodes.
    /// </summary>
    [JsonIgnore]
    public List<LinkInNode> LinkedNodes { get; set; } = new();

    private readonly Dictionary<string, TaskCompletionSource<FlowMessage>> _pendingCalls = new();

    public LinkCallNode()
    {
        Type = "link call";
    }

    public override Task InitializeAsync()
    {
        OnInput(HandleInputAsync);
        return base.InitializeAsync();
    }

    private async Task HandleInputAsync(FlowMessage msg)
    {
        if (LinkedNodes.Count == 0) return;

        var callId = NodeRed.Util.Util.GenerateId();
        var tcs = new TaskCompletionSource<FlowMessage>();
        
        lock (_pendingCalls)
        {
            _pendingCalls[callId] = tcs;
        }

        try
        {
            // Add link source info for return routing
            var callMsg = NodeRed.Util.Util.CloneMessage(msg);
            callMsg.AdditionalProperties["_linkSource"] = new
            {
                id = Id,
                callId = callId
            };

            // Send to first linked node
            await LinkedNodes[0].OnLinkReceiveAsync(callMsg);

            // Wait for response with timeout
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(Timeout));
            var responseTask = tcs.Task;
            
            if (await Task.WhenAny(responseTask, Task.Delay(Timeout * 1000, cts.Token)) == responseTask)
            {
                var response = await responseTask;
                await SendAsync(response);
            }
            else
            {
                Error("Link call timeout", msg);
            }
        }
        finally
        {
            lock (_pendingCalls)
            {
                _pendingCalls.Remove(callId);
            }
        }
    }

    /// <summary>
    /// Called when a response is received from a link out node.
    /// </summary>
    public void OnLinkReturnAsync(string callId, FlowMessage msg)
    {
        TaskCompletionSource<FlowMessage>? tcs;
        lock (_pendingCalls)
        {
            _pendingCalls.TryGetValue(callId, out tcs);
        }

        tcs?.TrySetResult(msg);
    }
}
