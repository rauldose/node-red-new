// ============================================================
// SOURCE: Flow message structure from Node-RED runtime
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

using System.Text.Json.Serialization;

namespace NodeRed.Util;

/// <summary>
/// Represents a message that flows between nodes in a flow.
/// This structure matches the Node-RED message object format.
/// </summary>
public class FlowMessage
{
    /// <summary>
    /// The message ID.
    /// </summary>
    [JsonPropertyName("_msgid")]
    public string MsgId { get; set; } = Util.GenerateId();

    /// <summary>
    /// The message payload.
    /// </summary>
    [JsonPropertyName("payload")]
    public object? Payload { get; set; }

    /// <summary>
    /// The message topic.
    /// </summary>
    [JsonPropertyName("topic")]
    public string? Topic { get; set; }

    /// <summary>
    /// HTTP request object (not serialized, passed by reference).
    /// Used by HTTP In node - must not be cloned.
    /// </summary>
    [JsonIgnore]
    public object? Req { get; set; }

    /// <summary>
    /// HTTP response object (not serialized, passed by reference).
    /// Used by HTTP In node - must not be cloned.
    /// </summary>
    [JsonIgnore]
    public object? Res { get; set; }

    /// <summary>
    /// Additional properties stored dynamically.
    /// </summary>
    [JsonExtensionData]
    public Dictionary<string, object?>? AdditionalProperties { get; set; }

    /// <summary>
    /// Set a property value.
    /// </summary>
    public void Set(string key, object? value)
    {
        AdditionalProperties ??= new Dictionary<string, object?>();
        AdditionalProperties[key] = value;
    }

    /// <summary>
    /// Get a property value.
    /// </summary>
    public T? Get<T>(string key)
    {
        if (AdditionalProperties is not null && AdditionalProperties.TryGetValue(key, out var value))
        {
            if (value is T typedValue)
            {
                return typedValue;
            }
        }
        return default;
    }

    /// <summary>
    /// Clone the message (preserving Req/Res references).
    /// </summary>
    public FlowMessage Clone()
    {
        var clone = new FlowMessage
        {
            MsgId = Util.GenerateId(), // New ID for cloned message
            Payload = Payload,
            Topic = Topic,
            Req = Req, // Preserve reference
            Res = Res  // Preserve reference
        };

        if (AdditionalProperties is not null)
        {
            clone.AdditionalProperties = new Dictionary<string, object?>(AdditionalProperties);
        }

        return clone;
    }
}
