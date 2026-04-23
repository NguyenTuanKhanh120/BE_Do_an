using Microsoft.EntityFrameworkCore;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Admin;

namespace UniKnowledge.Services;

public interface IAdminService
{
    Task<PaginatedUsersDto> GetUsersAsync(string? search, string? role, int page, int pageSize);
    Task<AdminUserDto?> ToggleUserLockAsync(int userId);
    Task<AdminUserDto?> ChangeUserRoleAsync(int userId, string newRole);
}

public class AdminService : IAdminService
{
    private readonly AppDbContext _context;
    // Danh sách role hợp lệ trong hệ thống
    private static readonly string[] ValidRoles = { "Student", "Admin" };

    public AdminService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Lấy danh sách user có phân trang, tìm kiếm và lọc theo role.
    /// </summary>
    public async Task<PaginatedUsersDto> GetUsersAsync(string? search, string? role, int page, int pageSize)
    {
        // Bắt đầu từ toàn bộ Users, include Questions & Answers để đếm
        var query = _context.Users
            .Include(u => u.Questions)
            .Include(u => u.Answers)
            .AsQueryable();

        // Lọc theo từ khóa tìm kiếm (tên, email, username)
        if (!string.IsNullOrWhiteSpace(search))
        {
            var keyword = search.Trim().ToLower();
            query = query.Where(u =>
                u.Username.ToLower().Contains(keyword) ||
                u.Email.ToLower().Contains(keyword) ||
                (u.FullName != null && u.FullName.ToLower().Contains(keyword))
            );
        }

        // Lọc theo Role (nếu có)
        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role == role);
        }

        // Đếm tổng số kết quả (trước khi phân trang)
        var totalCount = await query.CountAsync();

        // Phân trang: Skip + Take
        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserDto
            {
                UserId = u.UserId,
                Username = u.Username,
                Email = u.Email,
                FullName = u.FullName,
                AvatarUrl = u.AvatarUrl,
                Role = u.Role,
                IsLocked = u.IsLocked,
                CreatedAt = u.CreatedAt,
                QuestionCount = u.Questions.Count,
                AnswerCount = u.Answers.Count
            })
            .ToListAsync();

        return new PaginatedUsersDto
        {
            Users = users,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling((double)totalCount / pageSize)
        };
    }

    /// <summary>
    /// Toggle khóa/mở khóa tài khoản. Trả về DTO sau khi cập nhật.
    /// </summary>
    public async Task<AdminUserDto?> ToggleUserLockAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Questions)
            .Include(u => u.Answers)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null) return null;

        // Đảo trạng thái: đang khóa → mở, đang mở → khóa
        user.IsLocked = !user.IsLocked;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(user);
    }

    /// <summary>
    /// Thay đổi role của user. Chỉ chấp nhận role hợp lệ.
    /// </summary>
    public async Task<AdminUserDto?> ChangeUserRoleAsync(int userId, string newRole)
    {
        // Kiểm tra role hợp lệ
        if (!ValidRoles.Contains(newRole))
        {
            throw new ArgumentException($"Invalid role '{newRole}'. Valid roles: {string.Join(", ", ValidRoles)}");
        }

        var user = await _context.Users
            .Include(u => u.Questions)
            .Include(u => u.Answers)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null) return null;

        user.Role = newRole;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(user);
    }

    /// <summary>
    /// Map User entity → AdminUserDto
    /// </summary>
    private static AdminUserDto MapToDto(Models.User user)
    {
        return new AdminUserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            IsLocked = user.IsLocked,
            CreatedAt = user.CreatedAt,
            QuestionCount = user.Questions.Count,
            AnswerCount = user.Answers.Count
        };
    }
}
