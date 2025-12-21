using Microsoft.EntityFrameworkCore;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Message;
using UniKnowledge.Models;

namespace UniKnowledge.Services;

public interface IMessageService
{
    Task<MessageResponseDto> CreateMessageAsync(int senderId, int receiverId, string content);
    Task<List<MessageResponseDto>> GetConversationAsync(int userId, int otherUserId, int page = 1, int pageSize = 50);
    Task<MessageResponseDto?> MarkAsReadAsync(int messageId, int userId);
    Task<int> GetUnreadCountAsync(int userId);
    Task<List<ConversationDto>> GetConversationsAsync(int userId);
}

public class MessageService : IMessageService
{
    private readonly AppDbContext _context;

    public MessageService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<MessageResponseDto> CreateMessageAsync(int senderId, int receiverId, string content)
    {
        // Validate users exist
        var sender = await _context.Users.FindAsync(senderId);
        var receiver = await _context.Users.FindAsync(receiverId);

        if (sender == null || receiver == null)
        {
            throw new Exception("User not found");
        }

        var message = new Message
        {
            SenderId = senderId,
            ReceiverId = receiverId,
            Content = content.Trim(),
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        return MapToDto(message, sender, receiver);
    }

    public async Task<List<MessageResponseDto>> GetConversationAsync(int userId, int otherUserId, int page = 1, int pageSize = 50)
    {
        var messages = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .Where(m => (m.SenderId == userId && m.ReceiverId == otherUserId) ||
                       (m.SenderId == otherUserId && m.ReceiverId == userId))
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync();

        return messages.Select(m => MapToDto(m, m.Sender, m.Receiver)).ToList();
    }

    public async Task<MessageResponseDto?> MarkAsReadAsync(int messageId, int userId)
    {
        var message = await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Receiver)
            .FirstOrDefaultAsync(m => m.MessageId == messageId && m.ReceiverId == userId);

        if (message == null)
        {
            return null;
        }

        if (!message.IsRead)
        {
            message.IsRead = true;
            await _context.SaveChangesAsync();
        }

        return MapToDto(message, message.Sender, message.Receiver);
    }

    public async Task<int> GetUnreadCountAsync(int userId)
    {
        return await _context.Messages
            .CountAsync(m => m.ReceiverId == userId && !m.IsRead);
    }

    public async Task<List<ConversationDto>> GetConversationsAsync(int userId)
    {
        // Get all unique users that the current user has conversations with
        var conversationPartners = await _context.Messages
            .Where(m => m.SenderId == userId || m.ReceiverId == userId)
            .Select(m => m.SenderId == userId ? m.ReceiverId : m.SenderId)
            .Distinct()
            .ToListAsync();

        var conversations = new List<ConversationDto>();

        foreach (var partnerId in conversationPartners)
        {
            var partner = await _context.Users.FindAsync(partnerId);
            if (partner == null) continue;

            // Get last message
            var lastMessage = await _context.Messages
                .Where(m => (m.SenderId == userId && m.ReceiverId == partnerId) ||
                           (m.SenderId == partnerId && m.ReceiverId == userId))
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            // Get unread count
            var unreadCount = await _context.Messages
                .CountAsync(m => m.SenderId == partnerId && m.ReceiverId == userId && !m.IsRead);

            conversations.Add(new ConversationDto
            {
                OtherUserId = partnerId,
                OtherUsername = partner.Username,
                OtherAvatarUrl = partner.AvatarUrl,
                LastMessage = lastMessage?.Content,
                LastMessageTime = lastMessage?.CreatedAt,
                UnreadCount = unreadCount,
                IsLastMessageFromMe = lastMessage?.SenderId == userId
            });
        }

        // Sort by last message time (most recent first)
        return conversations
            .OrderByDescending(c => c.LastMessageTime ?? DateTime.MinValue)
            .ToList();
    }

    private MessageResponseDto MapToDto(Message message, User sender, User receiver)
    {
        return new MessageResponseDto
        {
            MessageId = message.MessageId,
            SenderId = message.SenderId,
            SenderUsername = sender.Username,
            SenderAvatarUrl = sender.AvatarUrl,
            ReceiverId = message.ReceiverId,
            ReceiverUsername = receiver.Username,
            Content = message.Content,
            IsRead = message.IsRead,
            CreatedAt = message.CreatedAt
        };
    }
}

