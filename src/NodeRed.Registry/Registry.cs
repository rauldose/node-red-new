// ============================================================
// SOURCE: packages/node_modules/@node-red/registry/lib/registry.js
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

using System.Text.Json;
using NodeRed.Util;

namespace NodeRed.Registry;

/// <summary>
/// Node information structure.
/// Translated from filterNodeInfo() in registry.js
/// </summary>
public class NodeInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> Types { get; set; } = new();
    public bool Enabled { get; set; }
    public bool Local { get; set; }
    public bool User { get; set; }
    public string? Module { get; set; }
    public string? Version { get; set; }
    public string? PendingVersion { get; set; }
    public object? Err { get; set; }
    public bool Loaded { get; set; }
    public List<PluginInfo>? Plugins { get; set; }
    public bool? Editor { get; set; }
    public bool? Runtime { get; set; }
}

/// <summary>
/// Plugin information.
/// </summary>
public class PluginInfo
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
}

/// <summary>
/// Module configuration.
/// </summary>
public class ModuleConfig
{
    public string Name { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string? PendingVersion { get; set; }
    public bool Local { get; set; }
    public bool User { get; set; }
    public string? Path { get; set; }
    public Dictionary<string, NodeSetConfig> Nodes { get; set; } = new();
    public Dictionary<string, PluginConfig>? Plugins { get; set; }
    public List<IconConfig>? Icons { get; set; }
    public ExamplesConfig? Examples { get; set; }
    public ResourcesConfig? Resources { get; set; }
    public List<string>? Dependencies { get; set; }
    public List<string>? UsedBy { get; set; }
}

/// <summary>
/// Node set configuration.
/// </summary>
public class NodeSetConfig
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Module { get; set; }
    public List<string> Types { get; set; } = new();
    public bool Enabled { get; set; } = true;
    public bool Loaded { get; set; }
    public bool Local { get; set; }
    public bool User { get; set; }
    public string? File { get; set; }
    public string? Template { get; set; }
    public string? Config { get; set; }
    public object? Err { get; set; }
    public List<PluginConfig>? Plugins { get; set; }
    public string? Type { get; set; }
}

/// <summary>
/// Plugin configuration.
/// </summary>
public class PluginConfig
{
    public string Id { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Template { get; set; }
    public string? File { get; set; }
}

/// <summary>
/// Icon configuration.
/// </summary>
public class IconConfig
{
    public string Path { get; set; } = string.Empty;
    public List<string> Icons { get; set; } = new();
}

/// <summary>
/// Examples configuration.
/// </summary>
public class ExamplesConfig
{
    public string Path { get; set; } = string.Empty;
}

/// <summary>
/// Resources configuration.
/// </summary>
public class ResourcesConfig
{
    public string Path { get; set; } = string.Empty;
}

/// <summary>
/// Module info returned by getModuleInfo.
/// </summary>
public class ModuleInfo
{
    public string Name { get; set; } = string.Empty;
    public string? Version { get; set; }
    public string? PendingVersion { get; set; }
    public bool Local { get; set; }
    public bool User { get; set; }
    public string? Path { get; set; }
    public List<NodeInfo> Nodes { get; set; } = new();
    public List<NodeInfo> Plugins { get; set; } = new();
    public List<string>? Dependencies { get; set; }
}

/// <summary>
/// Node constructor delegate.
/// </summary>
public delegate object NodeConstructor(object config);

/// <summary>
/// Node options.
/// </summary>
public class NodeOptions
{
    public Func<string, Task<IEnumerable<string>>>? DynamicModuleList { get; set; }
}

/// <summary>
/// Settings interface for registry.
/// </summary>
public interface IRegistrySettings
{
    bool Available();
    object? Get(string prop);
    Task SetAsync(string prop, object? value);
    void EnableNodeSettings(IEnumerable<string> types);
    void DisableNodeSettings(IEnumerable<string> types);
}

/// <summary>
/// Loader interface for registry.
/// </summary>
public interface INodeLoader
{
    string? GetNodeHelp(NodeSetConfig config, string lang);
}

/// <summary>
/// Node registry for managing node modules and types.
/// Translated from: @node-red/registry/lib/registry.js
/// </summary>
public class NodeRegistry
{
    // ============================================================
    // ORIGINAL CODE (lines 28-36):
    // ------------------------------------------------------------
    // var nodeConfigCache = {};
    // var moduleConfigs = {};
    // var nodeList = [];
    // var nodeConstructors = {};
    // var nodeOptions = {};
    // var subflowModules = {};
    // var nodeTypeToId = {};
    // var moduleNodes = {};
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    private readonly Dictionary<string, string> _nodeConfigCache = new();
    private Dictionary<string, ModuleConfig> _moduleConfigs = new();
    private List<string> _nodeList = new();
    private readonly Dictionary<string, NodeConstructor> _nodeConstructors = new();
    private readonly Dictionary<string, NodeOptions> _nodeOptions = new();
    private readonly Dictionary<string, object> _subflowModules = new();
    private readonly Dictionary<string, string> _nodeTypeToId = new();
    private readonly Dictionary<string, List<string>> _moduleNodes = new();
    private readonly Dictionary<string, List<string>> _iconPaths = new();
    private readonly Dictionary<string, string> _iconCache = new();
    // ============================================================

