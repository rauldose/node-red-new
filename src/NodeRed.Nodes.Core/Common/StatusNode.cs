// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/common/25-status.js
// ============================================================
// Status node - reports status changes from other nodes.
// ============================================================

using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Common;

/// <summary>
/// Status node - reports status changes from other nodes.
/// Translated from: @node-red/nodes/core/common/25-status.js
/// </summary>
public class StatusNode : Node
{
    /// <summary>
    /// List of node IDs to report status from. Empty = all nodes in flow.
    /// </summary>
    [JsonPropertyName("scope")]
    public List<string>? Scope { get; set; }

    public StatusNode()
    {
        Type = "status";
    }

    public override Task InitializeAsync()
    {
        // Status nodes are wired internally by the runtime
        // when a target node updates its status
        return base.InitializeAsync();
    }

    /// <summary>
    /// Called by the runtime when a node's status changes.
    /// </summary>
    public async Task OnStatusChangeAsync(NodeStatus status, Node sourceNode)
    {
        // Check scope
        if (Scope is not null && Scope.Count > 0 && !Scope.Contains(sourceNode.Id))
        {
            return;
        }

        var msg = new FlowMessage
        {
            MsgId = NodeRed.Util.Util.GenerateId(),
            Payload = status
        };

        msg.AdditionalProperties["status"] = new
        {
            fill = status.Fill,
            shape = status.Shape,
            text = status.Text,
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
