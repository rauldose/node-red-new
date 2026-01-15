// ============================================================
// SOURCE: packages/node_modules/@node-red/editor-client/src/js/nodes.js
//         packages/node_modules/@node-red/editor-client/src/js/ui/view.js
// ============================================================
// Editor state service managing all UI state.
// ============================================================

using System.Net.Http.Json;

namespace NodeRed.Editor.Services;

/// <summary>
/// Central state management for the editor UI.
/// Translated from multiple Node-RED editor JS files.
/// </summary>
public class EditorState
{
    private readonly HttpClient? _httpClient;

    public EditorState(HttpClient? httpClient = null)
    {
        _httpClient = httpClient;
        InitializeDefaultPalette();
    }

    // Event for notifying UI components of state changes
    public event Action? OnStateChanged;

    /// <summary>
    /// Notify all subscribers that the state has changed.
    /// Call this after modifying state that affects other components.
    /// </summary>
    public void NotifyStateChanged() => OnStateChanged?.Invoke();

    // Loading state
    public bool IsLoading { get; set; }
    public string LoadingMessage { get; set; } = "";
    public int LoadingProgress { get; set; }

    // Deployment state
    public bool IsDeploying { get; set; }
    public bool IsDirty { get; set; }
    public string? FlowsRev { get; set; }

    // UI state
    public bool PaletteOpen { get; set; } = true;
    public bool SidebarOpen { get; set; } = true;
    public bool MenuOpen { get; set; }
    public bool ShowShade { get; set; }
    public TrayConfig? CurrentTray { get; set; }

    // Version
    public string Version { get; set; } = "1.0.0";

    // Canvas state
    // ============================================================
    // SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/view.js
    // LINES: 33-50
    // ============================================================
    // var space_width = 8000,
    //     space_height = 8000,
    //     lineCurveScale = 0.75,
    //     scaleFactor = 1,
    // var gridSize = 20;
    // var snapGrid = false;
    // ============================================================
    public int CanvasWidth { get; set; } = 8000;
    public int CanvasHeight { get; set; } = 8000;
    public double Scale { get; set; } = 1.0;
    public int GridSize { get; set; } = 20;
    public bool SnapGrid { get; set; } = false;
    public bool ShowGrid { get; set; } = true;
    public double LineCurveScale { get; set; } = 0.75;
    
    // ============================================================
    // SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/view.js
    // LINES: 37-38
    // ============================================================
    // node_width = 100,
    // node_height = 30,
    // ============================================================
    public int DefaultNodeWidth { get; set; } = 100;
    public int DefaultNodeHeight { get; set; } = 30;
    public Point ScrollPosition { get; set; } = new(0, 0);
    public Point DropPosition { get; set; } = new(0, 0);
    public bool IsSelecting { get; set; }
    public Point SelectionStart { get; set; } = new(0, 0);
    public Point SelectionEnd { get; set; } = new(0, 0);
    public bool IsMultiSelect { get; set; }

    // Palette
    public List<PaletteCategory> PaletteCategories { get; set; } = new();
    public PaletteNode? DraggedNode { get; set; }

    // Context menu
    public bool ContextMenuOpen { get; set; }
    public Point ContextMenuPosition { get; set; } = new(0, 0);
    public List<ContextMenuItem> ContextMenuItems { get; set; } = new();

    // Wire drawing state
    public bool IsDrawingWire { get; set; }
    public string? WireSourceNodeId { get; set; }
    public int WireSourcePort { get; set; }
    public Point WireDrawEnd { get; set; } = new(0, 0);

    // Node dragging state
    public bool IsDraggingNode { get; set; }
    public string? DraggingNodeId { get; set; }
    public Point DragOffset { get; set; } = new(0, 0);

    // Clipboard
    public List<FlowNode> ClipboardNodes { get; set; } = new();
    public List<FlowWire> ClipboardWires { get; set; } = new();

    // Undo/Redo stacks
    public Stack<EditorAction> UndoStack { get; set; } = new();
    public Stack<EditorAction> RedoStack { get; set; } = new();

