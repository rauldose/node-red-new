// ============================================================
// SOURCE: packages/node_modules/@node-red/editor-client/src/js/nodes.js
//         packages/node_modules/@node-red/runtime/lib/flows/Subflow.js
//         packages/node_modules/@node-red/editor-client/src/js/ui/subflow.js
// ============================================================
// Subflow support - treating a flow as a reusable node.
// ============================================================

namespace NodeRed.Editor.Services;

/// <summary>
/// Represents a subflow - a flow that can be used as a node.
/// Translated from Node-RED Subflow.js and nodes.js subflow handling.
/// </summary>
public class Subflow
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "subflow";
    public string Name { get; set; } = "";
    public string? Info { get; set; }
    public string? Category { get; set; } = "subflows";
    public string Color { get; set; } = "#DDAA99";

    // Input/output port definitions
    // ============================================================
    // SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/subflow.js
    // LINES: 90-110
    // ============================================================
    // Subflow can have 0 or 1 input and 0-n outputs
    // ============================================================
    public List<SubflowPort> In { get; set; } = new();
    public List<SubflowPort> Out { get; set; } = new();

    // Status node
    // ============================================================
    // SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/subflow.js
    // LINES: 120-135
    // ============================================================
    // Subflows can have a status node that propagates status to instances
    // ============================================================
    public SubflowStatusNode? Status { get; set; }

    // Environment variables
    public List<SubflowEnvVar> Env { get; set; } = new();

    // The nodes contained within the subflow
    public List<FlowNode> Nodes { get; set; } = new();
    public List<FlowWire> Wires { get; set; } = new();
    public List<NodeGroup> Groups { get; set; } = new();

    // Subflow instance properties
    public Dictionary<string, object>? Meta { get; set; }

    // Icon
    public string Icon { get; set; } = "subflow.svg";
    public string? IconUrl { get; set; }

    // Input/Output labels
    public List<string> InputLabels { get; set; } = new();
    public List<string> OutputLabels { get; set; } = new();

    /// <summary>
    /// Create a subflow instance (a node that uses this subflow as template).
    /// </summary>
    public FlowNode CreateInstance(string flowId, int x, int y)
    {
        return new FlowNode
        {
            Id = NodeRed.Util.Util.GenerateId(),
            Type = $"subflow:{Id}",
            Name = "",
            Label = Name,
            FlowId = flowId,
            X = x,
            Y = y,
            Width = 120,
            Height = 30,
            Color = Color,
            Inputs = In.Count > 0 ? 1 : 0,
            Outputs = Out.Count,
            Properties = new Dictionary<string, object>()
        };
    }

    /// <summary>
    /// Create a subflow from selected nodes.
    /// </summary>
    public static Subflow CreateFromSelection(IEnumerable<FlowNode> selectedNodes, IEnumerable<FlowWire> selectedWires, string name)
    {
        var nodesList = selectedNodes.ToList();
        var wiresList = selectedWires.ToList();

        var subflow = new Subflow
        {
            Id = NodeRed.Util.Util.GenerateId(),
            Name = name,
            Color = "#DDAA99"
        };

        // Find input connections (wires coming from outside the selection)
        var nodeIds = new HashSet<string>(nodesList.Select(n => n.Id));
        var inputWires = wiresList.Where(w => !nodeIds.Contains(w.SourceId) && nodeIds.Contains(w.TargetId)).ToList();
        var outputWires = wiresList.Where(w => nodeIds.Contains(w.SourceId) && !nodeIds.Contains(w.TargetId)).ToList();

        // Create input ports
        if (inputWires.Count > 0)
        {
            subflow.In.Add(new SubflowPort
            {
                X = 50,
                Y = 30,
                Wires = inputWires.Select(w => new SubflowWireRef { Id = w.TargetId }).ToList()
            });
        }

        // Create output ports
        var outputNodes = outputWires.Select(w => w.SourceId).Distinct().ToList();
        foreach (var nodeId in outputNodes)
        {
            subflow.Out.Add(new SubflowPort
            {
                X = 450,
                Y = 30 + subflow.Out.Count * 50,
                Wires = new List<SubflowWireRef> { new SubflowWireRef { Id = nodeId } }
            });
        }

        // Copy nodes (adjusting positions)
        var minX = nodesList.Min(n => n.X);
        var minY = nodesList.Min(n => n.Y);

        foreach (var node in nodesList)
        {
            var copy = new FlowNode
            {
                Id = node.Id,
                Type = node.Type,
                Name = node.Name,
                Label = node.Label,
                FlowId = subflow.Id,
                X = node.X - minX + 100,
                Y = node.Y - minY + 50,
                Width = node.Width,
                Height = node.Height,
                Color = node.Color,
                Inputs = node.Inputs,
                Outputs = node.Outputs,
                Properties = node.Properties != null ? new Dictionary<string, object>(node.Properties) : null
            };
            subflow.Nodes.Add(copy);
        }

        // Copy internal wires
        foreach (var wire in wiresList.Where(w => nodeIds.Contains(w.SourceId) && nodeIds.Contains(w.TargetId)))
        {
            subflow.Wires.Add(new FlowWire
            {
                SourceId = wire.SourceId,
                SourcePort = wire.SourcePort,
                TargetId = wire.TargetId,
                TargetPort = wire.TargetPort,
                FlowId = subflow.Id
            });
        }

        return subflow;
    }
}

