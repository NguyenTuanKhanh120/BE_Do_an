using System.ComponentModel.DataAnnotations;

namespace UniKnowledge.DTOs.Category;

public class CreateCategoryDto
{
    [Required(ErrorMessage = "Category name is required")]
    [StringLength(100, MinimumLength = 2)]
    public string CategoryName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? Description { get; set; }

    [Required(ErrorMessage = "Slug is required")]
    [StringLength(100)]
    public string Slug { get; set; } = string.Empty;
}
