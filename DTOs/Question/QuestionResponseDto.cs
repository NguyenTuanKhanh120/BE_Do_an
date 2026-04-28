namespace UniKnowledge.DTOs.Question;

public class QuestionResponseDto
{
    public int QuestionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int ViewCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? FileUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Author info
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }

    // Category info
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;

    // Tags
    public List<TagDto> Tags { get; set; } = new();

    // Stats
    public int AnswerCount { get; set; }
    public int VoteCount { get; set; }
    public bool HasAcceptedAnswer { get; set; }

    // Share: thông tin bài gốc (null nếu đây là bài thường)
    public int? OriginalQuestionId { get; set; }
    public OriginalQuestionSummaryDto? OriginalQuestion { get; set; }
}

public class TagDto
{
    public int TagId { get; set; }
    public string TagName { get; set; } = string.Empty;
}

/// <summary>
/// DTO chứa thông tin tóm tắt bài gốc — hiển thị trong khung trích dẫn
/// </summary>
public class OriginalQuestionSummaryDto
{
    public int QuestionId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string ContentPreview { get; set; } = string.Empty;
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

/// <summary>
/// DTO cho request chia sẻ — chỉ chứa lời bình của người share
/// </summary>
public class ShareQuestionDto
{
    public string? Content { get; set; }
}