/// <summary>
/// A port on a subflow (input or output).
/// ============================================================
/// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/subflow.js
/// LINES: 150-175
/// ============================================================
/// </summary>
public class SubflowPort
{
    public string Id { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public List<SubflowWireRef> Wires { get; set; } = new();
    public string? Label { get; set; }
}

/// <summary>
/// Subflow status node for propagating status to instances.
/// ============================================================
/// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/subflow.js
/// LINES: 180-195
/// ============================================================
/// </summary>
public class SubflowStatusNode
{
    public int X { get; set; } = 280;
    public int Y { get; set; } = 100;
    public List<SubflowWireRef> Wires { get; set; } = new();
}

/// <summary>
/// A wire reference in a subflow port.
/// </summary>
public class SubflowWireRef
{
    public string Id { get; set; } = "";
    public int Port { get; set; }
}

/// <summary>
/// An environment variable for a subflow.
/// ============================================================
/// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/editors/subflow.js
/// LINES: 450-520
/// ============================================================
/// </summary>
public class SubflowEnvVar
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "str";
    public object? Value { get; set; }
    public SubflowEnvVarUI? UI { get; set; }
    
    // Order for reordering
    public int Order { get; set; }
    
    // Credential handling
    public bool IsCredential { get; set; }
}

/// <summary>
/// UI configuration for subflow environment variable.
/// ============================================================
/// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/editors/subflow.js
/// LINES: 525-600
/// ============================================================
/// </summary>
public class SubflowEnvVarUI
{
    public string? Type { get; set; }  // none, input, select, checkbox, spinner, cred
    public string? Icon { get; set; }
    public string? Label { get; set; }
    public List<SubflowEnvOption>? Options { get; set; }
    
    // UI options
    public bool Hidden { get; set; }
    public double? Min { get; set; }
    public double? Max { get; set; }
    public double? Step { get; set; }
    public int? Rows { get; set; }
}

/// <summary>
/// Option for enum-type environment variable.
/// </summary>
public class SubflowEnvOption
{
    public string Label { get; set; } = "";
    public object? Value { get; set; }
}

/// <summary>
/// Manager for subflows.
/// </summary>
public class SubflowManager
{
    private readonly List<Subflow> _subflows = new();
    
    public IReadOnlyList<Subflow> Subflows => _subflows;
    
    public void AddSubflow(Subflow subflow)
    {
        _subflows.Add(subflow);
    }
    
    public void RemoveSubflow(string id)
    {
        _subflows.RemoveAll(s => s.Id == id);
    }
    
    public Subflow? GetSubflow(string id)
    {
        return _subflows.FirstOrDefault(s => s.Id == id);
    }
    
    /// <summary>
    /// Get the subflow definition from a subflow instance type (e.g., "subflow:abc123")
    /// </summary>
    public Subflow? GetSubflowFromInstanceType(string type)
    {
        if (!type.StartsWith("subflow:")) return null;
        var id = type.Substring(8);
        return GetSubflow(id);
    }
    
    public void Clear()
    {
        _subflows.Clear();
    }
}
