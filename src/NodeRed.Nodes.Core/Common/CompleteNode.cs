// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/common/24-complete.js
// ============================================================
// Complete node - triggers when another node completes.
// ============================================================

using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Common;

/// <summary>
/// Complete node - triggers when another node completes processing.
/// Translated from: @node-red/nodes/core/common/24-complete.js
/// </summary>
public class CompleteNode : Node
{
    /// <summary>
    /// List of node IDs to listen to for completion.
    /// </summary>
    [JsonPropertyName("scope")]
    public List<string>? Scope { get; set; }

    /// <summary>
    /// Whether uncaught exceptions trigger this node.
    /// </summary>
    [JsonPropertyName("uncaught")]
    public bool Uncaught { get; set; }

    public CompleteNode()
    {
        Type = "complete";
    }

    public override Task InitializeAsync()
    {
        // Complete nodes are wired internally by the runtime
        // when a target node calls done()
        return base.InitializeAsync();
    }

    /// <summary>
    /// Called by the runtime when a target node completes.
    /// </summary>
    public async Task OnNodeCompleteAsync(FlowMessage msg, Node sourceNode)
    {
        if (Scope is null || Scope.Count == 0 || Scope.Contains(sourceNode.Id))
        {
            var completeMsg = NodeRed.Util.Util.CloneMessage(msg);
            await SendAsync(completeMsg);
        }
    }
}
