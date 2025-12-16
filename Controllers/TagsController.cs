using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Tag;
using UniKnowledge.Models;

namespace UniKnowledge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TagsController : ControllerBase
{
    private readonly AppDbContext _context;

    public TagsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("seed")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SeedTags()
    {
        // Double check role
        var userRole = User.FindFirstValue(ClaimTypes.Role);
        if (userRole != "Admin")
        {
            return StatusCode(403, new { message = "Only Admin users can seed tags" });
        }

        // Check if tags already exist
        if (await _context.Tags.AnyAsync())
        {
            // Clear existing tags first
            var existingTags = await _context.Tags.ToListAsync();
            _context.Tags.RemoveRange(existingTags);
            await _context.SaveChangesAsync();
        }

        var tags = new List<Tag>
        {
            new Tag { TagName = "C#", Description = "C# programming language" },
            new Tag { TagName = "Reactjs", Description = "React JavaScript library" },
            new Tag { TagName = "NodeJs", Description = "Node.js runtime environment" }
        };

        _context.Tags.AddRange(tags);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Tags seeded successfully",
            count = tags.Count,
            tags = tags.Select(t => new
            {
                t.TagId,
                t.TagName,
                t.Description
            })
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<TagResponseDto>> CreateTag([FromBody] CreateTagDto dto)
    {
        // Check if tag name already exists
        if (await _context.Tags.AnyAsync(t => t.TagName == dto.TagName))
        {
            return BadRequest(new { message = "Tag name already exists" });
        }

        var tag = new Tag
        {
            TagName = dto.TagName,
            Description = dto.Description
        };

        _context.Tags.Add(tag);
        await _context.SaveChangesAsync();

        var result = new TagResponseDto
        {
            TagId = tag.TagId,
            TagName = tag.TagName,
            Description = tag.Description,
            QuestionCount = 0
        };

        return CreatedAtAction(nameof(GetTag), new { id = tag.TagId }, result);
    }

    [HttpGet]
    public async Task<ActionResult<List<TagResponseDto>>> GetTags([FromQuery] string? search)
    {
        var query = _context.Tags
            .Include(t => t.QuestionTags)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(t => t.TagName.Contains(search));
        }

        var tags = await query
            .OrderBy(t => t.TagName)
            .ToListAsync();

        var result = tags.Select(t => new TagResponseDto
        {
            TagId = t.TagId,
            TagName = t.TagName,
            Description = t.Description,
            QuestionCount = t.QuestionTags.Count
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<TagResponseDto>> GetTag(int id)
    {
        var tag = await _context.Tags
            .Include(t => t.QuestionTags)
            .FirstOrDefaultAsync(t => t.TagId == id);

        if (tag == null)
        {
            return NotFound(new { message = "Tag not found" });
        }

        var result = new TagResponseDto
        {
            TagId = tag.TagId,
            TagName = tag.TagName,
            Description = tag.Description,
            QuestionCount = tag.QuestionTags.Count
        };

        return Ok(result);
    }

    [HttpGet("popular")]
    public async Task<ActionResult<List<TagResponseDto>>> GetPopularTags([FromQuery] int limit = 20)
    {
        var tags = await _context.Tags
            .Include(t => t.QuestionTags)
            .OrderByDescending(t => t.QuestionTags.Count)
            .Take(limit)
            .ToListAsync();

        var result = tags.Select(t => new TagResponseDto
        {
            TagId = t.TagId,
            TagName = t.TagName,
            Description = t.Description,
            QuestionCount = t.QuestionTags.Count
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id}/questions")]
    public async Task<ActionResult> GetQuestionsByTag(int id, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var tag = await _context.Tags.FirstOrDefaultAsync(t => t.TagId == id);
        if (tag == null)
        {
            return NotFound(new { message = $"Tag with id {id} not found" });
        }

        var questions = await _context.Questions
            .Include(q => q.User)
            .Include(q => q.Category)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .Where(q => q.QuestionTags.Any(qt => qt.TagId == id) && q.Status != "Hidden")
            .OrderByDescending(q => q.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var result = questions.Select(q => new
        {
            q.QuestionId,
            q.Title,
            q.Content,
            q.ViewCount,
            q.Status,
            q.CreatedAt,
            User = new { q.User.Username, q.User.AvatarUrl },
            Category = q.Category != null ? new { q.Category.CategoryId, q.Category.CategoryName } : null,
            Tags = q.QuestionTags.Select(qt => new { qt.Tag.TagId, qt.Tag.TagName }),
            AnswerCount = q.Answers.Count,
            VoteCount = q.Votes.Sum(v => v.VoteType)
        }).ToList();

        return Ok(new
        {
            tagId = id,
            tagName = tag.TagName,
            questions = result,
            total = await _context.Questions.CountAsync(q => q.QuestionTags.Any(qt => qt.TagId == id) && q.Status != "Hidden")
        });
    }
}

