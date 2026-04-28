using Microsoft.EntityFrameworkCore;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Question;
using UniKnowledge.Models;

namespace UniKnowledge.Services;

public interface IQuestionService
{
    Task<List<QuestionResponseDto>> GetQuestionsAsync(string? search = null, int? categoryId = null, int? tagId = null, string? status = null, int page = 1, int pageSize = 20);
    Task<QuestionResponseDto?> GetQuestionByIdAsync(int id);
    Task<QuestionResponseDto> CreateQuestionAsync(CreateQuestionDto dto, int userId);
    Task<QuestionResponseDto?> UpdateQuestionAsync(int id, UpdateQuestionDto dto, int userId);
    Task<bool> DeleteQuestionAsync(int id, int userId);
    Task<bool> IncrementViewCountAsync(int id);
    Task<QuestionResponseDto> ShareQuestionAsync(int originalQuestionId, ShareQuestionDto dto, int userId);
}

public class QuestionService : IQuestionService
{
    private readonly AppDbContext _context;
    private readonly IFileUploadService _fileUploadService;

    public QuestionService(AppDbContext context, IFileUploadService fileUploadService)
    {
        _context = context;
        _fileUploadService = fileUploadService;
    }

    public async Task<List<QuestionResponseDto>> GetQuestionsAsync(string? search = null, int? categoryId = null, int? tagId = null, string? status = null, int page = 1, int pageSize = 20)
    {
        var query = _context.Questions
            .Include(q => q.User)
            .Include(q => q.Category)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .Include(q => q.OriginalQuestion)
                .ThenInclude(oq => oq!.User)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(q => q.Title.Contains(search) || q.Content.Contains(search));
        }

        if (categoryId.HasValue)
        {
            query = query.Where(q => q.CategoryId == categoryId.Value);
        }

        if (tagId.HasValue)
        {
            query = query.Where(q => q.QuestionTags.Any(qt => qt.TagId == tagId.Value));
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(q => q.Status == status);
        }
        else
        {
            // By default, only show Open questions
            query = query.Where(q => q.Status != "Hidden");
        }

        // Order by created date (newest first)
        query = query.OrderByDescending(q => q.CreatedAt);