    // Flows
    public string ActiveFlowId { get; set; } = "";
    public List<Flow> Flows { get; set; } = new();
    public List<FlowNode> Nodes { get; set; } = new();
    public List<FlowWire> Wires { get; set; } = new();
    public List<ConfigNode> ConfigNodes { get; set; } = new();

    // Debug
    public List<DebugMessage> DebugMessages { get; set; } = new();

    // ============================================================
    // SECTION 17: GROUP STATE
    // SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/group.js
    // ============================================================
    public GroupManager GroupManager { get; } = new();
    public bool IsDraggingGroup { get; set; }
    public string? DraggingGroupId { get; set; }
    public bool IsResizingGroup { get; set; }
    public string? ResizingGroupId { get; set; }
    public Point LastMousePosition { get; set; } = new(0, 0);
    public NodeGroup? EditingGroup { get; set; }
    public bool ShowGroupDialog { get; set; }

    // ============================================================
    // SECTION 16: SUBFLOW STATE
    // SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/subflow.js
    // ============================================================
    public SubflowManager SubflowManager { get; } = new();
    public Subflow? EditingSubflow { get; set; }
    public bool ShowSubflowDialog { get; set; }

    // Selection
    public IEnumerable<FlowNode> SelectedNodes => Nodes.Where(n => n.Selected);

    public void MarkDirty()
    {
        IsDirty = true;
    }

    public void MarkClean()
    {
        IsDirty = false;
    }

    public async Task LoadSettingsAsync()
    {
        if (_httpClient is null) return;

        try
        {
            var settings = await _httpClient.GetFromJsonAsync<Dictionary<string, object>>("settings");
            if (settings?.TryGetValue("version", out var version) == true)
            {
                Version = version?.ToString() ?? "1.0.0";
            }
        }
        catch
        {
            // Use defaults
        }
    }

    public async Task LoadNodesAsync()
    {
        if (_httpClient is null) return;

        try
        {
            var nodes = await _httpClient.GetFromJsonAsync<List<NodeInfo>>("nodes");
            if (nodes is not null)
            {
                UpdatePaletteFromNodeList(nodes);
            }
        }
        catch
        {
            // Use default palette
        }
    }

    public async Task LoadFlowsAsync()
    {
        if (_httpClient is null) return;

        try
        {
            var response = await _httpClient.GetFromJsonAsync<FlowsResponse>("flows");
            if (response is not null)
            {
                FlowsRev = response.Rev;
                ImportFlows(response.Flows);
            }
        }
        catch
        {
            // Start with empty flows
            if (Flows.Count == 0)
            {
                var defaultFlow = new Flow
                {
                    Id = NodeRed.Util.Util.GenerateId(),
                    Label = "Flow 1",
                    Type = "tab"
                };
                Flows.Add(defaultFlow);
                ActiveFlowId = defaultFlow.Id;
            }
        }
    }

