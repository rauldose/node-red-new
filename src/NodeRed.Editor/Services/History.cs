// ============================================================
// SOURCE: packages/node_modules/@node-red/editor-client/src/js/history.js
// ============================================================
// Undo/Redo history system for editor actions.
// ============================================================

namespace NodeRed.Editor.Services;

/// <summary>
/// History management for undo/redo operations.
/// Translated from Node-RED history.js
/// </summary>
public class History
{
    private readonly Stack<HistoryEvent> _undoStack = new();
    private readonly Stack<HistoryEvent> _redoStack = new();
    private readonly int _maxStackSize;

    public event EventHandler? Changed;

    public History(int maxStackSize = 100)
    {
        _maxStackSize = maxStackSize;
    }

    public bool CanUndo => _undoStack.Count > 0;
    public bool CanRedo => _redoStack.Count > 0;
    public int UndoCount => _undoStack.Count;
    public int RedoCount => _redoStack.Count;

    /// <summary>
    /// Push a new event onto the undo stack.
    /// Clears the redo stack.
    /// </summary>
    public void Push(HistoryEvent evt)
    {
        _undoStack.Push(evt);
        _redoStack.Clear();

        // Limit stack size
        while (_undoStack.Count > _maxStackSize)
        {
            var temp = new Stack<HistoryEvent>();
            for (int i = 0; i < _maxStackSize; i++)
            {
                temp.Push(_undoStack.Pop());
            }
            _undoStack.Clear();
            while (temp.Count > 0)
            {
                _undoStack.Push(temp.Pop());
            }
        }

        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Pop an event from the undo stack and execute its undo action.
    /// </summary>
    public HistoryEvent? Undo()
    {
        if (!CanUndo) return null;

        var evt = _undoStack.Pop();
        _redoStack.Push(evt);
        Changed?.Invoke(this, EventArgs.Empty);
        return evt;
    }

    /// <summary>
    /// Pop an event from the redo stack and execute its redo action.
    /// </summary>
    public HistoryEvent? Redo()
    {
        if (!CanRedo) return null;

        var evt = _redoStack.Pop();
        _undoStack.Push(evt);
        Changed?.Invoke(this, EventArgs.Empty);
        return evt;
    }

    /// <summary>
    /// Clear both stacks.
    /// </summary>
    public void Clear()
    {
        _undoStack.Clear();
        _redoStack.Clear();
        Changed?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Get a description of the current undo action.
    /// </summary>
    public string? GetUndoDescription()
    {
        if (!CanUndo) return null;
        return _undoStack.Peek().Description;
    }

    /// <summary>
    /// Get a description of the current redo action.
    /// </summary>
    public string? GetRedoDescription()
    {
        if (!CanRedo) return null;
        return _redoStack.Peek().Description;
    }
}

/// <summary>
/// A single history event that can be undone/redone.
/// </summary>
public class HistoryEvent
{
    public HistoryEventType Type { get; set; }
    public string Description { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;

    // Store the state before and after the change
    public List<NodeSnapshot>? NodesBefore { get; set; }
    public List<NodeSnapshot>? NodesAfter { get; set; }
    public List<WireSnapshot>? WiresBefore { get; set; }
    public List<WireSnapshot>? WiresAfter { get; set; }
    public List<FlowSnapshot>? FlowsBefore { get; set; }
    public List<FlowSnapshot>? FlowsAfter { get; set; }

    // For move operations
    public List<(string Id, int DeltaX, int DeltaY)>? MoveDelta { get; set; }

    // For property changes
    public Dictionary<string, object?>? PropertiesBefore { get; set; }
    public Dictionary<string, object?>? PropertiesAfter { get; set; }
}

public enum HistoryEventType
{
    Add,
    Delete,
    Move,
    Edit,
    Connect,
    Disconnect,
    CreateFlow,
    DeleteFlow,
    EditFlow,
    Import,
    Group,
    Ungroup
}

/// <summary>
/// Snapshot of a node for history storage.
/// </summary>
public class NodeSnapshot
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Name { get; set; }
    public string FlowId { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public string Color { get; set; } = "#ddd";
    public int Inputs { get; set; }
    public int Outputs { get; set; }
    public Dictionary<string, object>? Properties { get; set; }

    public static NodeSnapshot FromNode(FlowNode node)
    {
        return new NodeSnapshot
        {
            Id = node.Id,
            Type = node.Type,
            Name = node.Name,
            FlowId = node.FlowId,
            X = node.X,
            Y = node.Y,
            Width = node.Width,
            Height = node.Height,
            Color = node.Color,
            Inputs = node.Inputs,
            Outputs = node.Outputs,
            Properties = node.Properties != null ? new Dictionary<string, object>(node.Properties) : null
        };
    }

    public FlowNode ToNode()
    {
        return new FlowNode
        {
            Id = Id,
            Type = Type,
            Name = Name,
            Label = Name ?? Type,
            FlowId = FlowId,
            X = X,
            Y = Y,
            Width = Width,
            Height = Height,
            Color = Color,
            Inputs = Inputs,
            Outputs = Outputs,
            Properties = Properties != null ? new Dictionary<string, object>(Properties) : null
        };
    }
}

/// <summary>
/// Snapshot of a wire for history storage.
/// </summary>
public class WireSnapshot
{
    public string SourceId { get; set; } = "";
    public int SourcePort { get; set; }
    public string TargetId { get; set; } = "";
    public int TargetPort { get; set; }
    public string FlowId { get; set; } = "";

    public static WireSnapshot FromWire(FlowWire wire)
    {
        return new WireSnapshot
        {
            SourceId = wire.SourceId,
            SourcePort = wire.SourcePort,
            TargetId = wire.TargetId,
            TargetPort = wire.TargetPort,
            FlowId = wire.FlowId
        };
    }

    public FlowWire ToWire()
    {
        return new FlowWire
        {
            SourceId = SourceId,
            SourcePort = SourcePort,
            TargetId = TargetId,
            TargetPort = TargetPort,
            FlowId = FlowId
        };
    }
}

/// <summary>
/// Snapshot of a flow/tab for history storage.
/// </summary>
public class FlowSnapshot
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public bool Disabled { get; set; }

    public static FlowSnapshot FromFlow(Flow flow)
    {
        return new FlowSnapshot
        {
            Id = flow.Id,
            Label = flow.Label,
            Disabled = flow.Disabled
        };
    }

    public Flow ToFlow()
    {
        return new Flow
        {
            Id = Id,
            Label = Label,
            Type = "tab",
            Disabled = Disabled
        };
    }
}
