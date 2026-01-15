// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/common/25-catch.js
// ============================================================
// Catch node - catches errors from other nodes.
// ============================================================

using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Common;

/// <summary>
/// Catch node - catches errors from other nodes.
/// Translated from: @node-red/nodes/core/common/25-catch.js
/// </summary>
public class CatchNode : Node
{
    /// <summary>
    /// List of node IDs to catch errors from. Empty = all nodes in flow.
    /// </summary>
    [JsonPropertyName("scope")]
    public List<string>? Scope { get; set; }

    /// <summary>
    /// Whether to catch uncaught exceptions only.
    /// </summary>
    [JsonPropertyName("uncaught")]
    public bool Uncaught { get; set; }

    public CatchNode()
    {
        Type = "catch";
    }

    public override Task InitializeAsync()
    {
        // Catch nodes are wired internally by the runtime
        // when a target node throws an error
        return base.InitializeAsync();
    }

    /// <summary>
    /// Called by the runtime when an error occurs in a target node.
    /// </summary>
    public async Task OnErrorAsync(NodeError error, Node sourceNode)
    {
        // Check scope
        if (Scope is not null && Scope.Count > 0 && !Scope.Contains(sourceNode.Id))
        {
            return;
        }

        var msg = error.OriginalMessage is not null 
            ? NodeRed.Util.Util.CloneMessage(error.OriginalMessage) 
            : new FlowMessage { MsgId = NodeRed.Util.Util.GenerateId() };

        // Add error information to message
        msg.AdditionalProperties["error"] = new
        {
            message = error.Message,
            source = new
            {
                id = sourceNode.Id,
                type = sourceNode.Type,
                name = sourceNode.Name
            }
        };

        await SendAsync(msg);
    }
}
