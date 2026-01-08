using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace MediAid.Hubs;

[Authorize(AuthenticationSchemes = "Cookies")]
public class ChatHub : Hub
{
    private readonly ILogger<ChatHub> _logger;
    private static readonly Dictionary<string, string> _connectedUsers = new();

    public ChatHub(ILogger<ChatHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Utilisateur";
        
        if (!string.IsNullOrEmpty(userId))
        {
            _connectedUsers[Context.ConnectionId] = userId;
            await Clients.All.SendAsync("UserConnected", userId, userName);
            _logger.LogInformation($"User {userId} connected to chat hub");
        }
        else
        {
            _logger.LogWarning($"User connected without authentication. ConnectionId: {Context.ConnectionId}");
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _connectedUsers.ContainsKey(Context.ConnectionId) 
            ? _connectedUsers[Context.ConnectionId] 
            : Context.UserIdentifier;
        var userName = Context.User?.FindFirst(ClaimTypes.Name)?.Value ?? "Utilisateur";
        
        if (_connectedUsers.ContainsKey(Context.ConnectionId))
        {
            _connectedUsers.Remove(Context.ConnectionId);
        }
        
        await Clients.All.SendAsync("UserDisconnected", userId, userName);
        _logger.LogInformation($"User {userId} disconnected from chat hub");
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinRequestGroup(string requestId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.UserIdentifier;
        await Groups.AddToGroupAsync(Context.ConnectionId, $"request_{requestId}");
        _logger.LogInformation($"User {userId} joined group request_{requestId}");
        
        // Notify others in the group
        await Clients.Group($"request_{requestId}").SendAsync("UserJoinedGroup", userId);
    }

    public async Task LeaveRequestGroup(string requestId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.UserIdentifier;
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"request_{requestId}");
        _logger.LogInformation($"User {userId} left group request_{requestId}");
    }

    public async Task SendMessageToGroup(string requestId, string senderId, string senderName, string content, string messageId, DateTime timestamp, object? attachments = null, string status = "sent")
    {
        await Clients.Group($"request_{requestId}").SendAsync("ReceiveMessage", 
            senderId, senderName, content, timestamp, messageId, attachments, status);
    }
    
    public async Task MarkMessageDelivered(string messageId, string requestId)
    {
        await Clients.Group($"request_{requestId}").SendAsync("MessageDelivered", messageId);
    }

    public async Task Typing(string requestId, string senderName)
    {
        await Clients.OthersInGroup($"request_{requestId}").SendAsync("UserTyping", senderName);
    }

    public async Task StopTyping(string requestId)
    {
        await Clients.OthersInGroup($"request_{requestId}").SendAsync("UserStoppedTyping");
    }

    public async Task MessageRead(string requestId, string messageId, string readerId)
    {
        await Clients.Group($"request_{requestId}").SendAsync("MessageRead", messageId, readerId);
    }
    
    public async Task GetOnlineUsers(string requestId)
    {
        var userId = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.UserIdentifier;
        // Return list of online users in this request group
        await Clients.Caller.SendAsync("OnlineUsers", new[] { userId });
    }
}
