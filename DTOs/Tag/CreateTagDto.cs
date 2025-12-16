using System.ComponentModel.DataAnnotations;

namespace UniKnowledge.DTOs.Tag;

public class CreateTagDto
{
    [Required(ErrorMessage = "Tag name is required")]
    [StringLength(50, MinimumLength = 2)]
    public string TagName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }
}

