using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using UniKnowledge.Data;
using UniKnowledge.DTOs.Category;
using UniKnowledge.Models;

namespace UniKnowledge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _context;

    public CategoriesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("seed")]
    [Authorize(Roles = "Admin")]
    public async Task<ActionResult> SeedCategories()
    {
        // Double check role
        var userRole = User.FindFirstValue(ClaimTypes.Role);
        if (userRole != "Admin")
        {
            return StatusCode(403, new { message = "Only Admin users can seed categories" });
        }

        // Check if categories already exist
        if (await _context.Categories.AnyAsync())
        {
            return BadRequest(new { message = "Categories already exist. Delete them first if you want to reseed." });
        }

        var categories = new List<Category>
        {
            new Category 
            { 
                CategoryName = "Programming", 
                Description = "Questions about programming languages, algorithms, and software development",
                Slug = "programming"
            },
            new Category 
            { 
                CategoryName = "Web Development", 
                Description = "Frontend, backend, and full-stack web development questions",
                Slug = "web-development"
            },
            new Category 
            { 
                CategoryName = "Database", 
                Description = "Database design, SQL, NoSQL, and data management",
                Slug = "database"
            },
            new Category 
            { 
                CategoryName = "DevOps", 
                Description = "CI/CD, deployment, containerization, and infrastructure",
                Slug = "devops"
            },
            new Category 
            { 
                CategoryName = "Mobile Development", 
                Description = "iOS, Android, React Native, and mobile app development",
                Slug = "mobile-development"
            },
            new Category 
            { 
                CategoryName = "Data Science", 
                Description = "Machine learning, AI, data analysis, and statistics",
                Slug = "data-science"
            },
            new Category 
            { 
                CategoryName = "Security", 
                Description = "Cybersecurity, encryption, authentication, and secure coding",
                Slug = "security"
            },
            new Category 
            { 
                CategoryName = "Other", 
                Description = "General questions and topics that don't fit other categories",
                Slug = "other"
            }
        };

        _context.Categories.AddRange(categories);
        await _context.SaveChangesAsync();

        return Ok(new
        {
            message = "Categories seeded successfully",
            count = categories.Count,
            categories = categories.Select(c => new
            {
                c.CategoryId,
                c.CategoryName,
                c.Description,
                c.Slug
            })
        });
    }

    [HttpGet]
    public async Task<ActionResult<List<CategoryDto>>> GetCategories()
    {
        var categories = await _context.Categories
            .Include(c => c.Questions)
            .OrderBy(c => c.CategoryName)
            .ToListAsync();

        var result = categories.Select(c => new CategoryDto
        {
            CategoryId = c.CategoryId,
            CategoryName = c.CategoryName,
            Description = c.Description,
            Slug = c.Slug,
            QuestionCount = c.Questions.Count
        }).ToList();

        return Ok(result);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<CategoryDto>> GetCategory(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Questions)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
        {
            return NotFound(new { message = "Category not found" });
        }

        var result = new CategoryDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            Slug = category.Slug,
            QuestionCount = category.Questions.Count
        };

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<CategoryDto>> CreateCategory([FromBody] CreateCategoryDto dto)
    {
        // Check if category name already exists
        if (await _context.Categories.AnyAsync(c => c.CategoryName == dto.CategoryName))
        {
            return BadRequest(new { message = "Category name already exists" });
        }

        // Check if slug already exists
        if (await _context.Categories.AnyAsync(c => c.Slug == dto.Slug))
        {
            return BadRequest(new { message = "Slug already exists" });
        }

        var category = new Category
        {
            CategoryName = dto.CategoryName,
            Description = dto.Description,
            Slug = dto.Slug
        };

        _context.Categories.Add(category);
        await _context.SaveChangesAsync();

        var result = new CategoryDto
        {
            CategoryId = category.CategoryId,
            CategoryName = category.CategoryName,
            Description = category.Description,
            Slug = category.Slug,
            QuestionCount = 0
        };

        return CreatedAtAction(nameof(GetCategory), new { id = category.CategoryId }, result);
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto)
    {
        var category = await _context.Categories.FindAsync(id);

        if (category == null)
        {
            return NotFound(new { message = "Category not found" });
        }

        // Check if new category name already exists (excluding current category)
        if (!string.IsNullOrEmpty(dto.CategoryName) && 
            await _context.Categories.AnyAsync(c => c.CategoryName == dto.CategoryName && c.CategoryId != id))
        {
            return BadRequest(new { message = "Category name already exists" });
        }

        // Check if new slug already exists (excluding current category)
        if (!string.IsNullOrEmpty(dto.Slug) && 
            await _context.Categories.AnyAsync(c => c.Slug == dto.Slug && c.CategoryId != id))
        {
            return BadRequest(new { message = "Slug already exists" });
        }

        if (!string.IsNullOrEmpty(dto.CategoryName))
            category.CategoryName = dto.CategoryName;

        if (dto.Description != null)
            category.Description = dto.Description;

        if (!string.IsNullOrEmpty(dto.Slug))
            category.Slug = dto.Slug;

        await _context.SaveChangesAsync();

        // Reload with question count
        var updatedCategory = await _context.Categories
            .Include(c => c.Questions)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        var result = new CategoryDto
        {
            CategoryId = updatedCategory!.CategoryId,
            CategoryName = updatedCategory.CategoryName,
            Description = updatedCategory.Description,
            Slug = updatedCategory.Slug,
            QuestionCount = updatedCategory.Questions.Count
        };

        return Ok(result);
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCategory(int id)
    {
        var category = await _context.Categories
            .Include(c => c.Questions)
            .FirstOrDefaultAsync(c => c.CategoryId == id);

        if (category == null)
        {
            return NotFound(new { message = "Category not found" });
        }

        // Check if category has questions
        if (category.Questions.Any())
        {
            return BadRequest(new { message = "Cannot delete category with existing questions. Please reassign or delete the questions first." });
        }

        _context.Categories.Remove(category);
        await _context.SaveChangesAsync();

        return NoContent();
    }
}

