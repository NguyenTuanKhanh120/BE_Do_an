using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniKnowledge.DTOs.Vote;
using UniKnowledge.Services;

namespace UniKnowledge.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class VotesController : ControllerBase
{
    private readonly IVoteService _voteService;

    public VotesController(IVoteService voteService)
    {
        _voteService = voteService;
    }

    [HttpPost("questions/{id}/vote")]
    public async Task<ActionResult> VoteQuestion(int id, [FromBody] VoteDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _voteService.VoteQuestionAsync(id, dto, userId);
        return Ok(new { message = "Vote recorded successfully" });
    }

    [HttpPost("answers/{id}/vote")]
    public async Task<ActionResult> VoteAnswer(int id, [FromBody] VoteDto dto)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _voteService.VoteAnswerAsync(id, dto, userId);
        return Ok(new { message = "Vote recorded successfully" });
    }

    [HttpDelete("questions/{id}/vote")]
    public async Task<ActionResult> RemoveVoteQuestion(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _voteService.RemoveVoteQuestionAsync(id, userId);

        if (!result)
        {
            return NotFound(new { message = "Vote not found" });
        }

        return Ok(new { message = "Vote removed successfully" });
    }

    [HttpDelete("answers/{id}/vote")]
    public async Task<ActionResult> RemoveVoteAnswer(int id)
    {
        var userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _voteService.RemoveVoteAnswerAsync(id, userId);

        if (!result)
        {
            return NotFound(new { message = "Vote not found" });
        }

        return Ok(new { message = "Vote removed successfully" });
    }
}

