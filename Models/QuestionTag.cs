namespace UniKnowledge.Models;

public class QuestionTag
{
    public int QuestionId { get; set; }
    public int TagId { get; set; }

    // Navigation properties
    public Question Question { get; set; } = null!;
    public Tag Tag { get; set; } = null!;
}

