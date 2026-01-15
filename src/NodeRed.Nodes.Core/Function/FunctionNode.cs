// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/function/10-function.js
// ============================================================
// Function node - executes custom code.
// ============================================================

using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Function;

/// <summary>
/// Function node - executes custom code.
/// Translated from: @node-red/nodes/core/function/10-function.js
/// Note: In Node-RED, this executes JavaScript. In .NET, this would need
/// a scripting engine like Roslyn or a custom DSL.
/// </summary>
public class FunctionNode : Node
{
    // ============================================================
    // ORIGINAL CODE (10-function.js lines 15-30):
    // ------------------------------------------------------------
    // this.name = n.name;
    // this.func = n.func;
    // this.outputs = n.outputs;
    // this.noerr = n.noerr || 0;
    // this.initialize = n.initialize || "";
    // this.finalize = n.finalize || "";
    // this.libs = n.libs || [];
    // ------------------------------------------------------------

    /// <summary>
    /// The function code to execute.
    /// </summary>
    [JsonPropertyName("func")]
    public string Func { get; set; } = "return msg;";

    /// <summary>
    /// Number of outputs.
    /// </summary>
    [JsonPropertyName("outputs")]
    public int Outputs { get; set; } = 1;

    /// <summary>
    /// Error suppression level.
    /// </summary>
    [JsonPropertyName("noerr")]
    public int NoErr { get; set; }

    /// <summary>
    /// Initialization code (runs on startup).
    /// </summary>
    [JsonPropertyName("initialize")]
    public string? Initialize { get; set; }

    /// <summary>
    /// Finalization code (runs on close).
    /// </summary>
    [JsonPropertyName("finalize")]
    public string? Finalize { get; set; }

    /// <summary>
    /// External libraries to load.
    /// </summary>
    [JsonPropertyName("libs")]
    public List<FunctionLib>? Libs { get; set; }

    /// <summary>
    /// Compiled function delegate (would be set by scripting engine).
    /// </summary>
    [JsonIgnore]
    public Func<FlowMessage, FunctionContext, Task<FlowMessage?[]?>>? CompiledFunction { get; set; }

    public FunctionNode()
    {
        Type = "function";
    }

    public override Task InitializeAsync()
    {
        OnInput(HandleInputAsync);
        
        // In a full implementation, would compile the function code here
        // using Roslyn or similar scripting engine
        
        return base.InitializeAsync();
    }

    private async Task HandleInputAsync(FlowMessage msg)
    {
        try
        {
            var context = new FunctionContext(this);

            if (CompiledFunction is not null)
            {
                var results = await CompiledFunction(msg, context);
                
                if (results is not null)
                {
                    await SendAsync(results);
                }
            }
            else
            {
                // No compiled function - just pass through
                await SendAsync(msg);
            }
        }
        catch (Exception ex)
        {
            if (NoErr == 0)
            {
                Error(ex, msg);
            }
        }
    }
}

/// <summary>
/// External library reference for function node.
/// </summary>
public class FunctionLib
{
    [JsonPropertyName("var")]
    public string? Var { get; set; }

    [JsonPropertyName("module")]
    public string? Module { get; set; }
}

/// <summary>
/// Context object passed to function execution.
/// Provides access to node, flow, and global contexts.
/// </summary>
public class FunctionContext
{
    private readonly FunctionNode _node;
    private readonly Dictionary<string, object?> _flowContext = new();
    private readonly Dictionary<string, object?> _globalContext = new();

    public FunctionContext(FunctionNode node)
    {
        _node = node;
    }

    /// <summary>
    /// Get a value from flow context.
    /// </summary>
    public object? Get(string key)
    {
        _flowContext.TryGetValue(key, out var value);
        return value;
    }

    /// <summary>
    /// Set a value in flow context.
    /// </summary>
    public void Set(string key, object? value)
    {
        _flowContext[key] = value;
    }

    /// <summary>
    /// Get global context.
    /// </summary>
    public FunctionGlobalContext Global => new(_globalContext);

    /// <summary>
    /// Log a message.
    /// </summary>
    public void Log(string message)
    {
        _node.LogMessage(message);
    }

    /// <summary>
    /// Log a warning.
    /// </summary>
    public void Warn(string message)
    {
        _node.Warn(message);
    }

    /// <summary>
    /// Log an error.
    /// </summary>
    public void Error(string message, FlowMessage? msg = null)
    {
        _node.Error(message, msg);
    }

    /// <summary>
    /// Set node status.
    /// </summary>
    public void Status(NodeStatus status)
    {
        _node.SetStatus(status);
    }
}

/// <summary>
/// Global context accessor for function node.
/// </summary>
public class FunctionGlobalContext
{
    private readonly Dictionary<string, object?> _context;

    public FunctionGlobalContext(Dictionary<string, object?> context)
    {
        _context = context;
    }

    public object? Get(string key)
    {
        _context.TryGetValue(key, out var value);
        return value;
    }

    public void Set(string key, object? value)
    {
        _context[key] = value;
    }
}
