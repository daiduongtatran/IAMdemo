namespace IAMDemoProject.Models;

public class AuthResponse
{
    public string Token { get; set; } = string.Empty;
    public string VaiTro { get; set; } = string.Empty;
    public string TenDangNhap { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string TokenType { get; set; } = "Bearer";
}
