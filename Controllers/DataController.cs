using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IAMDemoProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    private readonly ILogger<DataController> _logger;

    public DataController(ILogger<DataController> logger) => _logger = logger;

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
}