// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/network/21-httprequest.js
// ============================================================
// HTTP Request node - makes HTTP requests.
// ============================================================

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Network;

/// <summary>
/// HTTP Request node - makes HTTP requests.
/// Translated from: @node-red/nodes/core/network/21-httprequest.js
/// </summary>
public class HttpRequestNode : Node
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(2)
    };

    /// <summary>
    /// HTTP method: GET, POST, PUT, DELETE, etc.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = "GET";

    /// <summary>
    /// URL to request.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = "";

    /// <summary>
    /// Return type: "txt", "bin", "obj".
    /// </summary>
    [JsonPropertyName("ret")]
    public string Ret { get; set; } = "txt";

    /// <summary>
    /// Payload type for request body.
    /// </summary>
    [JsonPropertyName("paytoqs")]
    public string PayToQs { get; set; } = "ignore";

    /// <summary>
    /// TLS configuration node ID.
    /// </summary>
    [JsonPropertyName("tls")]
    public string? Tls { get; set; }

    /// <summary>
    /// Whether to persist cookies.
    /// </summary>
    [JsonPropertyName("persist")]
    public bool Persist { get; set; }

    /// <summary>
    /// Proxy configuration.
    /// </summary>
    [JsonPropertyName("proxy")]
    public string? Proxy { get; set; }

    /// <summary>
    /// Whether to use authentication from msg.
    /// </summary>
    [JsonPropertyName("authType")]
    public string? AuthType { get; set; }

    /// <summary>
    /// Whether to send credentials with request.
    /// </summary>
    [JsonPropertyName("senderr")]
    public bool SendErr { get; set; }

    public HttpRequestNode()
    {
        Type = "http request";
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
            // Build URL - can be from config or msg.url
            var url = !string.IsNullOrEmpty(Url) 
                ? Url 
                : msg.AdditionalProperties.GetValueOrDefault("url")?.ToString() ?? "";

            if (string.IsNullOrEmpty(url))
            {
                Error("No URL specified", msg);
                return;
            }

            // Process mustache-like substitutions in URL
            url = SubstituteVariables(url, msg);

            // Determine method
            var method = Method.ToUpperInvariant();
            if (method == "USE")
            {
                method = msg.AdditionalProperties.GetValueOrDefault("method")?.ToString()?.ToUpperInvariant() ?? "GET";
            }

            // Build request
            using var request = new HttpRequestMessage(new HttpMethod(method), url);

            // Add headers from message
            if (msg.AdditionalProperties.TryGetValue("headers", out var headersObj) && 
                headersObj is Dictionary<string, object> headers)
            {
                foreach (var header in headers)
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value?.ToString());
                }
            }

            // Add body for POST/PUT/PATCH
            if (method is "POST" or "PUT" or "PATCH" && msg.Payload is not null)
            {
                if (msg.Payload is byte[] bytes)
                {
                    request.Content = new ByteArrayContent(bytes);
                }
                else if (msg.Payload is string str)
                {
                    request.Content = new StringContent(str, Encoding.UTF8, "text/plain");
                }
                else
                {
                    var json = JsonSerializer.Serialize(msg.Payload);
                    request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                }
            }

            // Send request
            SetStatus(new NodeStatus { Fill = "blue", Shape = "dot", Text = "requesting" });

            var response = await _httpClient.SendAsync(request);

            // Build response message
            var responseMsg = NodeRed.Util.Util.CloneMessage(msg);

            // Set status code
            responseMsg.AdditionalProperties["statusCode"] = (int)response.StatusCode;

            // Set response headers
            var responseHeaders = new Dictionary<string, string>();
            foreach (var header in response.Headers)
            {
                responseHeaders[header.Key.ToLower()] = string.Join(", ", header.Value);
            }
            foreach (var header in response.Content.Headers)
            {
                responseHeaders[header.Key.ToLower()] = string.Join(", ", header.Value);
            }
            responseMsg.AdditionalProperties["headers"] = responseHeaders;

            // Set payload based on return type
            responseMsg.Payload = Ret switch
            {
                "bin" => await response.Content.ReadAsByteArrayAsync(),
                "obj" => await ParseJsonResponseAsync(response),
                _ => await response.Content.ReadAsStringAsync()
            };

            ClearStatus();

            // Handle errors
            if (!response.IsSuccessStatusCode && !SendErr)
            {
                Error($"HTTP {(int)response.StatusCode}: {response.ReasonPhrase}", responseMsg);
            }
            else
            {
                await SendAsync(responseMsg);
            }
        }
        catch (HttpRequestException ex)
        {
            SetStatus(new NodeStatus { Fill = "red", Shape = "ring", Text = "error" });
            Error(ex, msg);
        }
        catch (TaskCanceledException)
        {
            SetStatus(new NodeStatus { Fill = "red", Shape = "ring", Text = "timeout" });
            Error("Request timed out", msg);
        }
        catch (Exception ex)
        {
            SetStatus(new NodeStatus { Fill = "red", Shape = "ring", Text = "error" });
            Error(ex, msg);
        }
    }

    private static string SubstituteVariables(string url, FlowMessage msg)
    {
        // Simple mustache-like substitution
        url = url.Replace("{{{payload}}}", msg.Payload?.ToString() ?? "");
        url = url.Replace("{{payload}}", Uri.EscapeDataString(msg.Payload?.ToString() ?? ""));
        url = url.Replace("{{{topic}}}", msg.Topic ?? "");
        url = url.Replace("{{topic}}", Uri.EscapeDataString(msg.Topic ?? ""));
        
        return url;
    }

    private static async Task<object?> ParseJsonResponseAsync(HttpResponseMessage response)
    {
        var content = await response.Content.ReadAsStringAsync();
        try
        {
            return JsonSerializer.Deserialize<object>(content);
        }
        catch
        {
            return content;
        }
    }
}
