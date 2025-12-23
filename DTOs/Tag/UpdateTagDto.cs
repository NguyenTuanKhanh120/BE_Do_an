using System.ComponentModel.DataAnnotations;

namespace UniKnowledge.DTOs.Tag;

public class UpdateTagDto
{
    [StringLength(50, MinimumLength = 2)]
    public string? TagName { get; set; }
    
    [StringLength(255)]
    public string? Description { get; set; }
}

