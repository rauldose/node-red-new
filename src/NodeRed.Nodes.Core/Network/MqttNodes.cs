// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/network/10-mqtt.js
// ============================================================
// MQTT nodes - publish and subscribe to MQTT topics.
// ============================================================

using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Network;

/// <summary>
/// MQTT In node - subscribes to MQTT topics.
/// Translated from: @node-red/nodes/core/network/10-mqtt.js
/// Note: Actual MQTT connectivity would require a library like MQTTnet.
/// </summary>
public class MqttInNode : Node
{
    /// <summary>
    /// Topic to subscribe to.
    /// </summary>
    [JsonPropertyName("topic")]
    public string TopicPattern { get; set; } = "";

    /// <summary>
    /// QoS level: 0, 1, 2.
    /// </summary>
    [JsonPropertyName("qos")]
    public int Qos { get; set; }

    /// <summary>
    /// Payload output type: "auto", "utf8", "buffer", "base64", "json".
    /// </summary>
    [JsonPropertyName("datatype")]
    public string DataType { get; set; } = "auto";

    /// <summary>
    /// Broker configuration node ID.
    /// </summary>
    [JsonPropertyName("broker")]
    public string? Broker { get; set; }

    /// <summary>
    /// No Local flag (MQTT v5).
    /// </summary>
    [JsonPropertyName("nl")]
    public bool NoLocal { get; set; }

    /// <summary>
    /// Retain as Published flag (MQTT v5).
    /// </summary>
    [JsonPropertyName("rap")]
    public bool RetainAsPublished { get; set; }

    /// <summary>
    /// Retain handling option (MQTT v5).
    /// </summary>
    [JsonPropertyName("rh")]
    public int RetainHandling { get; set; }

    /// <summary>
    /// Reference to the broker node.
    /// </summary>
    [JsonIgnore]
    public MqttBrokerNode? BrokerNode { get; set; }

    public MqttInNode()
    {
        Type = "mqtt in";
    }

    public override Task InitializeAsync()
    {
        // Would register with broker for subscription
        return base.InitializeAsync();
    }

    /// <summary>
    /// Called when a message is received on the subscribed topic.
    /// </summary>
    public async Task OnMessageAsync(string topic, byte[] payload, bool retained, int qos)
    {
        var msg = new FlowMessage
        {
            MsgId = NodeRed.Util.Util.GenerateId(),
            Topic = topic
        };

        // Set payload based on data type
        msg.Payload = DataType switch
        {
            "utf8" => System.Text.Encoding.UTF8.GetString(payload),
            "buffer" => payload,
            "base64" => Convert.ToBase64String(payload),
            "json" => System.Text.Json.JsonSerializer.Deserialize<object>(
                System.Text.Encoding.UTF8.GetString(payload)),
            "auto" => TryParsePayload(payload),
            _ => payload
        };

        msg.AdditionalProperties["qos"] = qos;
        msg.AdditionalProperties["retain"] = retained;

        await SendAsync(msg);
    }

    private static object TryParsePayload(byte[] payload)
    {
        var str = System.Text.Encoding.UTF8.GetString(payload);
        
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<object>(str) ?? str;
        }
        catch
        {
            return str;
        }
    }
}

/// <summary>
/// MQTT Out node - publishes to MQTT topics.
/// Translated from: @node-red/nodes/core/network/10-mqtt.js
/// </summary>
public class MqttOutNode : Node
{
    /// <summary>
    /// Topic to publish to.
    /// </summary>
    [JsonPropertyName("topic")]
    public string TopicPattern { get; set; } = "";

    /// <summary>
    /// QoS level: 0, 1, 2.
    /// </summary>
    [JsonPropertyName("qos")]
    public int Qos { get; set; }

    /// <summary>
    /// Whether to retain message.
    /// </summary>
    [JsonPropertyName("retain")]
    public bool Retain { get; set; }

    /// <summary>
    /// Broker configuration node ID.
    /// </summary>
    [JsonPropertyName("broker")]
    public string? Broker { get; set; }

    /// <summary>
    /// Reference to the broker node.
    /// </summary>
    [JsonIgnore]
    public MqttBrokerNode? BrokerNode { get; set; }

    public MqttOutNode()
    {
        Type = "mqtt out";
    }

    public override Task InitializeAsync()
    {
        OnInput(HandleInputAsync);
        return base.InitializeAsync();
    }

    private async Task HandleInputAsync(FlowMessage msg)
    {
        if (BrokerNode is null)
        {
            Warn("No MQTT broker configured");
            return;
        }

        var topic = !string.IsNullOrEmpty(TopicPattern) ? TopicPattern : msg.Topic;
        
        if (string.IsNullOrEmpty(topic))
        {
            Warn("No topic specified");
            return;
        }

        // Get QoS from message or config
        var qos = msg.AdditionalProperties.TryGetValue("qos", out var msgQos) && msgQos is int q
            ? q
            : Qos;

        // Get retain from message or config
        var retain = msg.AdditionalProperties.TryGetValue("retain", out var msgRetain) && msgRetain is bool r
            ? r
            : Retain;

        // Convert payload to bytes
        byte[] payload;
        if (msg.Payload is byte[] bytes)
        {
            payload = bytes;
        }
        else if (msg.Payload is string str)
        {
            payload = System.Text.Encoding.UTF8.GetBytes(str);
        }
        else
        {
            var json = System.Text.Json.JsonSerializer.Serialize(msg.Payload);
            payload = System.Text.Encoding.UTF8.GetBytes(json);
        }

        await BrokerNode.PublishAsync(topic, payload, qos, retain);
    }
}

/// <summary>
/// MQTT Broker configuration node.
/// Translated from: @node-red/nodes/core/network/10-mqtt.js
/// </summary>
public class MqttBrokerNode : Node
{
    /// <summary>
    /// Broker hostname.
    /// </summary>
    [JsonPropertyName("broker")]
    public string BrokerHost { get; set; } = "localhost";

    /// <summary>
    /// Broker port.
    /// </summary>
    [JsonPropertyName("port")]
    public int Port { get; set; } = 1883;

    /// <summary>
    /// Client ID.
    /// </summary>
    [JsonPropertyName("clientid")]
    public string? ClientId { get; set; }

    /// <summary>
    /// Username for authentication.
    /// </summary>
    [JsonPropertyName("usetls")]
    public bool UseTls { get; set; }

    /// <summary>
    /// Keep alive interval.
    /// </summary>
    [JsonPropertyName("keepalive")]
    public int KeepAlive { get; set; } = 60;

    /// <summary>
    /// Clean session flag.
    /// </summary>
    [JsonPropertyName("cleansession")]
    public bool CleanSession { get; set; } = true;

    /// <summary>
    /// MQTT protocol version: 4 (3.1.1) or 5.
    /// </summary>
    [JsonPropertyName("protocolVersion")]
    public int ProtocolVersion { get; set; } = 4;

    public MqttBrokerNode()
    {
        Type = "mqtt-broker";
    }

    /// <summary>
    /// Publish a message to the broker.
    /// </summary>
    public Task PublishAsync(string topic, byte[] payload, int qos, bool retain)
    {
        // Would use MQTTnet or similar to publish
        Debug($"MQTT Publish: {topic} [{payload.Length} bytes]");
        return Task.CompletedTask;
    }

    /// <summary>
    /// Subscribe to a topic.
    /// </summary>
    public Task SubscribeAsync(string topic, int qos, Action<string, byte[], bool, int> callback)
    {
        // Would use MQTTnet or similar to subscribe
        Debug($"MQTT Subscribe: {topic} QoS={qos}");
        return Task.CompletedTask;
    }
}
