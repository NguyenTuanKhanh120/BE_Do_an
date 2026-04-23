using Microsoft.EntityFrameworkCore;
using UniKnowledge.Data;
using UniKnowledge.Models;

namespace UniKnowledge.Services;

public interface IFollowService
{
    Task<bool> ToggleFollowAsync(int followerId, int followingId);
    Task<int> GetFollowerCountAsync(int userId);
    Task<int> GetFollowingCountAsync(int userId);
    Task<bool> IsFollowingAsync(int followerId, int followingId);
}

public class FollowService : IFollowService
{
    private readonly AppDbContext _context;

    public FollowService(AppDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Toggle follow: if already following → unfollow, otherwise → follow.
    /// Returns true if now following, false if unfollowed.
    /// </summary>
    public async Task<bool> ToggleFollowAsync(int followerId, int followingId)
    {
        if (followerId == followingId)
        {
            throw new Exception("You cannot follow yourself.");
        }

        var existingFollow = await _context.Follows
            .FirstOrDefaultAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);

        if (existingFollow != null)
        {
            // Already following → unfollow
            _context.Follows.Remove(existingFollow);
            await _context.SaveChangesAsync();
            return false;
        }
        else
        {
            // Not following → follow
            var follow = new Follow
            {
                FollowerId = followerId,
                FollowingId = followingId,
                CreatedAt = DateTime.UtcNow
            };
            _context.Follows.Add(follow);
            await _context.SaveChangesAsync();
            return true;
        }
    }

    public async Task<int> GetFollowerCountAsync(int userId)
    {
        return await _context.Follows.CountAsync(f => f.FollowingId == userId);
    }

    public async Task<int> GetFollowingCountAsync(int userId)
    {
        return await _context.Follows.CountAsync(f => f.FollowerId == userId);
    }

    public async Task<bool> IsFollowingAsync(int followerId, int followingId)
    {
        return await _context.Follows
            .AnyAsync(f => f.FollowerId == followerId && f.FollowingId == followingId);
    }
}
