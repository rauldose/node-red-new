// ============================================================
// SOURCE: packages/node_modules/@node-red/runtime/lib/flows/index.js
// SOURCE: packages/node_modules/@node-red/runtime/lib/flows/Flow.js
// ============================================================
// Copyright JS Foundation and other contributors, http://js.foundation
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ============================================================

using System.Collections.Concurrent;
using System.Text.Json;
using NodeRed.Util;

namespace NodeRed.Runtime;

/// <summary>
/// Represents the deployment type for flows.
/// </summary>
public enum DeployType
{
    Full,
    Nodes,
    Flows,
    Reload
}

/// <summary>
/// Configuration for a flow.
/// </summary>
public class FlowConfig
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = "tab";
    public string Label { get; set; } = string.Empty;
    public bool Disabled { get; set; }
    public string? Info { get; set; }
}

/// <summary>
/// Node status representation.
/// </summary>
public class NodeStatus
{
    public string? Fill { get; set; }
    public string? Shape { get; set; }
    public string? Text { get; set; }
}

/// <summary>
/// Interface for flow nodes.
/// </summary>
public interface IFlowNode
{
    string Id { get; }
    string Type { get; }
    string? Name { get; }
    Flow? Flow { get; set; }
    
    Task StartAsync();
    Task StopAsync(bool removed = false);
    Task ReceiveAsync(FlowMessage msg);
    T? GetProperty<T>(string name);
    void SetStatus(NodeStatus status);
}

/// <summary>
/// Represents a diff between flow configurations.
/// </summary>
public class FlowDiff
{
    public List<string> Added { get; set; } = new();
    public List<string> Changed { get; set; } = new();
    public List<string> Removed { get; set; } = new();
    public List<string> Rewired { get; set; } = new();
    public List<string> Linked { get; set; } = new();
}

/// <summary>
/// Represents a single flow (tab) in the editor.
/// </summary>
public class Flow
{
    public const string TYPE = "flow";
    
    public Flow? Parent { get; }
    public Flow? Global { get; }
    public FlowConfig Config { get; }
    public bool IsGlobalFlow { get; }
    public string Id => Config.Id;
    public bool Disabled => Config.Disabled;
    public string Path { get; }
    
    private readonly ConcurrentDictionary<string, IFlowNode> _activeNodes = new();
    private readonly List<IFlowNode> _catchNodes = new();
    private readonly List<IFlowNode> _statusNodes = new();
    private readonly List<IFlowNode> _completeNodes = new();
    private readonly object _lock = new();

    public Flow(Flow? parent, Flow? globalFlow, FlowConfig? flow = null)
    {
        Parent = parent;
        Global = globalFlow;
        
        if (flow is null && globalFlow is not null)
        {
            Config = new FlowConfig { Id = globalFlow.Id };
            IsGlobalFlow = true;
        }
        else
        {
            Config = flow ?? new FlowConfig { Id = NodeRed.Util.Util.GenerateId() };
            IsGlobalFlow = false;
        }
        
        Path = Config.Id;
    }

    public async Task StartAsync(FlowDiff? diff = null)
    {
        if (Disabled) return;

        Log.LogDebug($"Starting flow: {Id}");
        
        foreach (var node in _activeNodes.Values)
        {
            try
            {
                await node.StartAsync();
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to start node {node.Id}: {ex.Message}");
            }
        }
    }

    public async Task StopAsync(IEnumerable<string>? stopList = null, IEnumerable<string>? removedList = null)
    {
        Log.LogDebug($"Stopping flow: {Id}");
        
        var nodesToStop = stopList is not null 
            ? _activeNodes.Values.Where(n => stopList.Contains(n.Id))
            : _activeNodes.Values;
        
        var tasks = nodesToStop.Select(node => StopNodeAsync(node, removedList?.Contains(node.Id) ?? false));
        await Task.WhenAll(tasks);
    }
    
    private async Task StopNodeAsync(IFlowNode node, bool removed)
    {
        try
        {
            await node.StopAsync(removed);
            if (removed)
            {
                _activeNodes.TryRemove(node.Id, out _);
            }
        }
        catch (Exception ex)
        {
            Log.LogError($"Failed to stop node {node.Id}: {ex.Message}");
        }
    }

    public IFlowNode? GetNode(string id) => _activeNodes.TryGetValue(id, out var node) ? node : null;

    public IReadOnlyDictionary<string, IFlowNode> GetActiveNodes() => _activeNodes;

    public void AddNode(IFlowNode node)
    {
        _activeNodes[node.Id] = node;
        
        lock (_lock)
        {
            switch (node.Type.ToLowerInvariant())
            {
                case "catch": _catchNodes.Add(node); break;
                case "status": _statusNodes.Add(node); break;
                case "complete": _completeNodes.Add(node); break;
            }
        }
    }

