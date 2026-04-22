using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace IAMDemoProject.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // GIẢ LẬP: Nếu user là admin/123 thì cấp quyền Admin, còn lại là User
        string role = (request.Username == "admin" && request.Password == "123") ? "Admin" : "User";
        
        if (request.Password != "123") return Unauthorized("Sai mật khẩu rồi!");

        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("Key_Sieu_Bao_Mat_Nhom_Minh_123456");
        
        var tokenDescriptor = new SecurityTokenDescriptor {
            Subject = new ClaimsIdentity(new[] {
                new Claim(ClaimTypes.Name, request.Username),
                new Claim("role", role) // ĐÃ SỬA: Thay ClaimTypes.Role bằng "role"
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return Ok(new { 
            Token = tokenHandler.WriteToken(token),
            Role = role,
            Message = "Đăng nhập thành công!" 
        });
    }
}

public class LoginRequest {
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
}