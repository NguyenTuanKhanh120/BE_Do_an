namespace UniKnowledge.DTOs.Message;

public class MessageResponseDto
{
    public int MessageId { get; set; }
    public int SenderId { get; set; }
    public string SenderUsername { get; set; } = string.Empty;
    public string? SenderAvatarUrl { get; set; }
    public int ReceiverId { get; set; }
    public string ReceiverUsername { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