    public async Task DeployFlowsAsync()
    {
        if (_httpClient is null) return;

        try
        {
            var flowsToExport = ExportFlows();
            var request = new HttpRequestMessage(HttpMethod.Post, "flows")
            {
                Content = JsonContent.Create(new { flows = flowsToExport })
            };
            request.Headers.Add("Node-RED-API-Version", "v2");
            request.Headers.Add("Node-RED-Deployment-Type", "full");

            var response = await _httpClient.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                MarkClean();
            }
        }
        catch
        {
            // Handle error
        }
    }

    public void OpenNodeEditor(FlowNode node)
    {
        CurrentTray = new TrayConfig
        {
            Title = $"Edit {node.Type} node",
            Node = node,
            Open = true,
            ShowDelete = true,
            OnSave = () => { },
            OnDelete = () =>
            {
                Nodes.Remove(node);
                // Remove connected wires
                Wires.RemoveAll(w => w.SourceId == node.Id || w.TargetId == node.Id);
            }
        };
        ShowShade = true;
        NotifyStateChanged();
    }

    public void OpenConfigNodeEditor(ConfigNode node)
    {
        // Similar to node editor but for config nodes
        ShowShade = true;
        NotifyStateChanged();
    }

    public void CloseTray()
    {
        CurrentTray = null;
        ShowShade = false;
    }

    private void InitializeDefaultPalette()
    {
        PaletteCategories = new List<PaletteCategory>
        {
            new PaletteCategory
            {
                Name = "common",
                Nodes = new List<PaletteNode>
                {
                    new() { Type = "inject", Label = "inject", Color = "#a6bbcf", Inputs = 0, Outputs = 1 },
                    new() { Type = "debug", Label = "debug", Color = "#87a980", Inputs = 1, Outputs = 0 },
                    new() { Type = "complete", Label = "complete", Color = "#a6bbcf", Inputs = 0, Outputs = 1 },
                    new() { Type = "catch", Label = "catch", Color = "#a6bbcf", Inputs = 0, Outputs = 1 },
                    new() { Type = "status", Label = "status", Color = "#a6bbcf", Inputs = 0, Outputs = 1 },
                    new() { Type = "link in", Label = "link in", Color = "#ddd", Inputs = 0, Outputs = 1 },
                    new() { Type = "link out", Label = "link out", Color = "#ddd", Inputs = 1, Outputs = 0 },
                    new() { Type = "link call", Label = "link call", Color = "#ddd", Inputs = 1, Outputs = 1 },
                    new() { Type = "comment", Label = "comment", Color = "#fff", Inputs = 0, Outputs = 0 },
                }
            },
            new PaletteCategory
            {
                Name = "function",
                Nodes = new List<PaletteNode>
                {
                    new() { Type = "function", Label = "function", Color = "#fdd0a2", Inputs = 1, Outputs = 1 },
                    new() { Type = "switch", Label = "switch", Color = "#e2d96e", Inputs = 1, Outputs = 1 },
                    new() { Type = "change", Label = "change", Color = "#e2d96e", Inputs = 1, Outputs = 1 },
                    new() { Type = "range", Label = "range", Color = "#e2d96e", Inputs = 1, Outputs = 1 },
                    new() { Type = "template", Label = "template", Color = "#e2bc9b", Inputs = 1, Outputs = 1 },
                    new() { Type = "delay", Label = "delay", Color = "#e6b9a1", Inputs = 1, Outputs = 1 },
                    new() { Type = "trigger", Label = "trigger", Color = "#e6b9a1", Inputs = 1, Outputs = 1 },
                    new() { Type = "exec", Label = "exec", Color = "#fdd0a2", Inputs = 1, Outputs = 3 },
                    new() { Type = "rbe", Label = "filter", Color = "#e2d96e", Inputs = 1, Outputs = 1 },
                }
            },
            new PaletteCategory
            {
                Name = "network",
                Nodes = new List<PaletteNode>
                {
                    new() { Type = "mqtt in", Label = "mqtt in", Color = "#d8bfd8", Inputs = 0, Outputs = 1 },
                    new() { Type = "mqtt out", Label = "mqtt out", Color = "#d8bfd8", Inputs = 1, Outputs = 0 },
                    new() { Type = "http in", Label = "http in", Color = "#c0edc0", Inputs = 0, Outputs = 1 },
                    new() { Type = "http response", Label = "http response", Color = "#c0edc0", Inputs = 1, Outputs = 0 },
                    new() { Type = "http request", Label = "http request", Color = "#c0edc0", Inputs = 1, Outputs = 1 },
                    new() { Type = "websocket in", Label = "websocket in", Color = "#c0d0e0", Inputs = 0, Outputs = 1 },
                    new() { Type = "websocket out", Label = "websocket out", Color = "#c0d0e0", Inputs = 1, Outputs = 0 },
                    new() { Type = "tcp in", Label = "tcp in", Color = "#c0c0c0", Inputs = 0, Outputs = 1 },
                    new() { Type = "tcp out", Label = "tcp out", Color = "#c0c0c0", Inputs = 1, Outputs = 0 },
                    new() { Type = "udp in", Label = "udp in", Color = "#c0c0c0", Inputs = 0, Outputs = 1 },
                    new() { Type = "udp out", Label = "udp out", Color = "#c0c0c0", Inputs = 1, Outputs = 0 },
                }
            },
            new PaletteCategory
            {
                Name = "sequence",
                Nodes = new List<PaletteNode>
                {
                    new() { Type = "split", Label = "split", Color = "#e2d96e", Inputs = 1, Outputs = 1 },
                    new() { Type = "join", Label = "join", Color = "#e2d96e", Inputs = 1, Outputs = 1 },
                    new() { Type = "sort", Label = "sort", Color = "#e2d96e", Inputs = 1, Outputs = 1 },
                    new() { Type = "batch", Label = "batch", Color = "#e2d96e", Inputs = 1, Outputs = 1 },
                }
            },
            new PaletteCategory
            {
                Name = "parser",
                Nodes = new List<PaletteNode>
                {
                    new() { Type = "csv", Label = "csv", Color = "#eeeeee", Inputs = 1, Outputs = 1 },
                    new() { Type = "html", Label = "html", Color = "#eeeeee", Inputs = 1, Outputs = 1 },
                    new() { Type = "json", Label = "json", Color = "#eeeeee", Inputs = 1, Outputs = 1 },
                    new() { Type = "xml", Label = "xml", Color = "#eeeeee", Inputs = 1, Outputs = 1 },
                    new() { Type = "yaml", Label = "yaml", Color = "#eeeeee", Inputs = 1, Outputs = 1 },
                }
            },
            new PaletteCategory
            {
                Name = "storage",
                Nodes = new List<PaletteNode>
                {
                    new() { Type = "file", Label = "file", Color = "#e6e0f8", Inputs = 1, Outputs = 1 },
                    new() { Type = "file in", Label = "file in", Color = "#e6e0f8", Inputs = 1, Outputs = 1 },
                    new() { Type = "watch", Label = "watch", Color = "#e6e0f8", Inputs = 0, Outputs = 1 },
                }
            }
        };
    }

    private void UpdatePaletteFromNodeList(List<NodeInfo> nodes)
    {
        // Update palette with actual node list from server
        // For now, keep default palette
    }

    private void ImportFlows(List<Dictionary<string, object>>? flows)
    {
        if (flows is null) return;

        Flows.Clear();
        Nodes.Clear();
        Wires.Clear();
        ConfigNodes.Clear();

        foreach (var flowData in flows)
        {
            if (flowData.TryGetValue("type", out var type))
            {
                var typeStr = type?.ToString();
                if (typeStr == "tab")
                {
                    var flow = new Flow
                    {
                        Id = flowData.GetValueOrDefault("id")?.ToString() ?? "",
                        Label = flowData.GetValueOrDefault("label")?.ToString() ?? "Flow",
                        Type = "tab",
                        Disabled = flowData.GetValueOrDefault("disabled")?.ToString() == "true"
                    };
                    Flows.Add(flow);
                }
                else if (typeStr?.EndsWith("-config") == true)
                {
                    var configNode = new ConfigNode
                    {
                        Id = flowData.GetValueOrDefault("id")?.ToString() ?? "",
                        Type = typeStr,
                        Name = flowData.GetValueOrDefault("name")?.ToString()
                    };
                    ConfigNodes.Add(configNode);
                }
                else
                {
                    // Regular node
                    var node = new FlowNode
                    {
                        Id = flowData.GetValueOrDefault("id")?.ToString() ?? "",
                        Type = typeStr ?? "unknown",
                        Name = flowData.GetValueOrDefault("name")?.ToString(),
                        Label = flowData.GetValueOrDefault("name")?.ToString() ?? typeStr ?? "unknown",
                        FlowId = flowData.GetValueOrDefault("z")?.ToString() ?? "",
                        X = GetInt(flowData, "x", 100),
                        Y = GetInt(flowData, "y", 100),
                        Width = 120,
                        Height = 30,
                        Color = GetNodeColor(typeStr),
                        Inputs = GetInt(flowData, "inputs", typeStr == "inject" ? 0 : 1),
                        Outputs = GetInt(flowData, "outputs", typeStr == "debug" ? 0 : 1),
                        Properties = flowData
                    };
                    Nodes.Add(node);

                    // Process wires
                    if (flowData.TryGetValue("wires", out var wiresObj) && wiresObj is List<object> wiresList)
                    {
                        for (int outputPort = 0; outputPort < wiresList.Count; outputPort++)
                        {
                            if (wiresList[outputPort] is List<object> targetsList)
                            {
                                foreach (var targetId in targetsList)
                                {
                                    Wires.Add(new FlowWire
                                    {
                                        SourceId = node.Id,
                                        SourcePort = outputPort,
                                        TargetId = targetId?.ToString() ?? "",
                                        TargetPort = 0,
                                        FlowId = node.FlowId
                                    });
                                }
                            }
                        }
                    }
                }
            }
        }

        // Set active flow
        if (Flows.Count > 0 && string.IsNullOrEmpty(ActiveFlowId))
        {
            ActiveFlowId = Flows[0].Id;
        }

        // Calculate wire paths
        foreach (var wire in Wires)
        {
            wire.Path = CalculateWirePath(wire);
        }
    }

    private List<Dictionary<string, object>> ExportFlows()
    {
        var result = new List<Dictionary<string, object>>();

        // Export tabs
        foreach (var flow in Flows)
        {
            result.Add(new Dictionary<string, object>
            {
                ["id"] = flow.Id,
                ["type"] = "tab",
                ["label"] = flow.Label,
                ["disabled"] = flow.Disabled
            });
        }

        // Export nodes
        foreach (var node in Nodes)
        {
            var nodeData = new Dictionary<string, object>
            {
                ["id"] = node.Id,
                ["type"] = node.Type,
                ["z"] = node.FlowId,
                ["name"] = node.Name ?? "",
                ["x"] = node.X,
                ["y"] = node.Y
            };

            // Add wires
            var wires = new List<List<string>>();
            for (int i = 0; i < node.Outputs; i++)
            {
                var outputWires = Wires
                    .Where(w => w.SourceId == node.Id && w.SourcePort == i)
                    .Select(w => w.TargetId)
                    .ToList();
                wires.Add(outputWires);
            }
            nodeData["wires"] = wires;

            result.Add(nodeData);
        }

        return result;
    }

    private static int GetInt(Dictionary<string, object> data, string key, int defaultValue)
    {
        if (data.TryGetValue(key, out var value))
        {
            if (value is int i) return i;
            if (value is long l) return (int)l;
            if (value is double d) return (int)d;
            if (int.TryParse(value?.ToString(), out var parsed)) return parsed;
        }
        return defaultValue;
    }

    private static string GetNodeColor(string? type)
    {
        return type switch
        {
            "inject" => "#a6bbcf",
            "debug" => "#87a980",
            "function" => "#fdd0a2",
            "switch" or "change" or "range" => "#e2d96e",
            "template" => "#e2bc9b",
            "delay" or "trigger" => "#e6b9a1",
            "http in" or "http response" or "http request" => "#c0edc0",
            "mqtt in" or "mqtt out" => "#d8bfd8",
            "websocket in" or "websocket out" => "#c0d0e0",
            "tcp in" or "tcp out" or "udp in" or "udp out" => "#c0c0c0",
            "file" or "file in" or "watch" => "#e6e0f8",
            "csv" or "html" or "json" or "xml" or "yaml" => "#eeeeee",
            _ => "#ddd"
        };
    }

    private string CalculateWirePath(FlowWire wire)
    {
        var sourceNode = Nodes.FirstOrDefault(n => n.Id == wire.SourceId);
        var targetNode = Nodes.FirstOrDefault(n => n.Id == wire.TargetId);

        if (sourceNode is null || targetNode is null)
        {
            return "";
        }

        // Calculate port positions
        var sourceY = sourceNode.Outputs == 1
            ? sourceNode.Y + sourceNode.Height / 2
            : sourceNode.Y + (sourceNode.Height / (sourceNode.Outputs + 1)) * (wire.SourcePort + 1);
        var sourceX = sourceNode.X + sourceNode.Width;

        var targetX = targetNode.X;
        var targetY = targetNode.Y + targetNode.Height / 2;

        // Create bezier curve
        var dx = Math.Abs(targetX - sourceX);
        var controlOffset = Math.Max(75, dx / 2);

        return $"M {sourceX} {sourceY} C {sourceX + controlOffset} {sourceY}, {targetX - controlOffset} {targetY}, {targetX} {targetY}";
    }
}

