// ============================================================
// SOURCE: packages/node_modules/@node-red/nodes/core/network/21-httpin.js
// ============================================================
// HTTP In node - receives HTTP requests.
// ============================================================

using System.Text.Json.Serialization;
using NodeRed.Util;

namespace NodeRed.Nodes.Core.Network;

/// <summary>
/// HTTP In node - creates HTTP endpoints.
/// Translated from: @node-red/nodes/core/network/21-httpin.js
/// </summary>
public class HttpInNode : Node
{
    /// <summary>
    /// URL path for the endpoint.
    /// </summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = "/";

    /// <summary>
    /// HTTP method to accept.
    /// </summary>
    [JsonPropertyName("method")]
    public string Method { get; set; } = "get";

    /// <summary>
    /// Whether to upload files.
    /// </summary>
    [JsonPropertyName("upload")]
    public bool Upload { get; set; }

    /// <summary>
    /// Whether to use Swagger docs.
    /// </summary>
    [JsonPropertyName("swaggerDoc")]
    public object? SwaggerDoc { get; set; }

    public HttpInNode()
    {
        Type = "http in";
    }

    public override Task InitializeAsync()
    {
        // HTTP In nodes register routes with the HTTP server
        // This would be done by the runtime during startup
        return base.InitializeAsync();
    }

    /// <summary>
    /// Called by the HTTP server when a matching request is received.
    /// </summary>
    public async Task OnHttpRequestAsync(HttpRequest request)
    {
        var msg = new FlowMessage
        {
            MsgId = NodeRed.Util.Util.GenerateId(),
            Payload = request.Body,
            Topic = Url
        };

        // Add request properties
        msg.AdditionalProperties["req"] = new
        {
            method = request.Method,
            url = request.Url,
            headers = request.Headers,
            query = request.Query,
            body = request.Body
        };

        // Store reference for HTTP response node
        msg.Req = request;

        await SendAsync(msg);
    }
}

/// <summary>
/// HTTP Response node - sends HTTP responses.
/// Translated from: @node-red/nodes/core/network/21-httpin.js
/// </summary>
public class HttpResponseNode : Node
{
    /// <summary>
    /// Default status code.
    /// </summary>
    [JsonPropertyName("statusCode")]
    public int StatusCode { get; set; } = 200;

    /// <summary>
    /// Default headers.
    /// </summary>
    [JsonPropertyName("headers")]
    public Dictionary<string, string>? Headers { get; set; }

    public HttpResponseNode()
    {
        Type = "http response";
    }

    public override Task InitializeAsync()
    {
        OnInput(HandleInputAsync);
        return base.InitializeAsync();
    }

    private async Task HandleInputAsync(FlowMessage msg)
    {
        // Get the original request from the message
        if (msg.Req is not HttpRequest request || request.Response is null)
        {
            Warn("No HTTP request object to respond to");
            return;
        }

        var response = request.Response;

        // Set status code
        var statusCode = StatusCode;
        if (msg.AdditionalProperties.TryGetValue("statusCode", out var sc) && sc is int code)
        {
            statusCode = code;
        }
        response.StatusCode = statusCode;

        // Set headers
        if (Headers is not null)
        {
            foreach (var header in Headers)
            {
                response.Headers[header.Key] = header.Value;
            }
        }

        if (msg.AdditionalProperties.TryGetValue("headers", out var msgHeaders) && 
            msgHeaders is Dictionary<string, string> headerDict)
        {
            foreach (var header in headerDict)
            {
                response.Headers[header.Key] = header.Value;
            }
        }

        // Set body
        response.Body = msg.Payload;

        // Complete the response
        await response.SendAsync();
    }
}

/// <summary>
/// Represents an incoming HTTP request.
/// </summary>
public class HttpRequest
{
    public string Method { get; set; } = "GET";
    public string Url { get; set; } = "/";
    public Dictionary<string, string> Headers { get; set; } = new();
    public Dictionary<string, string> Query { get; set; } = new();
    public object? Body { get; set; }
    public HttpResponse? Response { get; set; }
}

/// <summary>
/// Represents an outgoing HTTP response.
/// </summary>
public class HttpResponse
{
    public int StatusCode { get; set; } = 200;
    public Dictionary<string, string> Headers { get; set; } = new();
    public object? Body { get; set; }
    
    private TaskCompletionSource<bool>? _tcs;

    public void SetCompletionSource(TaskCompletionSource<bool> tcs)
    {
        _tcs = tcs;
    }

    public Task SendAsync()
    {
        _tcs?.TrySetResult(true);
        return Task.CompletedTask;
    }
}
