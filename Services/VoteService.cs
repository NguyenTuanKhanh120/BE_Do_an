using Microsoft.EntityFrameworkCore;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Vote;
using UniKnowledge.Models;

namespace UniKnowledge.Services;

public interface IVoteService
{
    Task<bool> VoteQuestionAsync(int questionId, VoteDto dto, int userId);
    Task<bool> VoteAnswerAsync(int answerId, VoteDto dto, int userId);
    Task<bool> RemoveVoteQuestionAsync(int questionId, int userId);
    Task<bool> RemoveVoteAnswerAsync(int answerId, int userId);
}

public class VoteService : IVoteService
{
    private readonly AppDbContext _context;

    public VoteService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<bool> VoteQuestionAsync(int questionId, VoteDto dto, int userId)
    {
        // Check if already voted
        var existingVote = await _context.Votes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.QuestionId == questionId);

        if (existingVote != null)
        {
            // Update existing vote
            existingVote.VoteType = dto.VoteType;
            await _context.SaveChangesAsync();
        }
        else
        {
            // Create new vote
            var vote = new Vote
            {
                UserId = userId,
                QuestionId = questionId,
                VoteType = dto.VoteType,
                CreatedAt = DateTime.UtcNow
            };

            _context.Votes.Add(vote);
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> VoteAnswerAsync(int answerId, VoteDto dto, int userId)
    {
        // Check if already voted
        var existingVote = await _context.Votes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.AnswerId == answerId);

        if (existingVote != null)
        {
            // Update existing vote
            existingVote.VoteType = dto.VoteType;
            await _context.SaveChangesAsync();
        }
        else
        {
            // Create new vote
            var vote = new Vote
            {
                UserId = userId,
                AnswerId = answerId,
                VoteType = dto.VoteType,
                CreatedAt = DateTime.UtcNow
            };

            _context.Votes.Add(vote);
            await _context.SaveChangesAsync();
        }

        return true;
    }

    public async Task<bool> RemoveVoteQuestionAsync(int questionId, int userId)
    {
        var vote = await _context.Votes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.QuestionId == questionId);

        if (vote == null)
        {
            return false;
        }

        _context.Votes.Remove(vote);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> RemoveVoteAnswerAsync(int answerId, int userId)
    {
        var vote = await _context.Votes
            .FirstOrDefaultAsync(v => v.UserId == userId && v.AnswerId == answerId);

        if (vote == null)
        {
            return false;
        }

        _context.Votes.Remove(vote);
        await _context.SaveChangesAsync();
        return true;
    }
}