// Supporting models
public class PaletteCategory
{
    public string Name { get; set; } = "";
    public bool Collapsed { get; set; }
    public List<PaletteNode> Nodes { get; set; } = new();
}

public class PaletteNode
{
    public string Type { get; set; } = "";
    public string? Label { get; set; }
    public string Color { get; set; } = "#ddd";
    public int Inputs { get; set; } = 1;
    public int Outputs { get; set; } = 1;
}

public class Flow
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string Type { get; set; } = "tab";
    public bool Disabled { get; set; }
}

public class FlowNode
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Name { get; set; }
    public string Label { get; set; } = "";
    public string FlowId { get; set; } = "";
    public int X { get; set; }
    public int Y { get; set; }
    // ============================================================
    // SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/view.js
    // LINES: 37-38
    // ============================================================
    // Default node dimensions: width=100, height=30
    // Width is calculated dynamically based on label length
    // ============================================================
    public int Width { get; set; } = 100;
    public int Height { get; set; } = 30;
    public string Color { get; set; } = "#ddd";
    public int Inputs { get; set; } = 1;
    public int Outputs { get; set; } = 1;
    public bool Selected { get; set; }
    public string? StatusText { get; set; }
    public string? StatusFill { get; set; }
    public Dictionary<string, object>? Properties { get; set; }
}