        // Pagination
        var questions = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return questions.Select(q => MapToDto(q)).ToList();
    }

    public async Task<QuestionResponseDto?> GetQuestionByIdAsync(int id)
    {
        var question = await _context.Questions
            .Include(q => q.User)
            .Include(q => q.Category)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .Include(q => q.OriginalQuestion)
                .ThenInclude(oq => oq!.User)
            .FirstOrDefaultAsync(q => q.QuestionId == id);

        return question == null ? null : MapToDto(question);
    }

    public async Task<QuestionResponseDto> CreateQuestionAsync(CreateQuestionDto dto, int userId)
    {
        var question = new Question
        {
            UserId = userId,
            Title = dto.Title,
            Content = dto.Content,
            CategoryId = dto.CategoryId,
            ImageUrl = dto.ImageUrl,
            FileUrl = dto.FileUrl,
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };

        _context.Questions.Add(question);
        await _context.SaveChangesAsync();

        // Add tags
        if (dto.TagIds.Any())
        {
            var questionTags = dto.TagIds.Select(tagId => new QuestionTag
            {
                QuestionId = question.QuestionId,
                TagId = tagId
            }).ToList();

            _context.QuestionTags.AddRange(questionTags);
            await _context.SaveChangesAsync();
        }

        return (await GetQuestionByIdAsync(question.QuestionId))!;
    }

    public async Task<QuestionResponseDto?> UpdateQuestionAsync(int id, UpdateQuestionDto dto, int userId)
    {
        var question = await _context.Questions
            .Include(q => q.QuestionTags)
            .FirstOrDefaultAsync(q => q.QuestionId == id);

        if (question == null || question.UserId != userId)
        {
            return null;
        }

        if (!string.IsNullOrEmpty(dto.Title))
            question.Title = dto.Title;

        if (!string.IsNullOrEmpty(dto.Content))
            question.Content = dto.Content;

        if (dto.CategoryId.HasValue)
            question.CategoryId = dto.CategoryId.Value;

        if (dto.ImageUrl != null)
            question.ImageUrl = dto.ImageUrl;

        if (dto.FileUrl != null)
            question.FileUrl = dto.FileUrl;

        if (!string.IsNullOrEmpty(dto.Status))
            question.Status = dto.Status;

        if (dto.TagIds != null)
        {
            // Remove old tags
            _context.QuestionTags.RemoveRange(question.QuestionTags);

            // Add new tags
            var questionTags = dto.TagIds.Select(tagId => new QuestionTag
            {
                QuestionId = question.QuestionId,
                TagId = tagId
            }).ToList();

            _context.QuestionTags.AddRange(questionTags);
        }

        question.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetQuestionByIdAsync(id);
    }

    public async Task<bool> DeleteQuestionAsync(int id, int userId)
    {
        try
        {
            var question = await _context.Questions
                .Include(q => q.QuestionTags)
                .Include(q => q.Votes)
                .FirstOrDefaultAsync(q => q.QuestionId == id);

            if (question == null || question.UserId != userId)
            {
                return false;
            }


            // xóa file
            if (!string.IsNullOrEmpty(question.FileUrl))
            {
                try
                {
                    await _fileUploadService.DeleteFileAsync(question.FileUrl);
                }
                catch
                {
                    // Continue with question deletion even if file deletion fails
                }
            }

            if (question.QuestionTags != null && question.QuestionTags.Any())
            {
                _context.QuestionTags.RemoveRange(question.QuestionTags);
            }

            if (question.Votes != null && question.Votes.Any())
            {
                _context.Votes.RemoveRange(question.Votes);
            }

            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> IncrementViewCountAsync(int id)
    {
        var question = await _context.Questions.FirstOrDefaultAsync(q => q.QuestionId == id);

        if (question == null)
        {
            return false;
        }

        question.ViewCount++;
        await _context.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Chia sẻ câu hỏi: tạo Question mới với OriginalQuestionId trỏ về bài gốc
    /// </summary>
    public async Task<QuestionResponseDto> ShareQuestionAsync(int originalQuestionId, ShareQuestionDto dto, int userId)
    {
        // Kiểm tra bài gốc có tồn tại không
        var originalQuestion = await _context.Questions
            .Include(q => q.Category)
            .FirstOrDefaultAsync(q => q.QuestionId == originalQuestionId);

        if (originalQuestion == null)
            throw new Exception("Original question not found.");

        // Tạo bài share mới
        var sharedQuestion = new Question
        {
            UserId = userId,
            Title = originalQuestion.Title,  // Giữ tiêu đề bài gốc
            Content = dto.Content ?? "",      // Lời bình của người share
            CategoryId = originalQuestion.CategoryId,
            OriginalQuestionId = originalQuestionId,  // Trỏ về bài gốc
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };

        _context.Questions.Add(sharedQuestion);
        await _context.SaveChangesAsync();

        // Reload đầy đủ để map DTO
        var created = await _context.Questions
            .Include(q => q.User)
            .Include(q => q.Category)
            .Include(q => q.QuestionTags).ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .Include(q => q.OriginalQuestion).ThenInclude(oq => oq!.User)
            .FirstAsync(q => q.QuestionId == sharedQuestion.QuestionId);

        return MapToDto(created);
    }

    private QuestionResponseDto MapToDto(Question q)
    {
        var dto = new QuestionResponseDto
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
            HasAcceptedAnswer = q.Answers.Any(a => a.IsAccepted),
            OriginalQuestionId = q.OriginalQuestionId
        };

        // Nếu là bài share → map thông tin trích dẫn bài gốc
        if (q.OriginalQuestion != null)
        {
            dto.OriginalQuestion = new OriginalQuestionSummaryDto
            {
                QuestionId = q.OriginalQuestion.QuestionId,
                Title = q.OriginalQuestion.Title,
                ContentPreview = q.OriginalQuestion.Content.Length > 200
                    ? q.OriginalQuestion.Content.Substring(0, 200) + "..."
                    : q.OriginalQuestion.Content,
                UserId = q.OriginalQuestion.UserId,
                Username = q.OriginalQuestion.User.Username,
                AvatarUrl = q.OriginalQuestion.User.AvatarUrl
            };
        }

        return dto;
    }
}

