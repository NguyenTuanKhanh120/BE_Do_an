namespace UniKnowledge.DTOs.Admin;

/// <summary>
/// DTO trả về thông tin user cho trang quản lý Admin
/// </summary>
public class AdminUserDto
{
    public int UserId { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? FullName { get; set; }
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsLocked { get; set; }
    public DateTime CreatedAt { get; set; }
    public int QuestionCount { get; set; }
    public int AnswerCount { get; set; }
}

/// <summary>
/// DTO cho request đổi role
/// </summary>
public class ChangeRoleDto
{
    public string NewRole { get; set; } = string.Empty;
}

/// <summary>
/// Response chứa danh sách user + thông tin phân trang
/// </summary>
public class PaginatedUsersDto
{
    public List<AdminUserDto> Users { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages { get; set; }
}
