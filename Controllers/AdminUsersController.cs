using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UniKnowledge.DTOs.Admin;
using UniKnowledge.Services;

namespace UniKnowledge.Controllers;

/// <summary>
/// Controller quản lý người dùng — CHỈ Admin mới có quyền truy cập.
/// Attribute [Authorize(Roles = "Admin")] đảm bảo JWT phải chứa role = "Admin".
/// </summary>
[ApiController]
[Route("api/admin/users")]
[Authorize(Roles = "Admin")]
public class AdminUsersController : ControllerBase
{
    private readonly IAdminService _adminService;

    public AdminUsersController(IAdminService adminService)
    {
        _adminService = adminService;
    }

    /// <summary>
    /// GET /api/admin/users?search=xxx&role=Student&page=1&pageSize=10
    /// Lấy danh sách user có phân trang, tìm kiếm và lọc role.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedUsersDto>> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
    {
        if (page < 1) page = 1;
        if (pageSize < 1 || pageSize > 50) pageSize = 10;

        var result = await _adminService.GetUsersAsync(search, role, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// PATCH /api/admin/users/{id}/toggle-status
    /// Khóa hoặc mở khóa tài khoản (đảo trạng thái IsLocked).
    /// </summary>
    [HttpPatch("{id}/toggle-status")]
    public async Task<ActionResult<AdminUserDto>> ToggleUserStatus(int id)
    {
        var user = await _adminService.ToggleUserLockAsync(id);
        if (user == null)
        {
            return NotFound(new { message = "User not found" });
        }
        return Ok(user);
    }

    /// <summary>
    /// PUT /api/admin/users/{id}/change-role
    /// Thay đổi role của người dùng (Student ↔ Admin).
    /// </summary>
    [HttpPut("{id}/change-role")]
    public async Task<ActionResult<AdminUserDto>> ChangeUserRole(int id, [FromBody] ChangeRoleDto dto)
    {
        try
        {
            var user = await _adminService.ChangeUserRoleAsync(id, dto.NewRole);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }
            return Ok(user);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
