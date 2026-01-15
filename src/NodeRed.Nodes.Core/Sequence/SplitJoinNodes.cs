// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/sequence/17-split.js
// ============================================================
// Split and Join nodes - break apart and reassemble messages.
// ============================================================

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Collections.Concurrent;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Sequence;

/// <summary>
/// Split node - splits a message into a sequence of messages.
/// Translated from: @node-red/nodes/core/sequence/17-split.js
/// </summary>
public class SplitNode : Node
{
    /// <summary>
    /// Split behavior: "str", "bin", "len", "array".
    /// </summary>
    [JsonPropertyName("splt")]
    public string Splt { get; set; } = "\\n";

    /// <summary>
    /// Split type.
    /// </summary>
    [JsonPropertyName("spltType")]
    public string SpltType { get; set; } = "str";

    /// <summary>
    /// Array split behavior: "len" (fixed size), "array" (one per element).
    /// </summary>
    [JsonPropertyName("arraySplt")]
    public int ArraySplt { get; set; } = 1;

    /// <summary>
    /// Array split type.
    /// </summary>
    [JsonPropertyName("arraySpltType")]
    public string ArraySpltType { get; set; } = "len";

    /// <summary>
    /// Whether to add message parts info.
    /// </summary>
    [JsonPropertyName("addname")]
    public string? AddName { get; set; }

    public SplitNode()
    {
        Type = "split";
    }

    public override Task InitializeAsync()
    {
        OnInput(HandleInputAsync);
        return base.InitializeAsync();
    }

    private async Task HandleInputAsync(FlowMessage msg)
    {
        try
        {
            var payload = msg.Payload;
            
            if (payload is null)
            {
                await SendAsync(msg);
                return;
            }

            // Generate unique ID for this split operation
            var splitId = NodeRed.Util.Util.GenerateId();
            var parts = new List<object>();

            if (payload is string str)
            {
                parts = SplitString(str).Cast<object>().ToList();
            }
            else if (payload is byte[] bytes)
            {
                parts = SplitBuffer(bytes).Cast<object>().ToList();
            }
            else if (payload is object[] array)
            {
                parts = SplitArray(array);
            }
            else if (payload is IEnumerable<object> enumerable)
            {
                parts = SplitArray(enumerable.ToArray());
            }
            else if (payload is Dictionary<string, object> dict)
            {
                parts = dict.Select(kvp => (object)new Dictionary<string, object> 
                { 
                    ["key"] = kvp.Key, 
                    ["value"] = kvp.Value 
                }).ToList();
            }

            // Send each part
            for (int i = 0; i < parts.Count; i++)
            {
                var partMsg = NodeRed.Util.Util.CloneMessage(msg);
                partMsg.Payload = parts[i];
                
                // Add parts metadata
                partMsg.AdditionalProperties["parts"] = new
                {
                    id = splitId,
                    index = i,
                    count = parts.Count,
                    type = GetPartsType(payload)
                };

                await SendAsync(partMsg);
            }
        }
        catch (Exception ex)
        {
            Error(ex, msg);
        }
    }

    private List<string> SplitString(string str)
    {
        var separator = Splt.Replace("\\n", "\n").Replace("\\r", "\r").Replace("\\t", "\t");
        return str.Split(separator).ToList();
    }

    private List<byte[]> SplitBuffer(byte[] bytes)
    {
        if (SpltType == "len" && ArraySplt > 0)
        {
            var chunks = new List<byte[]>();
            for (int i = 0; i < bytes.Length; i += ArraySplt)
            {
                var length = Math.Min(ArraySplt, bytes.Length - i);
                var chunk = new byte[length];
                Array.Copy(bytes, i, chunk, 0, length);
                chunks.Add(chunk);
            }
            return chunks;
        }

        // Split by delimiter byte
        return new List<byte[]> { bytes };
    }

    private List<object> SplitArray(object[] array)
    {
        if (ArraySpltType == "len" && ArraySplt > 1)
        {
            var chunks = new List<object>();
            for (int i = 0; i < array.Length; i += ArraySplt)
            {
                var length = Math.Min(ArraySplt, array.Length - i);
                var chunk = new object[length];
                Array.Copy(array, i, chunk, 0, length);
                chunks.Add(chunk);
            }
            return chunks;
        }

        return array.ToList();
    }

    private static string GetPartsType(object payload)
    {
        if (payload is string) return "string";
        if (payload is byte[]) return "buffer";
        if (payload is Array) return "array";
        if (payload is Dictionary<string, object>) return "object";
        return "object";
    }
}

/// <summary>
/// Join node - joins a sequence of messages into one.
/// Translated from: @node-red/nodes/core/sequence/17-split.js
/// </summary>
public class JoinNode : Node
{
    private readonly ConcurrentDictionary<string, JoinGroup> _groups = new();

    /// <summary>
    /// Join mode: "auto", "manual", "reduce".
    /// </summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = "auto";

    /// <summary>
    /// Build type for manual mode: "string", "array", "buffer", "object".
    /// </summary>
    [JsonPropertyName("build")]
    public string Build { get; set; } = "array";

    /// <summary>
    /// Property to join.
    /// </summary>
    [JsonPropertyName("property")]
    public string Property { get; set; } = "payload";

    /// <summary>
    /// Property type.
    /// </summary>
    [JsonPropertyName("propertyType")]
    public string PropertyType { get; set; } = "msg";

    /// <summary>
    /// Key property for object build.
    /// </summary>
    [JsonPropertyName("key")]
    public string Key { get; set; } = "topic";

