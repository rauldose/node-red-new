// ============================================================
// NodeRed.Nodes.Core - Core Node Types Registry
// ============================================================
// Registers all built-in node types for the runtime.
// ============================================================

using NodeRed.Nodes.Core.Common;
using NodeRed.Nodes.Core.Function;
using NodeRed.Nodes.Core.Network;
using NodeRed.Nodes.Core.Parsers;
using NodeRed.Nodes.Core.Sequence;
using NodeRed.Nodes.Core.Storage;

namespace NodeRed.Nodes.Core;

/// <summary>
/// Registry of all core node types.
/// Used by the runtime to instantiate nodes based on their type.
/// </summary>
public static class CoreNodeTypes
{
    /// <summary>
    /// Dictionary mapping node type names to their factory functions.
    /// </summary>
    public static readonly Dictionary<string, Func<Node>> NodeFactories = new()
    {
        // Common nodes
        ["inject"] = () => new InjectNode(),
        ["debug"] = () => new DebugNode(),
        ["complete"] = () => new CompleteNode(),
        ["catch"] = () => new CatchNode(),
        ["status"] = () => new StatusNode(),
        ["link in"] = () => new LinkInNode(),
        ["link out"] = () => new LinkOutNode(),
        ["link call"] = () => new LinkCallNode(),
        ["comment"] = () => new CommentNode(),

        // Function nodes
        ["function"] = () => new FunctionNode(),
        ["switch"] = () => new SwitchNode(),
        ["change"] = () => new ChangeNode(),
        ["range"] = () => new RangeNode(),
        ["template"] = () => new TemplateNode(),
        ["delay"] = () => new DelayNode(),
        ["trigger"] = () => new TriggerNode(),
        ["exec"] = () => new ExecNode(),

        // Network nodes
        ["http request"] = () => new HttpRequestNode(),
        ["http in"] = () => new HttpInNode(),
        ["http response"] = () => new HttpResponseNode(),
        ["mqtt in"] = () => new MqttInNode(),
        ["mqtt out"] = () => new MqttOutNode(),
        ["mqtt-broker"] = () => new MqttBrokerNode(),

        // Parser nodes
        ["json"] = () => new JsonNode(),
        ["csv"] = () => new CsvNode(),

        // Storage nodes
        ["file"] = () => new FileNode(),
        ["file in"] = () => new FileInNode(),
        ["watch"] = () => new WatchNode(),

        // Sequence nodes
        ["split"] = () => new SplitNode(),
        ["join"] = () => new JoinNode(),
    };

    /// <summary>
    /// Create a node instance by type name.
    /// </summary>
    public static Node? CreateNode(string type)
    {
        if (NodeFactories.TryGetValue(type, out var factory))
        {
            return factory();
        }
        return null;
    }

    /// <summary>
    /// Get all registered node types.
    /// </summary>
    public static IEnumerable<string> GetNodeTypes()
    {
        return NodeFactories.Keys;
    }

    /// <summary>
    /// Check if a node type is registered.
    /// </summary>
    public static bool IsNodeTypeRegistered(string type)
    {
        return NodeFactories.ContainsKey(type);
    }

    /// <summary>
    /// Get node category for palette organization.
    /// </summary>
    public static string GetNodeCategory(string type)
    {
        return type switch
        {
            "inject" or "debug" or "complete" or "catch" or "status" or 
            "link in" or "link out" or "link call" or "comment" => "common",
            
            "function" or "switch" or "change" or "range" or "template" or 
            "delay" or "trigger" or "exec" => "function",
            
            "http request" or "http in" or "http response" or 
            "mqtt in" or "mqtt out" or "mqtt-broker" => "network",
            
            "json" or "csv" => "parser",
            
            "file" or "file in" or "watch" => "storage",
            
            "split" or "join" => "sequence",
            
            _ => "unknown"
        };
    }

    /// <summary>
    /// Get node color for the editor palette.
    /// </summary>
    public static string GetNodeColor(string type)
    {
        return type switch
        {
            "inject" => "#a6bbcf",
            "debug" => "#87a980",
            "complete" or "catch" or "status" => "#a6bbcf",
            "link in" or "link out" or "link call" => "#ddd",
            "comment" => "#fff",
            
            "function" or "exec" => "#fdd0a2",
            "switch" or "change" or "range" => "#e2d96e",
            "template" => "#e2bc9b",
            "delay" or "trigger" => "#e6b9a1",
            
            "http request" or "http in" or "http response" => "#c0edc0",
            "mqtt in" or "mqtt out" => "#d8bfd8",
            
            "json" or "csv" => "#eeeeee",
            
            "file" or "file in" or "watch" => "#e6e0f8",
            
            "split" or "join" => "#e2d96e",
            
            _ => "#ddd"
        };
    }

    /// <summary>
    /// Get number of inputs for a node type.
    /// </summary>
    public static int GetNodeInputs(string type)
    {
        return type switch
        {
            "inject" or "complete" or "catch" or "status" or 
            "link in" or "mqtt in" or "http in" or "watch" => 0,
            _ => 1
        };
    }

    /// <summary>
    /// Get number of outputs for a node type.
    /// </summary>
    public static int GetNodeOutputs(string type)
    {
        return type switch
        {
            "debug" or "http response" or "mqtt out" or 
            "link out" or "comment" => 0,
            "exec" => 3, // stdout, stderr, return code
            _ => 1
        };
    }
}
