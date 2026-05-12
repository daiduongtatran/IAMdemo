using Microsoft.Data.SqlClient;
using IAMDemoProject.Models;

namespace IAMDemoProject.Services;

public interface IUserDatabaseService
{
    Task<User?> GetUserByUsernameAsync(string tenDangNhap);
    Task UpdateFailedLoginAttemptsAsync(int userId, int attempts);
    Task LockAccountAsync(int userId);
    Task ResetFailedLoginAttemptsAsync(int userId);
}

public class UserDatabaseService : IUserDatabaseService
{
    private readonly string _connectionString;
    private const int MAX_FAILED_ATTEMPTS = 5;

    public UserDatabaseService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not found");
    }

    public async Task<User?> GetUserByUsernameAsync(string tenDangNhap)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(
                    "SELECT Id, TenDangNhap, MatKhauHash, VaiTro, BiKhoa, SoLanSaiMatKhau FROM NguoiDung WHERE TenDangNhap = @TenDangNhap", 
                    connection))
                {
                    command.Parameters.AddWithValue("@TenDangNhap", tenDangNhap);
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            return new User
                            {
                                Id = (int)reader["Id"],
                                TenDangNhap = reader["TenDangNhap"].ToString() ?? string.Empty,
                                MatKhauHash = reader["MatKhauHash"].ToString() ?? string.Empty,
                                VaiTro = reader["VaiTro"].ToString() ?? "User",
                                BiKhoa = (bool)reader["BiKhoa"],
                                SoLanSaiMatKhau = (int)reader["SoLanSaiMatKhau"]
                            };
                        }
                    }
                }
            }
            return null;
        }
        catch (SqlException)
        {
            throw new IAMException("Database error");
        }
    }

    public async Task UpdateFailedLoginAttemptsAsync(int userId, int attempts)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                bool shouldLock = attempts >= MAX_FAILED_ATTEMPTS;
                
                using (var command = new SqlCommand(
                    "UPDATE NguoiDung SET SoLanSaiMatKhau = @Attempts, BiKhoa = @BiKhoa WHERE Id = @Id", 
                    connection))
                {
                    command.Parameters.AddWithValue("@Attempts", attempts);
                    command.Parameters.AddWithValue("@BiKhoa", shouldLock);
                    command.Parameters.AddWithValue("@Id", userId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (SqlException)
        {
            throw new IAMException("Database error");
        }
    }

    public async Task LockAccountAsync(int userId)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("UPDATE NguoiDung SET BiKhoa = 1 WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", userId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (SqlException)
        {
            throw new IAMException("Database error");
        }
    }

    public async Task ResetFailedLoginAttemptsAsync(int userId)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("UPDATE NguoiDung SET SoLanSaiMatKhau = 0 WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@Id", userId);
                    await command.ExecuteNonQueryAsync();
                }
            }
        }
        catch (SqlException)
        {
            throw new IAMException("Database error");
        }
    }
}
