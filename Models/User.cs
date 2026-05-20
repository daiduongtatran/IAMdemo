namespace IAMDemoProject.Models;

public class User
{
    public int Id { get; set; }
    public string TenDangNhap { get; set; } = string.Empty;
    public string MatKhauHash { get; set; } = string.Empty;
    public string VaiTro { get; set; } = "User";
    public bool BiKhoa { get; set; } = false;
    public int SoLanSaiMatKhau { get; set; } = 0;
    public string? TotpSecret { get; set; }
}
