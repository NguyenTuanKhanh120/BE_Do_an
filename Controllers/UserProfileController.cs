using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniKnowledge.DTOs.User;
using UniKnowledge.DTOs.Question;
using UniKnowledge.Services;

namespace UniKnowledge.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserProfileController : ControllerBase
{
    private readonly IUserProfileService _userProfileService;
    private readonly IFollowService _followService;

    public UserProfileController(IUserProfileService userProfileService, IFollowService followService)
    {
        _userProfileService = userProfileService;
        _followService = followService;
    }

    [HttpGet("me")]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var profile = await _userProfileService.GetUserProfileAsync(userId);

        if (profile == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(profile);
    }

    [HttpGet("me/questions")]
    public async Task<ActionResult<List<QuestionResponseDto>>> GetMyQuestions()
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var questions = await _userProfileService.GetUserQuestionsAsync(userId);
        return Ok(questions);
    }

    [HttpPut("me")]
    public async Task<ActionResult<UserProfileDto>> UpdateMyProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var profile = await _userProfileService.UpdateProfileAsync(userId, dto);

            if (profile == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("me/change-password")]
    public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var result = await _userProfileService.ChangePasswordAsync(userId, dto);

            if (!result)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(new { message = "Password changed successfully" });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<UserProfileDto>> GetUserProfile(int id)
    {
        var profile = await _userProfileService.GetUserProfileAsync(id);

        if (profile == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(profile);
    }

    [HttpGet("{id}/questions")]
    [AllowAnonymous]
    public async Task<ActionResult<List<QuestionResponseDto>>> GetUserQuestions(int id)
    {
        var questions = await _userProfileService.GetUserQuestionsAsync(id);
        return Ok(questions);
    }

    /// <summary>
    /// GET /api/userprofile/{id}/public-profile
    /// Returns profile info + follower count + isFollowing flag
    /// </summary>
    [HttpGet("{id}/public-profile")]
    [AllowAnonymous]
    public async Task<ActionResult<PublicProfileDto>> GetPublicProfile(int id)
    {
        // currentUserId is null if not authenticated (anonymous visitor)
        int? currentUserId = null;
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userIdClaim != null)
        {
            currentUserId = int.Parse(userIdClaim);
        }

        var profile = await _userProfileService.GetPublicProfileAsync(id, currentUserId);

        if (profile == null)
        {
            return NotFound(new { message = "User not found" });
        }

        return Ok(profile);
    }

    /// <summary>
    /// POST /api/userprofile/{id}/toggle-follow
    /// Toggle follow/unfollow for the target user
    /// </summary>
    [HttpPost("{id}/toggle-follow")]
    public async Task<ActionResult> ToggleFollow(int id)
    {
        var currentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var isNowFollowing = await _followService.ToggleFollowAsync(currentUserId, id);
            return Ok(new { isFollowing = isNowFollowing });
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("me/upload-avatar")]
    public async Task<ActionResult<UserProfileDto>> UploadAvatar(IFormFile file)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        try
        {
            var profile = await _userProfileService.UploadAvatarAsync(userId, file);

            if (profile == null)
            {
                return NotFound(new { message = "User not found" });
            }

            return Ok(profile);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("search")]
    public async Task<ActionResult<List<UserProfileDto>>> SearchUsers([FromQuery] string? search = null, [FromQuery] int? limit = null)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var searchTerm = search ?? string.Empty;
        var limitValue = limit ?? 20;
        var users = await _userProfileService.SearchUsersAsync(searchTerm, userId, limitValue);
        return Ok(users);
    }

    /// <summary>
    /// GET /api/userprofile/search-light?keyword=xxx&limit=10
    /// Lightweight search for navbar autocomplete (returns only Id, FullName, Username, Avatar)
    /// </summary>
    [HttpGet("search-light")]
    [AllowAnonymous]
    public async Task<ActionResult<List<UserSearchDto>>> SearchUsersLight([FromQuery] string keyword = "", [FromQuery] int limit = 10)
    {
        var users = await _userProfileService.SearchUsersLightAsync(keyword, limit);
        return Ok(users);
    }
}