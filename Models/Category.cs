namespace UniKnowledge.Models;

public class Category
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Slug { get; set; } = string.Empty;

    // Navigation properties
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}

