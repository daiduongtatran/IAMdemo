namespace IAMDemoProject.Models;

/// <summary>
/// Request model cho login endpoint
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Tên đăng nhập
    /// </summary>
    public string TenDangNhap { get; set; } = string.Empty;
    
    /// <summary>
    /// Mật khẩu
    /// </summary>
    public string MatKhau { get; set; } = string.Empty;
}