    private IRegistrySettings? _settings;
    private INodeLoader? _loader;
    private readonly Events _events = new();
    private readonly object _lock = new();

    /// <summary>
    /// Events emitter.
    /// </summary>
    public Events Events => _events;

    // ============================================================
    // ORIGINAL CODE (lines 38-42):
    // ------------------------------------------------------------
    // function init(_settings,_loader) {
    //     settings = _settings;
    //     loader = _loader;
    //     clear();
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Initialize the registry.
    /// </summary>
    public void Init(IRegistrySettings settings, INodeLoader? loader = null)
    {
        _settings = settings;
        _loader = loader;
        Clear();
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 44-50):
    // ------------------------------------------------------------
    // function load() {
    //     if (settings.available()) {
    //         moduleConfigs = loadNodeConfigs();
    //     } else {
    //         moduleConfigs = {};
    //     }
    // }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Load node configurations from settings.
    /// </summary>
    public void Load()
    {
        lock (_lock)
        {
            if (_settings?.Available() == true)
            {
                _moduleConfigs = LoadNodeConfigs();
            }
            else
            {
                _moduleConfigs = new Dictionary<string, ModuleConfig>();
            }
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 52-81):
    // ------------------------------------------------------------
    // function filterNodeInfo(n) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Filter node info for external consumption.
    /// </summary>
    public static NodeInfo FilterNodeInfo(NodeSetConfig n)
    {
        var r = new NodeInfo
        {
            Id = !string.IsNullOrEmpty(n.Id) ? n.Id : $"{n.Module}/{n.Name}",
            Name = n.Name,
            Types = new List<string>(n.Types),
            Enabled = n.Enabled,
            Local = n.Local,
            User = n.User
        };

        if (!string.IsNullOrEmpty(n.Module))
        {
            r.Module = n.Module;
        }

        if (n.Err is not null)
        {
            r.Err = n.Err;
        }

        if (n.Plugins is not null)
        {
            r.Plugins = n.Plugins.Select(p => new PluginInfo
            {
                Id = p.Id,
                Type = p.Type,
                Module = p.Module
            }).ToList();
        }

        if (n.Type == "plugin")
        {
            r.Editor = !string.IsNullOrEmpty(n.Template);
            r.Runtime = !string.IsNullOrEmpty(n.File);
        }

        return r;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 85-93):
    // ------------------------------------------------------------
    // function getModuleFromSetId(id) { ... }
    // function getNodeFromSetId(id) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get module name from set ID.
    /// </summary>
    public static string GetModuleFromSetId(string id)
    {
        var parts = id.Split('/');
        return string.Join("/", parts.Take(parts.Length - 1));
    }

    /// <summary>
    /// Get node name from set ID.
    /// </summary>
    public static string GetNodeFromSetId(string id)
    {
        var parts = id.Split('/');
        return parts[^1];
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 147-189):
    // ------------------------------------------------------------
    // function loadNodeConfigs() { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    private Dictionary<string, ModuleConfig> LoadNodeConfigs()
    {
        var configsRaw = _settings?.Get("nodes");
        if (configsRaw is null)
        {
            return new Dictionary<string, ModuleConfig>();
        }

        try
        {
            var json = JsonSerializer.Serialize(configsRaw);
            var configs = JsonSerializer.Deserialize<Dictionary<string, ModuleConfig>>(json);
            return configs ?? new Dictionary<string, ModuleConfig>();
        }
        catch
        {
            return new Dictionary<string, ModuleConfig>();
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 191-236):
    // ------------------------------------------------------------
    // function addModule(module) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Add a module to the registry.
    /// </summary>
    public void AddModule(ModuleConfig module)
    {
        lock (_lock)
        {
            _moduleNodes[module.Name] = new List<string>();
            _moduleConfigs[module.Name] = module;

            foreach (var kvp in module.Nodes)
            {
                var setName = kvp.Key;
                var set = kvp.Value;

                if (set.Types.Count == 0)
                {
                    set.Err = new NodeRedException("Set has no types", "set_has_no_types");
                }

                _moduleNodes[module.Name].Add(set.Name);
                _nodeList.Add(set.Id);

                if (set.Err is null)
                {
                    foreach (var t in set.Types)
                    {
                        if (_nodeTypeToId.ContainsKey(t))
                        {
                            var existingInfo = GetNodeInfo(t);
                            set.Err = new NodeRedException("Type already registered", "type_already_registered")
                            {
                                Data = { ["type"] = t, ["moduleA"] = existingInfo?.Module, ["moduleB"] = set.Module }
                            };
                            break;
                        }
                    }

                    if (set.Err is null)
                    {
                        foreach (var t in set.Types)
                        {
                            _nodeTypeToId[t] = set.Id;
                        }
                    }
                }
            }

            if (module.Icons is not null)
            {
                if (!_iconPaths.ContainsKey(module.Name))
                {
                    _iconPaths[module.Name] = new List<string>();
                }
                foreach (var icon in module.Icons)
                {
                    _iconPaths[module.Name].Add(Path.GetFullPath(icon.Path));
                }
            }

            _nodeConfigCache.Clear();
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 308-333):
    // ------------------------------------------------------------
    // function getNodeInfo(typeOrId) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get node info by type or ID.
    /// </summary>
    public NodeInfo? GetNodeInfo(string typeOrId)
    {
        lock (_lock)
        {
            var id = typeOrId;
            if (_nodeTypeToId.TryGetValue(typeOrId, out var mappedId))
            {
                id = mappedId;
            }

            var moduleName = GetModuleFromSetId(id);
            if (_moduleConfigs.TryGetValue(moduleName, out var module))
            {
                var nodeName = GetNodeFromSetId(id);
                if (module.Nodes.TryGetValue(nodeName, out var config))
                {
                    var info = FilterNodeInfo(config);
                    info.Loaded = config.Loaded;
                    info.Version = module.Version;
                    if (!string.IsNullOrEmpty(module.PendingVersion))
                    {
                        info.PendingVersion = module.PendingVersion;
                    }
                    return info;
                }
            }

            return null;
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 352-377):
    // ------------------------------------------------------------
    // function getNodeList(filter) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get list of all nodes, optionally filtered.
    /// </summary>
    public List<NodeInfo> GetNodeList(Func<NodeSetConfig, bool>? filter = null)
    {
        var list = new List<NodeInfo>();

        lock (_lock)
        {
            foreach (var moduleKvp in _moduleConfigs)
            {
                var module = moduleKvp.Value;

                // Skip non-user modules that are only used by other modules
                if (!module.User && module.UsedBy?.Count > 0)
                {
                    continue;
                }

                foreach (var nodeKvp in module.Nodes)
                {
                    var node = nodeKvp.Value;

                    if (filter is null || filter(node))
                    {
                        var nodeInfo = FilterNodeInfo(node);
                        nodeInfo.Version = module.Version;
                        if (!string.IsNullOrEmpty(module.PendingVersion))
                        {
                            nodeInfo.PendingVersion = module.PendingVersion;
                        }
                        list.Add(nodeInfo);
                    }
                }
            }
        }

        return list;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 379-384):
    // ------------------------------------------------------------
    // function getModuleList() { return moduleConfigs; }
    // function getModule(id) { return moduleConfigs[id]; }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get all module configurations.
    /// </summary>
    public Dictionary<string, ModuleConfig> GetModuleList()
    {
        lock (_lock)
        {
            return new Dictionary<string, ModuleConfig>(_moduleConfigs);
        }
    }

    /// <summary>
    /// Get a specific module configuration.
    /// </summary>
    public ModuleConfig? GetModule(string id)
    {
        lock (_lock)
        {
            return _moduleConfigs.GetValueOrDefault(id);
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 386-421):
    // ------------------------------------------------------------
    // function getModuleInfo(module) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get module info.
    /// </summary>
    public ModuleInfo? GetModuleInfo(string module)
    {
        lock (_lock)
        {
            if (!_moduleNodes.TryGetValue(module, out var nodes))
            {
                return null;
            }

            var config = _moduleConfigs[module];
            var m = new ModuleInfo
            {
                Name = module,
                Version = config.Version,
                Local = config.Local,
                User = config.User,
                Path = config.Path,
                Dependencies = config.Dependencies
            };

            if (!string.IsNullOrEmpty(config.PendingVersion))
            {
                m.PendingVersion = config.PendingVersion;
            }

            foreach (var nodeName in nodes)
            {
                if (config.Nodes.TryGetValue(nodeName, out var nodeConfig))
                {
                    var nodeInfo = FilterNodeInfo(nodeConfig);
                    nodeInfo.Version = m.Version;
                    m.Nodes.Add(nodeInfo);
                }
            }

            if (config.Plugins is not null)
            {
                foreach (var plugin in config.Plugins.Values)
                {
                    var pluginInfo = new NodeInfo
                    {
                        Id = plugin.Id,
                        Name = plugin.Id,
                        Types = new List<string> { plugin.Type },
                        Version = m.Version,
                        Module = plugin.Module
                    };
                    m.Plugins.Add(pluginInfo);
                }
            }

            return m;
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 435-462):
    // ------------------------------------------------------------
    // function registerNodeConstructor(nodeSet,type,constructor,options) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Register a node constructor.
    /// </summary>
    public void RegisterNodeConstructor(string nodeSet, string type, NodeConstructor constructor, NodeOptions? options = null)
    {
        lock (_lock)
        {
            if (_nodeConstructors.ContainsKey(type))
            {
                throw new InvalidOperationException($"{type} already registered");
            }

            var nodeSetInfo = GetFullNodeInfo(nodeSet);
            if (nodeSetInfo is not null)
            {
                if (!nodeSetInfo.Types.Contains(type))
                {
                    nodeSetInfo.Types.Add(type);
                }
            }

            _nodeConstructors[type] = constructor;
            if (options is not null)
            {
                _nodeOptions[type] = options;
            }

            _events.Emit("type-registered", new NodeRedEventArgs(type));
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 535-549):
    // ------------------------------------------------------------
    // function getNodeConstructor(type) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get a node constructor by type.
    /// </summary>
    public NodeConstructor? GetNodeConstructor(string type)
    {
        lock (_lock)
        {
            if (!_nodeTypeToId.TryGetValue(type, out var id))
            {
                return _nodeConstructors.GetValueOrDefault(type);
            }

            var moduleName = GetModuleFromSetId(id);
            var nodeName = GetNodeFromSetId(id);

            if (_moduleConfigs.TryGetValue(moduleName, out var module) &&
                module.Nodes.TryGetValue(nodeName, out var config))
            {
                if (config.Enabled && config.Err is null)
                {
                    return _nodeConstructors.GetValueOrDefault(type);
                }
            }

            return null;
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 551-559):
    // ------------------------------------------------------------
    // function clear() { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Clear all registry data.
    /// </summary>
    public void Clear()
    {
        lock (_lock)
        {
            _nodeConfigCache.Clear();
            _moduleConfigs.Clear();
            _nodeList.Clear();
            _nodeConstructors.Clear();
            _nodeOptions.Clear();
            _subflowModules.Clear();
            _nodeTypeToId.Clear();
            _moduleNodes.Clear();
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 561-567):
    // ------------------------------------------------------------
    // function getTypeId(type) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get the ID for a type.
    /// </summary>
    public string? GetTypeId(string type)
    {
        lock (_lock)
        {
            return _nodeTypeToId.GetValueOrDefault(type);
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 488-514):
    // ------------------------------------------------------------
    // function getAllNodeConfigs(lang) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get all node configs as HTML string.
    /// </summary>
    public string GetAllNodeConfigs(string lang = "en-US")
    {
        lock (_lock)
        {
            if (_nodeConfigCache.TryGetValue(lang, out var cached))
            {
                return cached;
            }

            var result = "";

            foreach (var id in _nodeList)
            {
                var moduleName = GetModuleFromSetId(id);
                if (!_moduleConfigs.TryGetValue(moduleName, out var module))
                {
                    continue;
                }

                // Skip non-user modules used by others
                if (!module.User && module.UsedBy?.Count > 0)
                {
                    continue;
                }

                var nodeName = GetNodeFromSetId(id);
                if (!module.Nodes.TryGetValue(nodeName, out var config))
                {
                    continue;
                }

                if (config.Enabled && config.Err is null)
                {
                    result += $"\n<!-- --- [red-module:{id}] --- -->\n";
                    result += config.Config ?? "";
                    result += _loader?.GetNodeHelp(config, lang) ?? "";
                }
            }

            _nodeConfigCache[lang] = result;
            return result;
        }
    }
    // ============================================================

    /// <summary>
    /// Get full node info (internal use).
    /// </summary>
    private NodeSetConfig? GetFullNodeInfo(string typeOrId)
    {
        var id = typeOrId;
        if (_nodeTypeToId.TryGetValue(typeOrId, out var mappedId))
        {
            id = mappedId;
        }

        var moduleName = GetModuleFromSetId(id);
        if (_moduleConfigs.TryGetValue(moduleName, out var module))
        {
            var nodeName = GetNodeFromSetId(id);
            return module.Nodes.GetValueOrDefault(nodeName);
        }

        return null;
    }

    // ============================================================
    // ORIGINAL CODE (lines 675-701):
    // ------------------------------------------------------------
    // function getNodeIconPath(module,icon) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get the path to a node icon.
    /// </summary>
    public string? GetNodeIconPath(string module, string icon)
    {
        if (icon.Contains(".."))
        {
            throw new InvalidOperationException("Invalid icon path");
        }

        var iconName = $"{module}/{icon}";

        lock (_lock)
        {
            if (_iconCache.TryGetValue(iconName, out var cachedPath))
            {
                return cachedPath;
            }

            if (_iconPaths.TryGetValue(module, out var paths))
            {
                foreach (var p in paths)
                {
                    var iconPath = Path.Combine(p, icon);
                    if (File.Exists(iconPath))
                    {
                        _iconCache[iconName] = iconPath;
                        return iconPath;
                    }
                }
            }

            // Fallback to node-red icons
            if (module != "node-red")
            {
                return GetNodeIconPath("node-red", icon);
            }

            return null;
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 703-714):
    // ------------------------------------------------------------
    // function getNodeIcons() { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get all node icons.
    /// </summary>
    public Dictionary<string, List<string>> GetNodeIcons()
    {
        var iconList = new Dictionary<string, List<string>>();

        lock (_lock)
        {
            foreach (var moduleKvp in _moduleConfigs)
            {
                var module = moduleKvp.Value;
                if (module.Icons is not null)
                {
                    iconList[moduleKvp.Key] = new List<string>();
                    foreach (var icon in module.Icons)
                    {
                        iconList[moduleKvp.Key].AddRange(icon.Icons);
                    }
                }
            }
        }

        return iconList;
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 716-729):
    // ------------------------------------------------------------
    // function getModuleResource(module, resourcePath) { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Get a module resource path.
    /// </summary>
    public string? GetModuleResource(string module, string resourcePath)
    {
        lock (_lock)
        {
            if (!_moduleConfigs.TryGetValue(module, out var mod) || mod.Resources is null)
            {
                return null;
            }

            var basePath = mod.Resources.Path;
            var fullPath = Path.Combine(basePath, resourcePath);
            var relativePath = Path.GetRelativePath(basePath, fullPath);

            // Check for path traversal
            if (relativePath.StartsWith(".."))
            {
                return null;
            }

            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            return null;
        }
    }
    // ============================================================

    // ============================================================
    // ORIGINAL CODE (lines 95-145):
    // ------------------------------------------------------------
    // function saveNodeList() { ... }
    // ------------------------------------------------------------
    // TRANSLATION:
    // ------------------------------------------------------------
    /// <summary>
    /// Save the node list to settings.
    /// </summary>
    public async Task SaveNodeListAsync()
    {
        if (_settings?.Available() != true)
        {
            throw new InvalidOperationException("Settings unavailable");
        }

        var moduleList = new Dictionary<string, object>();
        var hadPending = false;
        var hasPending = false;

        lock (_lock)
        {
            foreach (var moduleKvp in _moduleConfigs)
            {
                var moduleName = moduleKvp.Key;
                var module = moduleKvp.Value;

                if (module.Nodes.Count == 0)
                {
                    continue;
                }

                var moduleData = new Dictionary<string, object?>
                {
                    ["name"] = moduleName,
                    ["version"] = module.Version,
                    ["local"] = module.Local,
                    ["user"] = module.User,
                    ["nodes"] = new Dictionary<string, object>()
                };

                if (!string.IsNullOrEmpty(module.PendingVersion))
                {
                    hadPending = true;
                    if (module.PendingVersion != module.Version)
                    {
                        moduleData["pending_version"] = module.PendingVersion;
                        hasPending = true;
                    }
                    else
                    {
                        module.PendingVersion = null;
                    }
                }

                var nodesData = (Dictionary<string, object>)moduleData["nodes"]!;
                foreach (var nodeKvp in module.Nodes)
                {
                    var nodeConfig = nodeKvp.Value;
                    var nodeInfo = FilterNodeInfo(nodeConfig);
                    nodesData[nodeKvp.Key] = new Dictionary<string, object?>
                    {
                        ["name"] = nodeInfo.Name,
                        ["types"] = nodeInfo.Types,
                        ["enabled"] = nodeInfo.Enabled,
                        ["local"] = nodeInfo.Local,
                        ["user"] = nodeInfo.User,
                        ["file"] = nodeConfig.File
                    };
                }

                moduleList[moduleName] = moduleData;
            }
        }

        if (hadPending && !hasPending)
        {
            _events.Emit("runtime-event", new NodeRedEventArgs(new { id = "restart-required", retain = true }));
        }

        await _settings.SetAsync("nodes", moduleList);
    }
    // ============================================================
}
