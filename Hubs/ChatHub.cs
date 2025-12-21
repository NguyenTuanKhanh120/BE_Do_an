using System.Linq;
using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using UniKnowledge.DTOs.Message;
using UniKnowledge.Services;

namespace UniKnowledge.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private static readonly ConcurrentDictionary<string, int> _userConnections = new();

    public ChatHub(IMessageService messageService)
    {
        _messageService = messageService;
    }

    public override async Task OnConnectedAsync()
    {
        try
        {
            var userId = GetUserId();
            if (userId.HasValue)
            {
                var groupName = $"user_{userId.Value}";
                await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
                _userConnections.TryAdd(Context.ConnectionId, userId.Value);
            }
        }
        finally
        {
            await base.OnConnectedAsync();
        }
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        try
        {
            _userConnections.TryRemove(Context.ConnectionId, out _);
            
            if (exception != null)
            {
                var exceptionMessage = exception.Message ?? string.Empty;
                var isExpectedError = exceptionMessage.Contains("Connection closed") || 
                                     exceptionMessage.Contains("The remote party closed") ||
                                     exceptionMessage.Contains("An existing connection was forcibly closed") ||
                                     exceptionMessage.Contains("Server returned an error on close");
                
                if (!isExpectedError)
                {
                    Console.WriteLine($"SignalR disconnection exception: {exception.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in OnDisconnectedAsync: {ex.Message}");
        }
        finally
        {
            await base.OnDisconnectedAsync(exception);
        }
    }

    public async Task SendMessage(int receiverId, string content)
    {
        var senderId = GetUserId();
        if (!senderId.HasValue)
        {
            await Clients.Caller.SendAsync("Error", "Unauthorized");
            return;
        }

        if (senderId.Value == receiverId)
        {
            await Clients.Caller.SendAsync("Error", "Cannot send message to yourself");
            return;
        }

        try
        {
            var messageDto = await _messageService.CreateMessageAsync(senderId.Value, receiverId, content);
            
            try
            {
                await Clients.Group($"user_{receiverId}").SendAsync("ReceiveMessage", messageDto);
            }
            catch
            {
                // Receiver may not be connected - message is saved in DB
            }
            
            try
            {
                await Clients.Caller.SendAsync("MessageSent", messageDto);
            }
            catch
            {
                // Sender connection may be lost
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in SendMessage: {ex.Message}");
            try
            {
                await Clients.Caller.SendAsync("Error", ex.Message);
            }
            catch { }
        }
    }

    public async Task MarkAsRead(int messageId)
    {
        var userId = GetUserId();
        if (!userId.HasValue)
        {
            try { await Clients.Caller.SendAsync("Error", "Unauthorized"); } catch { }
            return;
        }

        try
        {
            var message = await _messageService.MarkAsReadAsync(messageId, userId.Value);
            if (message != null)
            {
                try
                {
                    await Clients.Group($"user_{message.SenderId}").SendAsync("MessageRead", new
                    {
                        MessageId = message.MessageId,
                        ReadBy = userId.Value,
                        ReadAt = DateTime.UtcNow
                    });
                }
                catch { }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in MarkAsRead: {ex.Message}");
            try { await Clients.Caller.SendAsync("Error", ex.Message); } catch { }
        }
    }

    public async Task StartTyping(int receiverId)
    {
        var senderId = GetUserId();
        if (!senderId.HasValue) return;

        try
        {
            await Clients.Group($"user_{receiverId}").SendAsync("UserTyping", new
            {
                UserId = senderId.Value,
                Username = Context.User?.Identity?.Name
            });
        }
        catch { }
    }

    public async Task StopTyping(int receiverId)
    {
        var senderId = GetUserId();
        if (!senderId.HasValue) return;

        try
        {
            await Clients.Group($"user_{receiverId}").SendAsync("UserStoppedTyping", new
            {
                UserId = senderId.Value
            });
        }
        catch { }
    }

    private int? GetUserId()
    {
        if (Context.User?.Identity?.IsAuthenticated != true)
            return null;

        var userIdClaim = Context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId))
            return userId;

        return null;
    }
}

