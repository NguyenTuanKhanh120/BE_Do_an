using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniKnowledge.Services;

namespace UniKnowledge.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IFileUploadService _fileUploadService;

    public UploadController(IFileUploadService fileUploadService)
    {
        _fileUploadService = fileUploadService;
    }

    [Authorize]
    [HttpPost("question-attachment")]
    public async Task<ActionResult> UploadQuestionAttachment(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "No file uploaded" });
            }

            // Validate file extension
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".zip", ".rar", ".jpg", ".jpeg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            if (!allowedExtensions.Contains(fileExtension))
            {
                return BadRequest(new { message = $"File type not allowed. Allowed types: {string.Join(", ", allowedExtensions)}" });
            }

            // Validate file size (10MB max)
            if (file.Length > 10 * 1024 * 1024)
            {
                return BadRequest(new { message = "File size exceeds maximum allowed size of 10MB" });
            }

            // Upload file
            var filePath = await _fileUploadService.UploadFileAsync(file, "questions");
            var fileUrl = _fileUploadService.GetFileUrl(filePath);

            return Ok(new
            {
                message = "File uploaded successfully",
                filePath = filePath,
                fileUrl = fileUrl,
                fileName = file.FileName,
                fileSize = file.Length
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error uploading file: " + ex.Message });
        }
    }

    [Authorize]
    [HttpDelete("question-attachment")]
    public async Task<ActionResult> DeleteQuestionAttachment([FromQuery] string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return BadRequest(new { message = "File path is required" });
            }

            var deleted = await _fileUploadService.DeleteFileAsync(filePath);

            if (deleted)
            {
                return Ok(new { message = "File deleted successfully" });
            }
            else
            {
                return NotFound(new { message = "File not found" });
            }
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error deleting file: " + ex.Message });
        }
    }
}

