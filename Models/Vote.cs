namespace UniKnowledge.Models;

public class Vote
{
    public int VoteId { get; set; }
    public int UserId { get; set; }
    public int? QuestionId { get; set; }
    public int? AnswerId { get; set; }
    public int VoteType { get; set; } = 0; // 1: Upvote, -1: Downvote
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public Question? Question { get; set; }
    public Answer? Answer { get; set; }
}

