namespace UniKnowledge.Models;

/// <summary>
/// Represents a follow relationship between two users.
/// FollowerId is the user who follows, FollowingId is the user being followed.
/// </summary>
public class Follow
{
    public int FollowId { get; set; }
    public int FollowerId { get; set; }
    public int FollowingId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User Follower { get; set; } = null!;
    public User Following { get; set; } = null!;
}
