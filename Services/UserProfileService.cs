using Microsoft.EntityFrameworkCore;
using UniKnowledge.Data;
using UniKnowledge.DTOs.User;
using UniKnowledge.DTOs.Question;
using UniKnowledge.Models;

namespace UniKnowledge.Services;

public interface IUserProfileService
{
    Task<UserProfileDto?> GetUserProfileAsync(int userId);
    Task<List<QuestionResponseDto>> GetUserQuestionsAsync(int userId);
    Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto);
    Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto);
    Task<UserProfileDto?> UploadAvatarAsync(int userId, IFormFile file);

}

public class UserProfileService : IUserProfileService
{
    private readonly AppDbContext _context;

    public UserProfileService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<UserProfileDto?> GetUserProfileAsync(int userId)
    {
        var user = await _context.Users
            .Include(u => u.Questions)
            .Include(u => u.Answers)
            .FirstOrDefaultAsync(u => u.UserId == userId);

        if (user == null)
        {
            return null;
        }

        return new UserProfileDto
        {
            UserId = user.UserId,
            Username = user.Username,
            Email = user.Email,
            FullName = user.FullName,
            AvatarUrl = user.AvatarUrl,
            Role = user.Role,
            CreatedAt = user.CreatedAt,
            QuestionCount = user.Questions.Count,
            AnswerCount = user.Answers.Count
        };
    }

    public async Task<List<QuestionResponseDto>> GetUserQuestionsAsync(int userId)
    {
        var questions = await _context.Questions
            .Include(q => q.User)
            .Include(q => q.Category)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .Where(q => q.UserId == userId)
            .OrderByDescending(q => q.CreatedAt)
            .ToListAsync();

        return questions.Select(q => new QuestionResponseDto
        {
            QuestionId = q.QuestionId,
            Title = q.Title,
            Content = q.Content,
            ViewCount = q.ViewCount,
            Status = q.Status,
            ImageUrl = q.ImageUrl,
            FileUrl = q.FileUrl,
            CreatedAt = q.CreatedAt,
            UpdatedAt = q.UpdatedAt,
            UserId = q.UserId,
            Username = q.User.Username,
            AvatarUrl = q.User.AvatarUrl,
            CategoryId = q.CategoryId,
            CategoryName = q.Category.CategoryName,
            Tags = q.QuestionTags.Select(qt => new TagDto
            {
                TagId = qt.TagId,
                TagName = qt.Tag.TagName
            }).ToList(),
            AnswerCount = q.Answers.Count,
            VoteCount = q.Votes.Sum(v => v.VoteType),
            HasAcceptedAnswer = q.Answers.Any(a => a.IsAccepted)
        }).ToList();
    }

    public async Task<UserProfileDto?> UpdateProfileAsync(int userId, UpdateProfileDto dto)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return null;
        }

        // Check if new username already exists (excluding current user)
        if (!string.IsNullOrEmpty(dto.Username) && dto.Username != user.Username)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username && u.UserId != userId))
            {
                throw new Exception("Username already exists");
            }
            user.Username = dto.Username;
        }

        if (dto.FullName != null)
        {
            user.FullName = dto.FullName;
        }

        if (dto.AvatarUrl != null)
        {
            user.AvatarUrl = dto.AvatarUrl;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetUserProfileAsync(userId);
    }

    public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto dto)
    {
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
        {
            return false;
        }

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(dto.CurrentPassword, user.PasswordHash))
        {
            throw new Exception("Current password is incorrect");
        }

        // Hash new password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return true;
    }
    public async Task<UserProfileDto?> UploadAvatarAsync(int userId, IFormFile file)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null) return null;

        // Validate file
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

        if (!allowedExtensions.Contains(extension))
        {
            throw new Exception("Only image files (jpg, jpeg, png, gif) are allowed");
        }

        if (file.Length > 2 * 1024 * 1024) // 2MB
        {
            throw new Exception("File size must not exceed 2MB");
        }

        // Create uploads directory if not exists
        var uploadsPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
        Directory.CreateDirectory(uploadsPath);

        // Delete old avatar if exists
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            var oldFileName = Path.GetFileName(user.AvatarUrl);
            var oldFilePath = Path.Combine(uploadsPath, oldFileName);
            if (File.Exists(oldFilePath))
            {
                File.Delete(oldFilePath);
            }
        }

        // Save new file
        var fileName = $"{userId}_{Guid.NewGuid()}{extension}";
        var filePath = Path.Combine(uploadsPath, fileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Update database
        user.AvatarUrl = $"http://localhost:5134/uploads/avatars/{fileName}";
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetUserProfileAsync(userId);
    }
}