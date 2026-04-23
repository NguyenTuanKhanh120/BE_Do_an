namespace UniKnowledge.DTOs.User;

public class UserProfileDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int QuestionCount { get; set; }
    public int AnswerCount { get; set; }
}

public class UpdateProfileDto
{
    public string? Username { get; set; }
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
}

public class ChangePasswordDto
{
    public string CurrentPassword { get; set; } = string.Empty;
    public string NewPassword { get; set; } = string.Empty;
}

/// <summary>
/// DTO for public profile view — includes follow info
/// </summary>
public class PublicProfileDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int QuestionCount { get; set; }
    public int AnswerCount { get; set; }
    public int FollowerCount { get; set; }
    public int FollowingCount { get; set; }
    public bool IsFollowing { get; set; }  // Whether current user follows this user
}

/// <summary>
/// Lightweight DTO for search results — only essential display info
/// </summary>
public class UserSearchDto
{
    public int UserId { get; set; }
    public string? FullName { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}