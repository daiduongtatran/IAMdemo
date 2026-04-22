using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IAMDemoProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DataController : ControllerBase
{
    // Bất kỳ ai có Token đều vào được
    [Authorize]
    [HttpGet("common-info")]
    public IActionResult GetCommonInfo() => Ok("Đây là thông tin công khai cho mọi User đã đăng nhập.");

    // CHỈ ADMIN mới vào được (Chứng minh tính đúng của IAM)
    [Authorize(Roles = "Admin")]
    [HttpGet("admin-only-secret")]
    public IActionResult GetAdminSecret() => Ok("CHÚC MỪNG! Bạn đã vào được vùng dữ liệu tối mật của Admin.");
}