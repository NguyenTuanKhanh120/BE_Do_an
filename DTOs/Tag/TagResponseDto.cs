namespace UniKnowledge.DTOs.Tag;

public class TagResponseDto
{
    public int TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int QuestionCount { get; set; }
}

