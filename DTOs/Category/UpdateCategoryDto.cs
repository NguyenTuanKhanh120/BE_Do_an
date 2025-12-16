using System.ComponentModel.DataAnnotations;

namespace UniKnowledge.DTOs.Category;

public class UpdateCategoryDto
{
    [StringLength(100, MinimumLength = 2)]
    public string? CategoryName { get; set; }

    [StringLength(255)]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? Slug { get; set; }
}
