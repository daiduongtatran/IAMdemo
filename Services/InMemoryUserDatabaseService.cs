using System.Collections.Concurrent;
using IAMDemoProject.Models;

namespace IAMDemoProject.Services;

/// <summary>
/// In-memory user store for local/demo runs without SQL Server (see Demo:UseInMemoryStore).
/// </summary>
public class InMemoryUserDatabaseService : IUserDatabaseService
{
    private const int MaxFailedAttempts = 5;

    private readonly ConcurrentDictionary<string, User> _byUsername =
        new(StringComparer.OrdinalIgnoreCase);

    public InMemoryUserDatabaseService()
    {
        SeedUser(1, "admin", "Admin");
        SeedUser(2, "user", "User");
    }

    private void SeedUser(int id, string username, string vaiTro)
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("123", workFactor: 12);
        _byUsername[username] = new User
        {
            Id = id,
            TenDangNhap = username,
            MatKhauHash = hash,
            VaiTro = vaiTro,
            BiKhoa = false,
            SoLanSaiMatKhau = 0
        };
    }

    public Task<User?> GetUserByUsernameAsync(string tenDangNhap)
    {
        return Task.FromResult(
            _byUsername.TryGetValue(tenDangNhap, out var user) ? user : null);
    }

    public Task UpdateFailedLoginAttemptsAsync(int userId, int attempts)
    {
        foreach (var kv in _byUsername)
        {
            var u = kv.Value;
            if (u.Id != userId) continue;
            u.SoLanSaiMatKhau = attempts;
            u.BiKhoa = attempts >= MaxFailedAttempts;
            break;
        }

        return Task.CompletedTask;
    }

    public Task LockAccountAsync(int userId)
    {
        foreach (var kv in _byUsername)
        {
            if (kv.Value.Id != userId) continue;
            kv.Value.BiKhoa = true;
            break;
        }

        return Task.CompletedTask;
    }

    public Task ResetFailedLoginAttemptsAsync(int userId)
    {
        foreach (var kv in _byUsername)
        {
            if (kv.Value.Id != userId) continue;
            kv.Value.SoLanSaiMatKhau = 0;
            break;
        }

        return Task.CompletedTask;
    }

    public Task<List<User>> GetAllUsersAsync()
    {
        return Task.FromResult(_byUsername.Values.OrderBy(u => u.Id).ToList());
    }

    public Task<User> CreateUserAsync(string tenDangNhap, string matKhau, string vaiTro)
    {
        var newId = _byUsername.Values.Max(u => u.Id) + 1;
        var hash = BCrypt.Net.BCrypt.HashPassword(matKhau, workFactor: 12);
        var user = new User
        {
            Id = newId,
            TenDangNhap = tenDangNhap,
            MatKhauHash = hash,
            VaiTro = vaiTro,
            BiKhoa = false,
            SoLanSaiMatKhau = 0
        };
        _byUsername[tenDangNhap] = user;
        return Task.FromResult(user);
    }

    public Task DeleteUserAsync(int userId)
    {
        var userToRemove = _byUsername.Values.FirstOrDefault(u => u.Id == userId);
        if (userToRemove != null)
        {
            _byUsername.TryRemove(userToRemove.TenDangNhap, out _);
        }
        return Task.CompletedTask;
    }

    public Task UpdateUserRoleAsync(int userId, string vaiTro)
    {
        foreach (var kv in _byUsername)
        {
            if (kv.Value.Id != userId) continue;
            kv.Value.VaiTro = vaiTro;
            break;
        }
        return Task.CompletedTask;
    }
    public Task UpdateTotpSecretAsync(int userId, string secret)
    {
        // Vì đây chỉ là file giả lập không dùng đến, ta cứ báo hoàn thành là xong!
        return Task.CompletedTask;
    }
}
