using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using IAMDemoProject.Services;
using IAMDemoProject.Models;

namespace IAMDemoProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly ILogger<DataController> _logger;
    private readonly IUserDatabaseService _userDatabaseService;

    public DataController(ILogger<DataController> logger, IUserDatabaseService userDatabaseService) 
    {
        _logger = logger;
        _userDatabaseService = userDatabaseService;
    }

    [Authorize]
    [HttpGet("common-info")]
    public IActionResult GetCommonInfo()
    {
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        var userId = User.FindFirst("user_id")?.Value ?? "Unknown";
        return Ok(new { Message = "Common info for all users", UserName = userName, UserId = userId, Timestamp = DateTime.UtcNow });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only-secret")]
    public IActionResult GetAdminSecret()
    {
        return Ok(new { Message = "Admin secret data", SecretData = new[] { "Data1", "Data2", "Data3" }, AccessLevel = "ADMIN_ONLY" });
    }

    [Authorize]
    [HttpGet("user-profile")]
    public IActionResult GetUserProfile()
    {
        var role = User.FindFirst("role")?.Value ?? "Unknown";
        if (role == "Admin") return Forbid();

        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        return Ok(new { Message = "User profile", UserName = userName, LastLogin = DateTime.UtcNow });
    }

    [Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        var userName = User.FindFirst(ClaimTypes.Name)?.Value ?? "Unknown";
        var userId = User.FindFirst("user_id")?.Value ?? "Unknown";
        var role = User.FindFirst("role")?.Value ?? "User";
        return Ok(new { UserId = userId, UserName = userName, Role = role, Permissions = GetPermissions(role) });
    }

    [Authorize]
    [HttpGet("check-permission/{permission}")]
    public IActionResult CheckPermission(string permission)
    {
        var role = User.FindFirst("role")?.Value ?? "User";
        var permissions = GetPermissions(role);
        return Ok(new { Permission = permission, HasPermission = permissions.Contains(permission), Role = role });
    }

    private string[] GetPermissions(string role) => role.ToLower() switch
    {
        "admin" => new[] { "read:all_data", "write:all_data", "delete:all_data", "manage:users", "manage:roles", "view:admin_panel" },
        "user" => new[] { "read:own_data", "write:own_data", "read:shared_data" },
        _ => new[] { "read:limited_data" }
    };

    [Authorize(Roles = "Admin")]
    [HttpGet("users")]
    public async Task<IActionResult> GetAllUsers()
    {
        try
        {
            var users = await _userDatabaseService.GetAllUsersAsync();
            var userDtos = users.Select(u => new 
            { 
                u.Id, 
                u.TenDangNhap, 
                u.VaiTro, 
                u.BiKhoa 
            }).ToList();
            return Ok(userDtos);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500, new { Message = "Error retrieving users" });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPost("users")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.TenDangNhap) || string.IsNullOrWhiteSpace(request?.MatKhau))
            return BadRequest(new { Message = "Username and password required" });

        try
        {
            var user = await _userDatabaseService.CreateUserAsync(request.TenDangNhap, request.MatKhau, request.VaiTro ?? "User");
            return Ok(new { Message = "User created", Id = user.Id, TenDangNhap = user.TenDangNhap, VaiTro = user.VaiTro });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500, new { Message = "Error creating user" });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpDelete("users/{userId}")]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        try
        {
            // Prevent deleting current admin user
            var currentUserId = User.FindFirst("user_id")?.Value;
            if (int.TryParse(currentUserId, out var id) && id == userId)
                return BadRequest(new { Message = "Cannot delete current user" });

            await _userDatabaseService.DeleteUserAsync(userId);
            return Ok(new { Message = "User deleted" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500, new { Message = "Error deleting user" });
        }
    }

    [Authorize(Roles = "Admin")]
    [HttpPut("users/{userId}/role")]
    public async Task<IActionResult> UpdateUserRole(int userId, [FromBody] UpdateUserRoleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request?.VaiTro))
            return BadRequest(new { Message = "Role required" });

        try
        {
            await _userDatabaseService.UpdateUserRoleAsync(userId, request.VaiTro);
            return Ok(new { Message = "User role updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500, new { Message = "Error updating user role" });
        }
    }

    [Authorize]
    [HttpPut("profile/update")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        try
        {
            var userId = User.FindFirst("user_id")?.Value;
            if (!int.TryParse(userId, out var uid))
                return Unauthorized(new { Message = "User not found" });

            if (string.IsNullOrWhiteSpace(request?.TenDangNhap))
                return BadRequest(new { Message = "Name is required" });

            // Update name and password if provided
            await _userDatabaseService.UpdateUserProfileAsync(uid, request.TenDangNhap, request.MatKhau);

            return Ok(new { Message = "Profile updated successfully", TenDangNhap = request.TenDangNhap });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500, new { Message = "Error updating profile" });
        }
    }
}

public class CreateUserRequest
{
    public string? TenDangNhap { get; set; }
    public string? MatKhau { get; set; }
    public string? VaiTro { get; set; }
}

public class UpdateUserRoleRequest
{
    public string? VaiTro { get; set; }
}

public class UpdateProfileRequest
{
    public string? TenDangNhap { get; set; }
    public string? MatKhau { get; set; }
}