    public async Task HandleErrorAsync(IFlowNode node, string message, FlowMessage? msg, bool reportable = true)
    {
        if (reportable)
        {
            Log.LogError($"[{node.Type}:{node.Id}] {message}");
        }
        
        var catchNodes = GetCatchNodesForNode(node);
        if (catchNodes.Count == 0 && Parent is not null)
        {
            await Parent.HandleErrorAsync(node, message, msg, false);
            return;
        }
        
        foreach (var catchNode in catchNodes)
        {
            var errorMsg = msg?.Clone() ?? new FlowMessage();
            errorMsg.Set("error", new
            {
                message,
                source = new { id = node.Id, type = node.Type, name = node.Name }
            });
            
            await catchNode.ReceiveAsync(errorMsg);
        }
    }
    
    private List<IFlowNode> GetCatchNodesForNode(IFlowNode node)
    {
        lock (_lock)
        {
            return _catchNodes.Where(c => CanCatchForNode(c, node)).ToList();
        }
    }
    
    private static bool CanCatchForNode(IFlowNode catchNode, IFlowNode targetNode)
    {
        var scope = catchNode.GetProperty<string[]>("scope");
        if (scope is null || scope.Length == 0)
        {
            return true;
        }
        return scope.Contains(targetNode.Id);
    }

    public async Task HandleStatusAsync(IFlowNode node, NodeStatus status)
    {
        List<IFlowNode> statusNodes;
        lock (_lock)
        {
            statusNodes = _statusNodes.ToList();
        }
        
        foreach (var statusNode in statusNodes)
        {
            var scope = statusNode.GetProperty<string[]>("scope");
            if (scope is not null && scope.Length > 0 && !scope.Contains(node.Id))
            {
                continue;
            }
            
            var msg = new FlowMessage();
            msg.Set("status", status);
            msg.Set("source", new { id = node.Id, type = node.Type, name = node.Name });
            
            await statusNode.ReceiveAsync(msg);
        }
    }

    public async Task HandleCompleteAsync(IFlowNode node, FlowMessage msg)
    {
        List<IFlowNode> completeNodes;
        lock (_lock)
        {
            completeNodes = _completeNodes.ToList();
        }
        
        foreach (var completeNode in completeNodes)
        {
            var scope = completeNode.GetProperty<string[]>("scope");
            if (scope is not null && scope.Length > 0 && !scope.Contains(node.Id))
            {
                continue;
            }
            
            await completeNode.ReceiveAsync(msg.Clone());
        }
    }
}

/// <summary>
/// Manages all flows in the runtime.
/// </summary>
public class FlowManager
{
    private readonly ConcurrentDictionary<string, Flow> _activeFlows = new();
    private readonly ConcurrentDictionary<string, string> _activeNodesToFlow = new();
    private bool _started;
    private List<Dictionary<string, object?>> _activeConfig = new();
    private readonly IRuntime _runtime;
    private readonly Events _events;

    public FlowManager(IRuntime runtime)
    {
        _runtime = runtime;
        _events = runtime.Events;
    }

    public bool IsStarted => _started;
    public IReadOnlyList<Dictionary<string, object?>> ActiveConfig => _activeConfig;

    public async Task LoadAsync(bool forceStart = false)
    {
        var config = await _runtime.Storage.GetFlowsAsync();
        if (config is not null)
        {
            await SetFlowsAsync(config, null, DeployType.Reload, false, forceStart);
        }
    }

    public async Task SetFlowsAsync(
        List<Dictionary<string, object?>> config,
        Dictionary<string, object?>? credentials,
        DeployType deploymentType,
        bool muteLog = false,
        bool forceStart = false)
    {
        var configClone = CloneConfig(config);
        var diff = CalculateDiff(_activeConfig, configClone, deploymentType);
        
        await StopFlowsAsync(diff, deploymentType);
        
        _activeConfig = configClone;
        await _runtime.Storage.SaveFlowsAsync(_activeConfig);
        
        if (forceStart || _started)
        {
            await StartFlowsAsync(diff, deploymentType);
        }
        
        _events.Emit("flows:deploy", new NodeRedEventArgs(new { type = deploymentType.ToString() }));
        
        if (!muteLog)
        {
            Log.LogInfo($"Flows deployed ({deploymentType})");
        }
    }

    public async Task StartFlowsAsync(FlowDiff? diff = null, DeployType type = DeployType.Full)
    {
        _started = true;
        var flows = ParseFlowsFromConfig(_activeConfig);
        
        foreach (var flowConfig in flows)
        {
            if (type == DeployType.Full || diff is null || diff.Added.Contains(flowConfig.Id) || diff.Changed.Contains(flowConfig.Id))
            {
                var flow = new Flow(null, null, flowConfig);
                _activeFlows[flowConfig.Id] = flow;
                await flow.StartAsync(diff);
            }
        }
        
        _events.Emit("flows:started", new NodeRedEventArgs(null));
    }
    
