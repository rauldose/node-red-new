// ============================================================
// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/group.js
// ============================================================
// Node groups - visual grouping of nodes.
// ============================================================

namespace NodeRed.Editor.Services;

/// <summary>
/// A group of nodes that can be styled and moved together.
/// Translated from Node-RED group.js
/// </summary>
public class NodeGroup
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "group";
    public string FlowId { get; set; } = "";
    public string? Name { get; set; }
    public string? Style { get; set; }

    // Visual properties
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }

    // Style properties
    public string? Fill { get; set; }
    public double FillOpacity { get; set; } = 0.1;
    public string? Stroke { get; set; } = "#999999";
    public double StrokeOpacity { get; set; } = 1;
    public string? Label { get; set; }
    public string LabelPosition { get; set; } = "nw"; // nw, ne, sw, se

    // Member nodes
    public List<string> Nodes { get; set; } = new();

    // Selection state (not persisted)
    public bool Selected { get; set; }

    /// <summary>
    /// Calculate bounds from member nodes.
    /// </summary>
    public void CalculateBounds(IEnumerable<FlowNode> allNodes)
    {
        var memberNodes = allNodes.Where(n => Nodes.Contains(n.Id)).ToList();

        if (memberNodes.Count == 0) return;

        var padding = 20;
        var labelHeight = string.IsNullOrEmpty(Name) ? 0 : 24;

        var minX = memberNodes.Min(n => n.X) - padding;
        var minY = memberNodes.Min(n => n.Y) - padding - (LabelPosition.StartsWith("n") ? labelHeight : 0);
        var maxX = memberNodes.Max(n => n.X + n.Width) + padding;
        var maxY = memberNodes.Max(n => n.Y + n.Height) + padding + (LabelPosition.StartsWith("s") ? labelHeight : 0);

        X = minX;
        Y = minY;
        Width = maxX - minX;
        Height = maxY - minY;
    }

    /// <summary>
    /// Check if a point is inside the group.
    /// </summary>
    public bool ContainsPoint(int px, int py)
    {
        return px >= X && px <= X + Width && py >= Y && py <= Y + Height;
    }

    /// <summary>
    /// Check if a node is inside the group bounds.
    /// </summary>
    public bool ContainsNode(FlowNode node)
    {
        return node.X >= X && 
               node.X + node.Width <= X + Width && 
               node.Y >= Y && 
               node.Y + node.Height <= Y + Height;
    }

    /// <summary>
    /// Create a new group from selected nodes.
    /// </summary>
    public static NodeGroup CreateFromNodes(IEnumerable<FlowNode> selectedNodes, string flowId)
    {
        var nodesList = selectedNodes.ToList();

        if (nodesList.Count == 0)
        {
            throw new ArgumentException("Cannot create group with no nodes");
        }

        var group = new NodeGroup
        {
            Id = NodeRed.Util.Util.GenerateId(),
            FlowId = flowId,
            Nodes = nodesList.Select(n => n.Id).ToList()
        };

        group.CalculateBounds(nodesList);

        return group;
    }
}

/// <summary>
/// Manager for node groups.
/// </summary>
public class GroupManager
{
    private readonly List<NodeGroup> _groups = new();

    public IReadOnlyList<NodeGroup> Groups => _groups;

    public void AddGroup(NodeGroup group)
    {
        _groups.Add(group);
    }

    public void RemoveGroup(NodeGroup group)
    {
        _groups.Remove(group);
    }

    public void RemoveGroup(string groupId)
    {
        _groups.RemoveAll(g => g.Id == groupId);
    }

    public NodeGroup? GetGroup(string groupId)
    {
        return _groups.FirstOrDefault(g => g.Id == groupId);
    }

    public IEnumerable<NodeGroup> GetGroupsForFlow(string flowId)
    {
        return _groups.Where(g => g.FlowId == flowId);
    }

    public NodeGroup? GetGroupContainingNode(string nodeId)
    {
        return _groups.FirstOrDefault(g => g.Nodes.Contains(nodeId));
    }

    public void AddNodeToGroup(string groupId, string nodeId)
    {
        var group = GetGroup(groupId);
        if (group != null && !group.Nodes.Contains(nodeId))
        {
            group.Nodes.Add(nodeId);
        }
    }

    public void RemoveNodeFromGroup(string groupId, string nodeId)
    {
        var group = GetGroup(groupId);
        group?.Nodes.Remove(nodeId);

        // Remove empty groups
        if (group?.Nodes.Count == 0)
        {
            _groups.Remove(group);
        }
    }

    public void UpdateGroupBounds(string groupId, IEnumerable<FlowNode> allNodes)
    {
        var group = GetGroup(groupId);
        group?.CalculateBounds(allNodes);
    }

    public void Clear()
    {
        _groups.Clear();
    }

    public void ClearFlow(string flowId)
    {
        _groups.RemoveAll(g => g.FlowId == flowId);
    }
}
