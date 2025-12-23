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

    [HttpGet("suggest")]
    public async Task<ActionResult<List<TagResponseDto>>> SuggestTags(
        [FromQuery] string? query, 
        [FromQuery] int limit = 10)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            return Ok(new List<TagResponseDto>());
        }

        var tags = await _context.Tags
            .Include(t => t.QuestionTags)
            .Where(t => t.TagName.Contains(query))
            .OrderByDescending(t => t.QuestionTags.Count)
            .ThenBy(t => t.TagName)
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

    [HttpGet("trending")]
    public async Task<ActionResult<List<TagResponseDto>>> GetTrendingTags(
        [FromQuery] int days = 7, 
        [FromQuery] int limit = 20)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-days);
        
        var tags = await _context.Tags
            .Include(t => t.QuestionTags)
                .ThenInclude(qt => qt.Question)
            .Select(t => new
            {
                Tag = t,
                RecentQuestionCount = t.QuestionTags
                    .Count(qt => qt.Question.CreatedAt >= cutoffDate && qt.Question.Status != "Hidden"),
                TotalQuestionCount = t.QuestionTags.Count
            })
            .Where(x => x.RecentQuestionCount > 0)
            .OrderByDescending(x => x.RecentQuestionCount)
            .ThenByDescending(x => x.TotalQuestionCount)
            .Take(limit)
            .ToListAsync();

        var result = tags.Select(x => new TagResponseDto
        {
            TagId = x.Tag.TagId,
            TagName = x.Tag.TagName,
            Description = x.Tag.Description,
            QuestionCount = x.TotalQuestionCount
        }).ToList();

        return Ok(result);
    }

    [HttpPost("questions/filter")]
    public async Task<ActionResult> FilterQuestionsByTags([FromBody] TagFilterDto dto)
    {
        var query = _context.Questions
            .Include(q => q.User)
            .Include(q => q.Category)
            .Include(q => q.QuestionTags)
                .ThenInclude(qt => qt.Tag)
            .Include(q => q.Answers)
            .Include(q => q.Votes)
            .AsQueryable();

        if (dto.TagIds != null && dto.TagIds.Any())
        {
            if (dto.Logic?.ToUpper() == "OR")
            {
                // Questions có ít nhất 1 trong các tags
                query = query.Where(q => q.QuestionTags.Any(qt => dto.TagIds.Contains(qt.TagId)));
            }
            else
            {
                // Questions phải có TẤT CẢ các tags (AND)
                query = query.Where(q => dto.TagIds.All(tagId => 
                    q.QuestionTags.Any(qt => qt.TagId == tagId)));
            }
        }

        query = query.Where(q => q.Status != "Hidden");

        var total = await query.CountAsync();

        var questions = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((dto.Page - 1) * dto.PageSize)
            .Take(dto.PageSize)
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
            questions = result,
            total,
            page = dto.Page,
            pageSize = dto.PageSize,
            totalPages = (int)Math.Ceiling(total / (double)dto.PageSize)
        });
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<TagResponseDto>> UpdateTag(int id, [FromBody] UpdateTagDto dto)
    {
        var tag = await _context.Tags
            .Include(t => t.QuestionTags)
            .FirstOrDefaultAsync(t => t.TagId == id);

        if (tag == null)
        {
            return NotFound(new { message = "Tag not found" });
        }

        // Check if tag name already exists (excluding current tag)
        if (!string.IsNullOrEmpty(dto.TagName) && dto.TagName != tag.TagName)
        {
            if (await _context.Tags.AnyAsync(t => t.TagName == dto.TagName && t.TagId != id))
            {
                return BadRequest(new { message = "Tag name already exists" });
            }
        }

        // Update properties if provided
        if (!string.IsNullOrEmpty(dto.TagName))
        {
            tag.TagName = dto.TagName;
        }

        if (dto.Description != null)
        {
            tag.Description = dto.Description;
        }

        await _context.SaveChangesAsync();

        var result = new TagResponseDto
        {
            TagId = tag.TagId,
            TagName = tag.TagName,
            Description = tag.Description,
            QuestionCount = tag.QuestionTags.Count
        };

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTag(int id)
    {
        var tag = await _context.Tags
            .Include(t => t.QuestionTags)
            .FirstOrDefaultAsync(t => t.TagId == id);

        if (tag == null)
        {
            return NotFound(new { message = "Tag not found" });
        }

        // Check if tag is being used
        if (tag.QuestionTags.Any())
        {
            return BadRequest(new { message = $"Cannot delete tag because it has {tag.QuestionTags.Count} question(s) associated with it" });
        }

        _context.Tags.Remove(tag);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

