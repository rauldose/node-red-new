// ============================================================
// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/group.js
// ============================================================
// Node groups - visual grouping of nodes.
// ============================================================

namespace NodeRed.Editor.Services;

/// <summary>
/// A group of nodes that can be styled and moved together.
/// Translated from Node-RED group.js
/// ============================================================
/// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/group.js
/// LINES: 35-85
/// ============================================================
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
    // ============================================================
    // SOURCE: packages/node_modules/@node-red/editor-client/src/sass/groups.scss
    // LINES: 20-50
    // ============================================================
    public string? Fill { get; set; }
    public double FillOpacity { get; set; } = 0.1;
    public string? Stroke { get; set; } = "#999999";
    public double StrokeOpacity { get; set; } = 1;
    public string? Label { get; set; }
    public string LabelPosition { get; set; } = "nw"; // nw, ne, sw, se

    // Member nodes
    public List<string> Nodes { get; set; } = new();

    // Nested groups support
    // ============================================================
    // SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/group.js
    // LINES: 150-180
    // ============================================================
    public string? ParentGroupId { get; set; }
    public List<string> ChildGroups { get; set; } = new();

    // Selection state (not persisted)
    public bool Selected { get; set; }
    
    // Z-order (for rendering)
    public int ZIndex { get; set; }
    
    // Resize handle being dragged (not persisted)
    public string? ActiveResizeHandle { get; set; }

    /// <summary>
    /// Get the label position offset for rendering.
    /// ============================================================
    /// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/group.js
    /// LINES: 240-260
    /// ============================================================
    /// </summary>
    public (int x, int y) GetLabelPosition()
    {
        return LabelPosition switch
        {
            "nw" => (10, 18),  // Top-left
            "ne" => (Width - 10, 18),  // Top-right
            "sw" => (10, Height - 6),  // Bottom-left
            "se" => (Width - 10, Height - 6),  // Bottom-right
            _ => (10, 18)
        };
    }
    
    /// <summary>
    /// Get text anchor for label based on position.
    /// </summary>
    public string GetLabelAnchor()
    {
        return LabelPosition.EndsWith("e") ? "end" : "start";
    }

    /// <summary>
    /// Calculate bounds from member nodes.
    /// ============================================================
    /// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/group.js
    /// LINES: 280-320
    /// ============================================================
    /// </summary>
    public void CalculateBounds(IEnumerable<FlowNode> allNodes, IEnumerable<NodeGroup>? allGroups = null)
    {
        var memberNodes = allNodes.Where(n => Nodes.Contains(n.Id)).ToList();
        var childGroupsList = allGroups?.Where(g => ChildGroups.Contains(g.Id)).ToList() ?? new List<NodeGroup>();

        if (memberNodes.Count == 0 && childGroupsList.Count == 0) return;

        var padding = 20;
        var labelHeight = string.IsNullOrEmpty(Name) ? 0 : 24;

        var minX = int.MaxValue;
        var minY = int.MaxValue;
        var maxX = int.MinValue;
        var maxY = int.MinValue;

        foreach (var node in memberNodes)
        {
            minX = Math.Min(minX, node.X);
            minY = Math.Min(minY, node.Y);
            maxX = Math.Max(maxX, node.X + node.Width);
            maxY = Math.Max(maxY, node.Y + node.Height);
        }

        foreach (var group in childGroupsList)
        {
            minX = Math.Min(minX, group.X);
            minY = Math.Min(minY, group.Y);
            maxX = Math.Max(maxX, group.X + group.Width);
            maxY = Math.Max(maxY, group.Y + group.Height);
        }

        minX -= padding;
        minY -= padding + (LabelPosition.StartsWith("n") ? labelHeight : 0);
        maxX += padding;
        maxY += padding + (LabelPosition.StartsWith("s") ? labelHeight : 0);

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
    /// Check if a point is on the group border (for selection).
    /// ============================================================
    /// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/group.js
    /// LINES: 350-380
    /// ============================================================
    /// </summary>
    public bool IsOnBorder(int px, int py, int tolerance = 5)
    {
        var inOuterRect = px >= X - tolerance && px <= X + Width + tolerance &&
                          py >= Y - tolerance && py <= Y + Height + tolerance;
        var inInnerRect = px >= X + tolerance && px <= X + Width - tolerance &&
                          py >= Y + tolerance && py <= Y + Height - tolerance;
        return inOuterRect && !inInnerRect;
    }
    
    /// <summary>
    /// Get resize handle at point (if any).
    /// ============================================================
    /// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/group.js
    /// LINES: 400-450
    /// ============================================================
    /// </summary>
    public string? GetResizeHandleAt(int px, int py, int handleSize = 8)
    {
        var halfHandle = handleSize / 2;
        
        // Check corners first
        if (IsNear(px, py, X, Y, halfHandle)) return "nw";
        if (IsNear(px, py, X + Width, Y, halfHandle)) return "ne";
        if (IsNear(px, py, X, Y + Height, halfHandle)) return "sw";
        if (IsNear(px, py, X + Width, Y + Height, halfHandle)) return "se";
        
        // Check edges
        if (IsNear(px, py, X + Width / 2, Y, halfHandle)) return "n";
        if (IsNear(px, py, X + Width / 2, Y + Height, halfHandle)) return "s";
        if (IsNear(px, py, X, Y + Height / 2, halfHandle)) return "w";
        if (IsNear(px, py, X + Width, Y + Height / 2, halfHandle)) return "e";
        
        return null;
    }
    
    private bool IsNear(int px, int py, int tx, int ty, int tolerance)
    {
        return Math.Abs(px - tx) <= tolerance && Math.Abs(py - ty) <= tolerance;
    }
    
    /// <summary>
    /// Get cursor for resize handle.
    /// </summary>
    public static string GetResizeCursor(string handle)
    {
        return handle switch
        {
            "n" or "s" => "ns-resize",
            "e" or "w" => "ew-resize",
            "nw" or "se" => "nwse-resize",
            "ne" or "sw" => "nesw-resize",
            _ => "default"
        };
    }
    
    /// <summary>
    /// Apply resize from handle drag.
    /// ============================================================
    /// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/group.js
    /// LINES: 480-550
    /// ============================================================
    /// </summary>
    public void ApplyResize(string handle, int deltaX, int deltaY, int minSize = 40)
    {
        switch (handle)
        {
            case "n":
                if (Height - deltaY >= minSize) { Y += deltaY; Height -= deltaY; }
                break;
            case "s":
                if (Height + deltaY >= minSize) { Height += deltaY; }
                break;
            case "e":
                if (Width + deltaX >= minSize) { Width += deltaX; }
                break;
            case "w":
                if (Width - deltaX >= minSize) { X += deltaX; Width -= deltaX; }
                break;
            case "nw":
                if (Width - deltaX >= minSize) { X += deltaX; Width -= deltaX; }
                if (Height - deltaY >= minSize) { Y += deltaY; Height -= deltaY; }
                break;
            case "ne":
                if (Width + deltaX >= minSize) { Width += deltaX; }
                if (Height - deltaY >= minSize) { Y += deltaY; Height -= deltaY; }
                break;
            case "sw":
                if (Width - deltaX >= minSize) { X += deltaX; Width -= deltaX; }
                if (Height + deltaY >= minSize) { Height += deltaY; }
                break;
            case "se":
                if (Width + deltaX >= minSize) { Width += deltaX; }
                if (Height + deltaY >= minSize) { Height += deltaY; }
                break;
        }
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
    /// ============================================================
    /// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/group.js
    /// LINES: 600-650
    /// ============================================================
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
