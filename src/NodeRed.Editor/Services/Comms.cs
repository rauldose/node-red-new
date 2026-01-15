// ============================================================
// SOURCE: packages/node_modules/@node-red/editor-api/lib/comms.js
//         packages/node_modules/@node-red/editor-client/src/js/comms.js
// ============================================================
// WebSocket communication for debug messages and status updates.
// ============================================================

using Microsoft.AspNetCore.SignalR;

namespace NodeRed.Editor.Services;

/// <summary>
/// SignalR Hub for real-time communication.
/// Replaces Node-RED's WebSocket-based comms.
/// </summary>
public class CommsHub : Hub
{
    /// <summary>
    /// Subscribe to debug messages.
    /// </summary>
    public async Task SubscribeDebug()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "debug");
    }

    /// <summary>
    /// Unsubscribe from debug messages.
    /// </summary>
    public async Task UnsubscribeDebug()
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, "debug");
    }

    /// <summary>
    /// Subscribe to status updates for specific nodes.
    /// </summary>
    public async Task SubscribeStatus(string[] nodeIds)
    {
        foreach (var nodeId in nodeIds)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"status:{nodeId}");
        }
    }

    /// <summary>
    /// Subscribe to notifications.
    /// </summary>
    public async Task SubscribeNotifications()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, "notifications");
    }

    public override async Task OnConnectedAsync()
    {
        // Auto-subscribe to general updates
        await Groups.AddToGroupAsync(Context.ConnectionId, "general");
        await base.OnConnectedAsync();
    }
}

/// <summary>
/// Service for sending real-time updates to clients.
/// </summary>
public interface ICommsService
{
    Task SendDebugMessageAsync(DebugPayload message);
    Task SendStatusUpdateAsync(string nodeId, StatusPayload status);
    Task SendNotificationAsync(NotificationPayload notification);
    Task SendFlowsDeployedAsync(string rev);
}

public class CommsService : ICommsService
{
    private readonly IHubContext<CommsHub> _hubContext;

    public CommsService(IHubContext<CommsHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendDebugMessageAsync(DebugPayload message)
    {
        await _hubContext.Clients.Group("debug").SendAsync("debug", message);
    }

    public async Task SendStatusUpdateAsync(string nodeId, StatusPayload status)
    {
        await _hubContext.Clients.Group($"status:{nodeId}").SendAsync("status", nodeId, status);
        await _hubContext.Clients.Group("general").SendAsync("status", nodeId, status);
    }

    public async Task SendNotificationAsync(NotificationPayload notification)
    {
        await _hubContext.Clients.Group("notifications").SendAsync("notification", notification);
        await _hubContext.Clients.Group("general").SendAsync("notification", notification);
    }

    public async Task SendFlowsDeployedAsync(string rev)
    {
        await _hubContext.Clients.All.SendAsync("deployed", new { rev });
    }
}

/// <summary>
/// Debug message payload.
/// </summary>
public class DebugPayload
{
    public string Id { get; set; } = "";
    public string? Name { get; set; }
    public string? Topic { get; set; }
    public string Msg { get; set; } = "";
    public object? Property { get; set; }
    public long Timestamp { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public string? Format { get; set; }
    public string? Path { get; set; }
    public string FlowId { get; set; } = "";
}

/// <summary>
/// Node status update payload.
/// </summary>
public class StatusPayload
{
    public string? Text { get; set; }
    public string? Fill { get; set; }  // "red", "green", "yellow", "blue", "grey"
    public string? Shape { get; set; } // "ring", "dot"
}

/// <summary>
/// Notification payload.
/// </summary>
public class NotificationPayload
{
    public string Type { get; set; } = "info";  // "info", "warning", "error", "success"
    public string? Text { get; set; }
    public int Timeout { get; set; } = 5000;
    public string? Id { get; set; }
    public bool Fixed { get; set; }
    public List<NotificationButton>? Buttons { get; set; }
}

/// <summary>
/// Button for a notification.
/// </summary>
public class NotificationButton
{
    public string Text { get; set; } = "";
    public string? Class { get; set; }
}

/// <summary>
/// Client-side service for receiving real-time updates.
/// Used in Blazor components.
/// </summary>
public class DebugMessageService : IAsyncDisposable
{
    private readonly List<DebugPayload> _messages = new();
    private const int MaxMessages = 1000;

    public event EventHandler<DebugPayload>? MessageReceived;
    public event EventHandler? MessagesCleared;

    public IReadOnlyList<DebugPayload> Messages => _messages;

    public void AddMessage(DebugPayload message)
    {
        _messages.Insert(0, message);

        // Limit message count
        while (_messages.Count > MaxMessages)
        {
            _messages.RemoveAt(_messages.Count - 1);
        }

        MessageReceived?.Invoke(this, message);
    }

    public void Clear()
    {
        _messages.Clear();
        MessagesCleared?.Invoke(this, EventArgs.Empty);
    }

    public void ClearNode(string nodeId)
    {
        _messages.RemoveAll(m => m.Id == nodeId);
        MessagesCleared?.Invoke(this, EventArgs.Empty);
    }

    public IEnumerable<DebugPayload> FilterByNode(string nodeId)
    {
        return _messages.Where(m => m.Id == nodeId);
    }

    public IEnumerable<DebugPayload> FilterByFlow(string flowId)
    {
        return _messages.Where(m => m.FlowId == flowId);
    }

    public ValueTask DisposeAsync()
    {
        _messages.Clear();
        return ValueTask.CompletedTask;
    }
}
