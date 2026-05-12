using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using IAMDemoProject.Models;
using BCrypt.Net;

namespace IAMDemoProject.Services;

public interface IAuthenticationService
{
    Task<AuthResponse> AuthenticateAsync(LoginRequest request);
    string GenerateJwtToken(User user);
    bool VerifyPassword(string password, string hash);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserDatabaseService _userDatabaseService;
    private readonly IConfiguration _configuration;
    private const int TOKEN_EXPIRATION_HOURS = 1;

    public AuthenticationService(IUserDatabaseService userDatabaseService, IConfiguration configuration)
    {
        _userDatabaseService = userDatabaseService;
        _configuration = configuration;
    }

    public async Task<AuthResponse> AuthenticateAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenDangNhap) || string.IsNullOrWhiteSpace(request.MatKhau))
            throw new AuthenticationException("Username and password required");

        var user = await _userDatabaseService.GetUserByUsernameAsync(request.TenDangNhap);
        if (user == null)
            throw new UserNotFoundException("Username or password incorrect");

        if (user.BiKhoa)
            throw new AccountLockedException("Account locked");

        if (!VerifyPassword(request.MatKhau, user.MatKhauHash))
        {
            int attempts = user.SoLanSaiMatKhau + 1;
            await _userDatabaseService.UpdateFailedLoginAttemptsAsync(user.Id, attempts);
            throw new AuthenticationException("Username or password incorrect");
        }

        await _userDatabaseService.ResetFailedLoginAttemptsAsync(user.Id);

        return new AuthResponse
        {
            Token = GenerateJwtToken(user),
            VaiTro = user.VaiTro,
            TenDangNhap = user.TenDangNhap,
            Message = "Login success",
            ExpiresIn = TOKEN_EXPIRATION_HOURS * 3600,
            TokenType = "Bearer"
        };
    }

    public string GenerateJwtToken(User user)
    {
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"] 
            ?? throw new InvalidOperationException("Jwt:SecretKey not found"));

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.TenDangNhap),
                new Claim("role", user.VaiTro),
                new Claim("user_id", user.Id.ToString()),
                new Claim("user_status", user.BiKhoa ? "locked" : "active"),
                new Claim("iat", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString())
            }),
            Expires = DateTime.UtcNow.AddHours(TOKEN_EXPIRATION_HOURS),
            Issuer = _configuration["Jwt:Issuer"] ?? "IAMSystem",
            Audience = _configuration["Jwt:Audience"] ?? "IAMUsers",
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };

        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityTokenHandler().CreateToken(descriptor));
    }

    public bool VerifyPassword(string password, string hash)
    {
        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            return false;
        }
    }

    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
}
