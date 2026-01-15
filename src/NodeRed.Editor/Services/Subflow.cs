// ============================================================
// SOURCE: packages/node_modules/@node-red/editor-client/src/js/nodes.js
//         packages/node_modules/@node-red/runtime/lib/flows/Subflow.js
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
    public List<SubflowPort> In { get; set; } = new();
    public List<SubflowPort> Out { get; set; } = new();

    // Environment variables
    public List<SubflowEnvVar> Env { get; set; } = new();

    // The nodes contained within the subflow
    public List<FlowNode> Nodes { get; set; } = new();
    public List<FlowWire> Wires { get; set; } = new();

    // Subflow instance properties
    public Dictionary<string, object>? Meta { get; set; }

    // Icon
    public string Icon { get; set; } = "subflow.svg";

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
/// </summary>
public class SubflowPort
{
    public int X { get; set; }
    public int Y { get; set; }
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
/// </summary>
public class SubflowEnvVar
{
    public string Name { get; set; } = "";
    public string Type { get; set; } = "str";
    public object? Value { get; set; }
    public SubflowEnvVarUI? UI { get; set; }
}

/// <summary>
/// UI configuration for subflow environment variable.
/// </summary>
public class SubflowEnvVarUI
{
    public string? Type { get; set; }
    public string? Icon { get; set; }
    public string? Label { get; set; }
    public List<SubflowEnvOption>? Options { get; set; }
}

/// <summary>
/// Option for enum-type environment variable.
/// </summary>
public class SubflowEnvOption
{
    public string Label { get; set; } = "";
    public object? Value { get; set; }
}
