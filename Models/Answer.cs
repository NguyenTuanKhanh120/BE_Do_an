namespace UniKnowledge.Models;

public class Answer
{
    public int AnswerId { get; set; }
    public int QuestionId { get; set; }
    public int UserId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsAccepted { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public Question Question { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<Vote> Votes { get; set; } = new List<Vote>();
}