    /// <summary>
    /// Joiner string for string build.
    /// </summary>
    [JsonPropertyName("joiner")]
    public string Joiner { get; set; } = "\\n";

    /// <summary>
    /// Join type for joiner.
    /// </summary>
    [JsonPropertyName("joinerType")]
    public string JoinerType { get; set; } = "str";

    /// <summary>
    /// Number of messages to join.
    /// </summary>
    [JsonPropertyName("count")]
    public string Count { get; set; } = "";

    /// <summary>
    /// Timeout for collecting messages.
    /// </summary>
    [JsonPropertyName("timeout")]
    public string Timeout { get; set; } = "";

    public JoinNode()
    {
        Type = "join";
    }

    public override Task InitializeAsync()
    {
        OnInput(HandleInputAsync);
        return base.InitializeAsync();
    }

    private async Task HandleInputAsync(FlowMessage msg)
    {
        try
        {
            if (Mode == "auto")
            {
                await HandleAutoJoinAsync(msg);
            }
            else if (Mode == "manual")
            {
                await HandleManualJoinAsync(msg);
            }
            else if (Mode == "reduce")
            {
                await HandleReduceAsync(msg);
            }
        }
        catch (Exception ex)
        {
            Error(ex, msg);
        }
    }

    private async Task HandleAutoJoinAsync(FlowMessage msg)
    {
        // Auto mode uses msg.parts from split node
        if (!msg.AdditionalProperties.TryGetValue("parts", out var partsObj))
        {
            await SendAsync(msg);
            return;
        }

        var parts = partsObj as dynamic;
        if (parts is null)
        {
            await SendAsync(msg);
            return;
        }

        string groupId = parts.id?.ToString() ?? msg.MsgId;
        int count = (int)(parts.count ?? 1);
        int index = (int)(parts.index ?? 0);
        string type = parts.type?.ToString() ?? "array";

        var group = _groups.GetOrAdd(groupId, _ => new JoinGroup(count, type));
        group.Add(index, NodeRed.Util.Util.GetMessageProperty(msg, Property));

        if (group.IsComplete)
        {
            _groups.TryRemove(groupId, out _);
            
            var resultMsg = NodeRed.Util.Util.CloneMessage(msg);
            resultMsg.Payload = group.GetResult(Joiner.Replace("\\n", "\n"));
            
            // Remove parts info
            resultMsg.AdditionalProperties.Remove("parts");
            
            await SendAsync(resultMsg);
        }
    }

    private async Task HandleManualJoinAsync(FlowMessage msg)
    {
        // Manual mode uses topic or configurable key
        var keyValue = Key == "topic" ? msg.Topic : NodeRed.Util.Util.GetMessageProperty(msg, Key)?.ToString();
        var groupId = keyValue ?? "default";

        var targetCount = int.TryParse(Count, out var c) ? c : 0;
        var group = _groups.GetOrAdd(groupId, _ => new JoinGroup(targetCount, Build));
        
        group.Add(group.CurrentCount, NodeRed.Util.Util.GetMessageProperty(msg, Property));

        // Check if complete (by count or by msg.complete flag)
        var complete = msg.AdditionalProperties.ContainsKey("complete") || 
                      (targetCount > 0 && group.CurrentCount >= targetCount);

        if (complete)
        {
            _groups.TryRemove(groupId, out _);
            
            var resultMsg = NodeRed.Util.Util.CloneMessage(msg);
            resultMsg.Payload = group.GetResult(Joiner.Replace("\\n", "\n"));
            resultMsg.AdditionalProperties.Remove("complete");
            
            await SendAsync(resultMsg);
        }
    }

    private Task HandleReduceAsync(FlowMessage msg)
    {
        // Reduce mode would accumulate with a custom expression
        // This requires scripting support
        return SendAsync(msg);
    }
}

/// <summary>
/// Helper class to track joined messages.
/// </summary>
internal class JoinGroup
{
    private readonly Dictionary<int, object?> _parts = new();
    private readonly int _targetCount;
    private readonly string _type;

    public int CurrentCount => _parts.Count;
    public bool IsComplete => _targetCount > 0 && _parts.Count >= _targetCount;

    public JoinGroup(int targetCount, string type)
    {
        _targetCount = targetCount;
        _type = type;
    }

    public void Add(int index, object? value)
    {
        _parts[index] = value;
    }

    public object GetResult(string joiner)
    {
        var sorted = _parts.OrderBy(p => p.Key).Select(p => p.Value).ToList();

        return _type switch
        {
            "string" => string.Join(joiner, sorted.Select(v => v?.ToString() ?? "")),
            "buffer" => CombineBuffers(sorted),
            "object" => CreateObject(sorted),
            _ => sorted.ToArray()
        };
    }

    private static byte[] CombineBuffers(List<object?> parts)
    {
        var buffers = parts.Where(p => p is byte[]).Cast<byte[]>().ToList();
        var totalLength = buffers.Sum(b => b.Length);
        var result = new byte[totalLength];
        var offset = 0;
        foreach (var buffer in buffers)
        {
            Buffer.BlockCopy(buffer, 0, result, offset, buffer.Length);
            offset += buffer.Length;
        }
        return result;
    }

    private static Dictionary<string, object?> CreateObject(List<object?> parts)
    {
        var result = new Dictionary<string, object?>();
        foreach (var part in parts)
        {
            if (part is Dictionary<string, object> dict && 
                dict.TryGetValue("key", out var key) && 
                dict.TryGetValue("value", out var value))
            {
                result[key?.ToString() ?? ""] = value;
            }
        }
        return result;
    }
}
