namespace UniKnowledge.DTOs.Message;

public class ConversationDto
{
    public int OtherUserId { get; set; }
    public string OtherUsername { get; set; } = string.Empty;
    public string? OtherAvatarUrl { get; set; }
    public string? LastMessage { get; set; }
    public DateTime? LastMessageTime { get; set; }
    public int UnreadCount { get; set; }
    public bool IsLastMessageFromMe { get; set; }
}

