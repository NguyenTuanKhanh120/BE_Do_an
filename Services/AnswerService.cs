using Microsoft.EntityFrameworkCore;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Answer;
using UniKnowledge.Models;

namespace UniKnowledge.Services;

public interface IAnswerService
{
    Task<List<AnswerResponseDto>> GetAnswersByQuestionIdAsync(int questionId);
    Task<AnswerResponseDto> CreateAnswerAsync(int questionId, CreateAnswerDto dto, int userId);
    Task<AnswerResponseDto?> UpdateAnswerAsync(int id, UpdateAnswerDto dto, int userId);
    Task<bool> DeleteAnswerAsync(int id, int userId);
    Task<bool> AcceptAnswerAsync(int id, int userId);
}

public class AnswerService : IAnswerService
{
    private readonly AppDbContext _context;

    public AnswerService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<AnswerResponseDto>> GetAnswersByQuestionIdAsync(int questionId)
    {
        var answers = await _context.Answers
            .Include(a => a.User)
            .Include(a => a.Votes)
            .Where(a => a.QuestionId == questionId)
            .OrderByDescending(a => a.IsAccepted)
            .ThenByDescending(a => a.CreatedAt)
            .ToListAsync();

        return answers.Select(a => MapToDto(a)).ToList();
    }

    public async Task<AnswerResponseDto> CreateAnswerAsync(int questionId, CreateAnswerDto dto, int userId)
    {
        var answer = new Answer
        {
            QuestionId = questionId,
            UserId = userId,
            Content = dto.Content,
            CreatedAt = DateTime.UtcNow
        };

        _context.Answers.Add(answer);
        await _context.SaveChangesAsync();

        return MapToDto(await _context.Answers
            .Include(a => a.User)
            .Include(a => a.Votes)
            .FirstAsync(a => a.AnswerId == answer.AnswerId));
    }

    public async Task<AnswerResponseDto?> UpdateAnswerAsync(int id, UpdateAnswerDto dto, int userId)
    {
        var answer = await _context.Answers
            .Include(a => a.User)
            .Include(a => a.Votes)
            .FirstOrDefaultAsync(a => a.AnswerId == id);

        if (answer == null || answer.UserId != userId)
        {
            return null;
        }

        answer.Content = dto.Content;
        answer.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return MapToDto(answer);
    }

    public async Task<bool> DeleteAnswerAsync(int id, int userId)
    {
        var answer = await _context.Answers.FirstOrDefaultAsync(a => a.AnswerId == id);

        if (answer == null || answer.UserId != userId)
        {
            return false;
        }

        _context.Answers.Remove(answer);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> AcceptAnswerAsync(int id, int userId)
    {
        var answer = await _context.Answers
            .Include(a => a.Question)
            .FirstOrDefaultAsync(a => a.AnswerId == id);

        if (answer == null || answer.Question.UserId != userId)
        {
            return false;
        }

        // Unaccept other answers
        var otherAnswers = await _context.Answers
            .Where(a => a.QuestionId == answer.QuestionId && a.AnswerId != id)
            .ToListAsync();

        foreach (var other in otherAnswers)
        {
            other.IsAccepted = false;
        }

        answer.IsAccepted = true;
        await _context.SaveChangesAsync();
        return true;
    }

    private AnswerResponseDto MapToDto(Answer a)
    {
        return new AnswerResponseDto
        {
            AnswerId = a.AnswerId,
            QuestionId = a.QuestionId,
            Content = a.Content,
            IsAccepted = a.IsAccepted,
            CreatedAt = a.CreatedAt,
            UpdatedAt = a.UpdatedAt,
            UserId = a.UserId,
            Username = a.User.Username,
            AvatarUrl = a.User.AvatarUrl,
            VoteCount = a.Votes.Sum(v => v.VoteType)
        };
    }
}

