using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using IAMDemoProject.Models;
using OtpNet; 
using BCrypt.Net;

namespace IAMDemoProject.Services;

public interface IAuthenticationService
{
    Task<object> AuthenticateAsync(LoginRequest request);
    Task<AuthResponse> VerifyMfaAsync(VerifyMfaRequest request);
    string GenerateJwtToken(User user);
    bool VerifyPassword(string password, string hash);
}

public class AuthenticationService : IAuthenticationService
{
    private readonly IUserDatabaseService _userDatabaseService;
    private readonly IConfiguration _configuration;
    private readonly string _jwtSecretKey;
    private const int TOKEN_EXPIRATION_HOURS = 1;

    public AuthenticationService(IUserDatabaseService userDatabaseService, IConfiguration configuration)
    {
        _userDatabaseService = userDatabaseService;
        _configuration = configuration;
        _jwtSecretKey = configuration["Jwt:SecretKey"] ?? "Key_Sieu_Bao_Mat_Nhom_Minh_123456_VeryLongKeyForHigherSecurity";
    }

    public async Task<object> AuthenticateAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.TenDangNhap) || string.IsNullOrWhiteSpace(request.MatKhau))
            throw new AuthenticationException("Username and password required");

        var user = await _userDatabaseService.GetUserByUsernameAsync(request.TenDangNhap);
        if (user == null || user.BiKhoa) throw new AuthenticationException("Tài khoản không hợp lệ hoặc bị khóa!");

        if (!VerifyPassword(request.MatKhau, user.MatKhauHash))
        {
            await _userDatabaseService.UpdateFailedLoginAttemptsAsync(user.Id, user.SoLanSaiMatKhau + 1);
            throw new AuthenticationException("Sai mật khẩu!");
        }
        await _userDatabaseService.ResetFailedLoginAttemptsAsync(user.Id);

        // ĐÃ SỬA: Dùng tên tự định nghĩa "mfa_username" thay vì ClaimTypes.Name dễ bị hệ thống đổi tên
        var tempKey = Encoding.UTF8.GetBytes(_jwtSecretKey);
        var tempDescriptor = new SecurityTokenDescriptor { 
            Subject = new ClaimsIdentity(new[] { 
                new Claim("mfa_user_id", user.Id.ToString()),
                new Claim("mfa_username", user.TenDangNhap) 
            }), 
            Expires = DateTime.UtcNow.AddMinutes(5), 
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(tempKey), SecurityAlgorithms.HmacSha256Signature) 
        };
        var tempToken = new JwtSecurityTokenHandler().WriteToken(new JwtSecurityTokenHandler().CreateToken(tempDescriptor));

        if (string.IsNullOrEmpty(user.TotpSecret))
        {
            var secretKeyBytes = KeyGeneration.GenerateRandomKey(20);
            var secretString = Base32Encoding.ToString(secretKeyBytes);
            
            await _userDatabaseService.UpdateTotpSecretAsync(user.Id, secretString);
            
            var issuer = "IAM_System";
            var qrUri = $"otpauth://totp/{issuer}:{user.TenDangNhap}?secret={secretString}&issuer={issuer}";

            return new MfaRequiredResponse { TempToken = tempToken, IsSetupRequired = true, QrCodeUri = qrUri, SetupCode = secretString };
        }

        return new MfaRequiredResponse { TempToken = tempToken, IsSetupRequired = false };
    }

    public async Task<AuthResponse> VerifyMfaAsync(VerifyMfaRequest request)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_jwtSecretKey);
        
        try 
        {
            tokenHandler.ValidateToken(request.TempToken, new TokenValidationParameters { ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(key), ValidateIssuer = false, ValidateAudience = false, ClockSkew = TimeSpan.Zero }, out SecurityToken validatedToken);
            var jwtToken = (JwtSecurityToken)validatedToken;
            
            // ĐÃ SỬA: Tìm đúng cái tên "mfa_username" do mình đặt
            var username = jwtToken.Claims.First(x => x.Type == "mfa_username").Value;
            var user = await _userDatabaseService.GetUserByUsernameAsync(username);

            if (user == null || string.IsNullOrEmpty(user.TotpSecret)) throw new AuthenticationException("MFA không hợp lệ: Không tìm thấy khóa bí mật.");

            var totp = new Totp(Base32Encoding.ToBytes(user.TotpSecret));
            bool isValid = totp.VerifyTotp(request.OtpCode, out long timeStepMatched, window: null);

            if (!isValid) throw new AuthenticationException("Mã OTP không chính xác hoặc đã hết hạn!");

            return new AuthResponse { Token = GenerateJwtToken(user), VaiTro = user.VaiTro, TenDangNhap = user.TenDangNhap, Message = "Login success", ExpiresIn = TOKEN_EXPIRATION_HOURS * 3600, TokenType = "Bearer" };
        }
        catch (AuthenticationException) { throw; }
        catch (Exception ex) 
        { 
            // Bắt và in lỗi thật ra console để dễ debug, không giấu lỗi nữa
            Console.WriteLine($"[LỖI MFA]: {ex.Message}");
            throw new AuthenticationException($"Lỗi xử lý Token: {ex.Message}"); 
        }
    }

    public string GenerateJwtToken(User user)
    {
        var key = Encoding.UTF8.GetBytes(_jwtSecretKey);
        var descriptor = new SecurityTokenDescriptor { Subject = new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), new Claim(ClaimTypes.Name, user.TenDangNhap), new Claim("role", user.VaiTro), new Claim("user_id", user.Id.ToString()) }), Expires = DateTime.UtcNow.AddHours(TOKEN_EXPIRATION_HOURS), Issuer = _configuration["Jwt:Issuer"] ?? "IAMSystem", Audience = _configuration["Jwt:Audience"] ?? "IAMUsers", SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature) };
        return new JwtSecurityTokenHandler().WriteToken(new JwtSecurityTokenHandler().CreateToken(descriptor));
    }
    public bool VerifyPassword(string password, string hash) { try { return BCrypt.Net.BCrypt.Verify(password, hash); } catch { return false; } }
    public string HashPassword(string password) => BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
}

public class MfaRequiredResponse
{
    public string TempToken { get; set; } = string.Empty;
    public bool IsSetupRequired { get; set; } = false;
    public string? QrCodeUri { get; set; }
    public string? SetupCode { get; set; }
}

public class VerifyMfaRequest
{
    public string TempToken { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
}