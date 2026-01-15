// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/common/90-comment.js
// ============================================================
// Comment node - a non-functional node for documentation.
// ============================================================

using System.Text.Json.Serialization;

namespace NodeRed.Nodes.Core.Common;

/// <summary>
/// Comment node - a non-functional node for documentation.
/// Translated from: @node-red/nodes/core/common/90-comment.js
/// </summary>
public class CommentNode : Node
{
    /// <summary>
    /// The comment text/documentation.
    /// </summary>
    [JsonPropertyName("info")]
    public string? Info { get; set; }

    public CommentNode()
    {
        Type = "comment";
    }

    // Comment nodes don't process messages - they're purely visual
}