    public async Task StopFlowsAsync(FlowDiff? diff = null, DeployType type = DeployType.Full)
    {
        var tasks = new List<Task>();
        
        foreach (var flow in _activeFlows.Values)
        {
            if (type == DeployType.Full || diff is null || diff.Removed.Contains(flow.Id) || diff.Changed.Contains(flow.Id))
            {
                tasks.Add(flow.StopAsync());
            }
        }
        
        await Task.WhenAll(tasks);
        
        if (type == DeployType.Full)
        {
            _activeFlows.Clear();
            _started = false;
        }
        
        _events.Emit("flows:stopped", new NodeRedEventArgs(null));
    }

    public List<Dictionary<string, object?>> GetFlows() => _activeConfig;

    public IFlowNode? GetNode(string id)
    {
        if (_activeNodesToFlow.TryGetValue(id, out var flowId))
        {
            if (_activeFlows.TryGetValue(flowId, out var flow))
            {
                return flow.GetNode(id);
            }
        }
        return null;
    }

    public Flow? GetFlow(string id) => _activeFlows.TryGetValue(id, out var flow) ? flow : null;

    public async Task<string> AddFlowAsync(Dictionary<string, object?> flow)
    {
        var id = flow.TryGetValue("id", out var idVal) ? idVal?.ToString() : null;
        if (string.IsNullOrEmpty(id))
        {
            id = NodeRed.Util.Util.GenerateId();
            flow["id"] = id;
        }
        
        _activeConfig.Add(flow);
        await _runtime.Storage.SaveFlowsAsync(_activeConfig);
        
        return id;
    }

    public async Task UpdateFlowAsync(string id, Dictionary<string, object?> newFlow)
    {
        var index = _activeConfig.FindIndex(f => f.TryGetValue("id", out var fId) && fId?.ToString() == id);
        if (index >= 0)
        {
            newFlow["id"] = id;
            _activeConfig[index] = newFlow;
            await _runtime.Storage.SaveFlowsAsync(_activeConfig);
        }
    }

    public async Task RemoveFlowAsync(string id)
    {
        _activeConfig.RemoveAll(f => f.TryGetValue("id", out var fId) && fId?.ToString() == id);
        await _runtime.Storage.SaveFlowsAsync(_activeConfig);
        
        if (_activeFlows.TryRemove(id, out var flow))
        {
            await flow.StopAsync();
        }
    }

    private static List<Dictionary<string, object?>> CloneConfig(List<Dictionary<string, object?>> config)
    {
        var json = JsonSerializer.Serialize(config);
        return JsonSerializer.Deserialize<List<Dictionary<string, object?>>>(json) ?? new();
    }

    private static FlowDiff CalculateDiff(List<Dictionary<string, object?>> oldConfig, List<Dictionary<string, object?>> newConfig, DeployType type)
    {
        var diff = new FlowDiff();
        
        if (type == DeployType.Full || type == DeployType.Reload)
        {
            foreach (var item in newConfig)
            {
                if (item.TryGetValue("id", out var id) && id is not null)
                {
                    diff.Added.Add(id.ToString()!);
                }
            }
            foreach (var item in oldConfig)
            {
                if (item.TryGetValue("id", out var id) && id is not null)
                {
                    diff.Removed.Add(id.ToString()!);
                }
            }
            return diff;
        }
        
        var oldIds = oldConfig.Where(c => c.ContainsKey("id")).Select(c => c["id"]?.ToString()).Where(id => id is not null).ToHashSet();
        var newIds = newConfig.Where(c => c.ContainsKey("id")).Select(c => c["id"]?.ToString()).Where(id => id is not null).ToHashSet();
        
        foreach (var id in newIds)
        {
            if (!oldIds.Contains(id))
            {
                diff.Added.Add(id!);
            }
            else
            {
                var oldItem = oldConfig.FirstOrDefault(c => c.TryGetValue("id", out var cId) && cId?.ToString() == id);
                var newItem = newConfig.FirstOrDefault(c => c.TryGetValue("id", out var cId) && cId?.ToString() == id);
                if (oldItem is not null && newItem is not null)
                {
                    var oldJson = JsonSerializer.Serialize(oldItem);
                    var newJson = JsonSerializer.Serialize(newItem);
                    if (oldJson != newJson)
                    {
                        diff.Changed.Add(id!);
                    }
                }
            }
        }
        
        foreach (var id in oldIds)
        {
            if (!newIds.Contains(id))
            {
                diff.Removed.Add(id!);
            }
        }
        
        return diff;
    }

    private static List<FlowConfig> ParseFlowsFromConfig(List<Dictionary<string, object?>> config)
    {
        return config
            .Where(c => c.TryGetValue("type", out var t) && t?.ToString() == "tab")
            .Select(c => new FlowConfig
            {
                Id = c.TryGetValue("id", out var id) ? id?.ToString() ?? "" : "",
                Type = "tab",
                Label = c.TryGetValue("label", out var label) ? label?.ToString() ?? "" : "",
                Disabled = c.TryGetValue("disabled", out var disabled) && disabled is bool b && b,
                Info = c.TryGetValue("info", out var info) ? info?.ToString() : null
            })
            .ToList();
    }
}
