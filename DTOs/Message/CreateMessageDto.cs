using System.ComponentModel.DataAnnotations;

namespace UniKnowledge.DTOs.Message;

public class CreateMessageDto
{
    [Required]
    public int ReceiverId { get; set; }

    [Required]
    [MinLength(1)]
    public string Content { get; set; } = string.Empty;
}

