namespace UniKnowledge.DTOs.Tag;

public class TagFilterDto
{
    public List<int>? TagIds { get; set; } = new();
    public string Logic { get; set; } = "AND"; // AND hoặc OR
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

