namespace UniKnowledge.DTOs.Answer;

public class AnswerResponseDto
{
    public int AnswerId { get; set; }
    public int QuestionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsAccepted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Author info
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    // Stats
    public int VoteCount { get; set; }
}

