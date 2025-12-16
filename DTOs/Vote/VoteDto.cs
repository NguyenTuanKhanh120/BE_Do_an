using System.ComponentModel.DataAnnotations;

namespace UniKnowledge.DTOs.Vote;

public class VoteDto
{
    [Required]
    [Range(-1, 1, ErrorMessage = "VoteType must be 1 (upvote) or -1 (downvote)")]
    public int VoteType { get; set; } // 1: Upvote, -1: Downvote
}

