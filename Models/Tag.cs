namespace UniKnowledge.Models;

public class Tag
{
    public int TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation properties
    public ICollection<QuestionTag> QuestionTags { get; set; } = new List<QuestionTag>();
}