public class FlowWire
{
    public string SourceId { get; set; } = "";
    public int SourcePort { get; set; }
    public string TargetId { get; set; } = "";
    public int TargetPort { get; set; }
    public string FlowId { get; set; } = "";
    public string Path { get; set; } = "";
    public string Color { get; set; } = "#888";
    public bool Selected { get; set; }
}

public class ConfigNode
{
    public string Id { get; set; } = "";
    public string Type { get; set; } = "";
    public string? Name { get; set; }
    public int Users { get; set; }
}

public class DebugMessage
{
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public string Topic { get; set; } = "";
    public string Payload { get; set; } = "";
    public string Level { get; set; } = "debug";
}

public class TrayConfig
{
    public string Title { get; set; } = "";
    public FlowNode? Node { get; set; }
    public bool Open { get; set; }
    public bool ShowDelete { get; set; }
    public Action? OnSave { get; set; }
    public Action? OnDelete { get; set; }
}

public class Point
{
    public int X { get; set; }
    public int Y { get; set; }

    public Point(int x, int y)
    {
        X = x;
        Y = y;
    }
}

public class NodeInfo
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public List<string> Types { get; set; } = new();
    public bool Enabled { get; set; }
    public string? Module { get; set; }
}

public class FlowsResponse
{
    public List<Dictionary<string, object>>? Flows { get; set; }
    public string? Rev { get; set; }
}

// ============================================================
// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/menu.js
// ============================================================
public class ContextMenuItem
{
    public string Id { get; set; } = "";
    public string Label { get; set; } = "";
    public string? Icon { get; set; }
    public string? Shortcut { get; set; }
    public bool Disabled { get; set; }
    public bool IsSeparator { get; set; }
    public Action? Action { get; set; }
}

// ============================================================
// SOURCE: packages/node_modules/@node-red/editor-client/src/js/history.js
// ============================================================
public class EditorAction
{
    public string Type { get; set; } = "";
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public List<FlowNode>? Nodes { get; set; }
    public List<FlowWire>? Wires { get; set; }
    public Dictionary<string, object>? Before { get; set; }
    public Dictionary<string, object>? After { get; set; }
}

// ============================================================
// SOURCE: packages/node_modules/@node-red/editor-client/src/js/ui/search.js
// ============================================================
public class SearchResult
{
    public string NodeId { get; set; } = "";
    public string Type { get; set; } = "";
    public string Label { get; set; } = "";
    public string Color { get; set; } = "#ddd";
    public string FlowId { get; set; } = "";
    public string FlowLabel { get; set; } = "";
    public bool IsFlow { get; set; }
}
