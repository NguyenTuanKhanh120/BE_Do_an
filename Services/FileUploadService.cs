using System.Security.Cryptography;
using System.Text;

namespace UniKnowledge.Services;

public interface IFileUploadService
{
    Task<string> UploadFileAsync(IFormFile file, string subFolder = "questions");
    Task<bool> DeleteFileAsync(string filePath);
    string GetFileUrl(string filePath);
}

public class FileUploadService : IFileUploadService
{
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;
    private const string UploadsFolder = "uploads";
    private const int MaxFileSize = 10 * 1024 * 1024; // 10MB

    public FileUploadService(IWebHostEnvironment environment, IConfiguration configuration)
    {
        _environment = environment;
        _configuration = configuration;
    }

    public async Task<string> UploadFileAsync(IFormFile file, string subFolder = "questions")
    {
        if (file == null || file.Length == 0)
        {
            throw new ArgumentException("File is empty");
        }

        if (file.Length > MaxFileSize)
        {
            throw new ArgumentException($"File size exceeds maximum allowed size of {MaxFileSize / 1024 / 1024}MB");
        }

        // Get uploads directory path
        var uploadsPath = Path.Combine(_environment.WebRootPath ?? Path.Combine(_environment.ContentRootPath, "wwwroot"), UploadsFolder, subFolder);
        
        // Create year/month subdirectory structure
        var now = DateTime.UtcNow;
        var yearMonthPath = Path.Combine(uploadsPath, now.Year.ToString(), now.Month.ToString("00"));
        
        // Ensure directory exists
        Directory.CreateDirectory(yearMonthPath);

        // Generate unique filename
        var fileExtension = Path.GetExtension(file.FileName);
        var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
        var filePath = Path.Combine(yearMonthPath, uniqueFileName);

        // Save file
        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        // Return relative path (e.g., "uploads/questions/2024/12/guid.pdf")
        var relativePath = Path.Combine(UploadsFolder, subFolder, now.Year.ToString(), now.Month.ToString("00"), uniqueFileName)
            .Replace('\\', '/'); // Use forward slashes for URL

        return relativePath;
    }

    public async Task<bool> DeleteFileAsync(string filePath)
    {
        try
        {
            if (string.IsNullOrEmpty(filePath))
            {
                return false;
            }

            // Handle both full URL and relative path
            string relativePath = filePath;
            
            // If it's a full URL, extract the relative path
            if (filePath.StartsWith("http://") || filePath.StartsWith("https://"))
            {
                try
                {
                    var uri = new Uri(filePath);
                    relativePath = uri.AbsolutePath.TrimStart('/');
                }
                catch
                {
                    // Fallback: extract manually
                    var parts = filePath.Split(new[] { "/uploads/" }, StringSplitOptions.None);
                    if (parts.Length > 1)
                    {
                        relativePath = "uploads/" + parts[1];
                    }
                    else
                    {
                        return false; // Could not extract path
                    }
                }
            }

            // Get wwwroot path
            var webRootPath = _environment.WebRootPath;
            if (string.IsNullOrEmpty(webRootPath))
            {
                webRootPath = Path.Combine(_environment.ContentRootPath, "wwwroot");
            }
            
            // Convert relative path to absolute path
            // relativePath: "uploads/questions/2024/12/file.pdf"
            // absolutePath: "C:\workspace\...\wwwroot\uploads\questions\2024\12\file.pdf"
            
            // Replace forward slashes with backslashes for Windows, or use Path.Combine which handles it
            var pathParts = relativePath.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
            var absolutePath = Path.Combine(webRootPath, string.Join(Path.DirectorySeparatorChar.ToString(), pathParts));
            
            // Normalize path (resolve .., ., etc.)
            absolutePath = Path.GetFullPath(absolutePath);
            
            // Verify the path is within wwwroot (security check)
            var normalizedWebRoot = Path.GetFullPath(webRootPath);
            if (!absolutePath.StartsWith(normalizedWebRoot, StringComparison.OrdinalIgnoreCase))
            {
                return false; // Path outside wwwroot, security issue
            }
            
            // Check if file exists
            if (File.Exists(absolutePath))
            {
                File.Delete(absolutePath);
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }

    public string GetFileUrl(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
        {
            return string.Empty;
        }

        // Ensure path uses forward slashes
        var normalizedPath = filePath.Replace('\\', '/');
        
        // Remove leading slash if present
        if (normalizedPath.StartsWith("/"))
        {
            normalizedPath = normalizedPath.Substring(1);
        }

        // Get base URL from configuration or use default
        var baseUrl = _configuration["BaseUrl"] ?? "http://localhost:5134";
        
        return $"{baseUrl}/{normalizedPath}";
    }
}

