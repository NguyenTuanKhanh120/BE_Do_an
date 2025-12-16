using System.ComponentModel.DataAnnotations;

namespace UniKnowledge.DTOs.Answer;

public class CreateAnswerDto
{
    [Required]
    [MinLength(20)]
    public string Content { get; set; } = string.Empty;
}

