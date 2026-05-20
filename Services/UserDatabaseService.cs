using Microsoft.Data.SqlClient;
using IAMDemoProject.Models;
using BCrypt.Net;

namespace IAMDemoProject.Services;

public interface IUserDatabaseService
{
    Task<User?> GetUserByUsernameAsync(string tenDangNhap);
    Task UpdateFailedLoginAttemptsAsync(int userId, int attempts);
    Task LockAccountAsync(int userId);
    Task ResetFailedLoginAttemptsAsync(int userId);
    Task<List<User>> GetAllUsersAsync();
    Task<User> CreateUserAsync(string tenDangNhap, string matKhau, string vaiTro);
    Task DeleteUserAsync(int userId);
    Task UpdateUserRoleAsync(int userId, string vaiTro);
    Task UpdateTotpSecretAsync(int userId, string secret);
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
                    "SELECT Id, TenDangNhap, MatKhauHash, VaiTro, BiKhoa, SoLanSaiMatKhau, TotpSecret FROM NguoiDung WHERE TenDangNhap = @TenDangNhap", 
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
                                SoLanSaiMatKhau = (int)reader["SoLanSaiMatKhau"],
                                TotpSecret = reader["TotpSecret"] != DBNull.Value ? reader["TotpSecret"].ToString() : null
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

    public async Task<List<User>> GetAllUsersAsync()
    {
        try
        {
            var users = new List<User>();
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(
                    "SELECT Id, TenDangNhap, MatKhauHash, VaiTro, BiKhoa, SoLanSaiMatKhau, TotpSecret FROM NguoiDung ORDER BY Id", 
                    connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            users.Add(new User
                            {
                                Id = (int)reader["Id"],
                                TenDangNhap = reader["TenDangNhap"].ToString() ?? string.Empty,
                                MatKhauHash = reader["MatKhauHash"].ToString() ?? string.Empty,
                                VaiTro = reader["VaiTro"].ToString() ?? "User",
                                BiKhoa = (bool)reader["BiKhoa"],
                                SoLanSaiMatKhau = (int)reader["SoLanSaiMatKhau"],
                                TotpSecret = reader["TotpSecret"] != DBNull.Value ? reader["TotpSecret"].ToString() : null
                            });
                        }
                    }
                }
            }
            return users;
        }
        catch (SqlException)
        {
            throw new IAMException("Database error");
        }
    }

    public async Task<User> CreateUserAsync(string tenDangNhap, string matKhau, string vaiTro)
    {
        try
        {
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(matKhau, workFactor: 12);
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand(
                    "INSERT INTO NguoiDung (TenDangNhap, MatKhauHash, VaiTro, BiKhoa, SoLanSaiMatKhau) VALUES (@TenDangNhap, @MatKhauHash, @VaiTro, 0, 0); SELECT SCOPE_IDENTITY();",
                    connection))
                {
                    command.Parameters.AddWithValue("@TenDangNhap", tenDangNhap);
                    command.Parameters.AddWithValue("@MatKhauHash", hashedPassword);
                    command.Parameters.AddWithValue("@VaiTro", vaiTro);
                    var id = (decimal)await command.ExecuteScalarAsync();
                    
                    return new User
                    {
                        Id = (int)id,
                        TenDangNhap = tenDangNhap,
                        MatKhauHash = hashedPassword,
                        VaiTro = vaiTro,
                        BiKhoa = false,
                        SoLanSaiMatKhau = 0,
                        TotpSecret = null
                    };
                }
            }
        }
        catch (SqlException)
        {
            throw new IAMException("Database error");
        }
    }

    public async Task DeleteUserAsync(int userId)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("DELETE FROM NguoiDung WHERE Id = @Id", connection))
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

    public async Task UpdateUserRoleAsync(int userId, string vaiTro)
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (var command = new SqlCommand("UPDATE NguoiDung SET VaiTro = @VaiTro WHERE Id = @Id", connection))
                {
                    command.Parameters.AddWithValue("@VaiTro", vaiTro);
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

    public async Task UpdateTotpSecretAsync(int userId, string secret)
    {
        using (var connection = new Microsoft.Data.SqlClient.SqlConnection(_connectionString))
        {
            await connection.OpenAsync();
            using (var command = new Microsoft.Data.SqlClient.SqlCommand("UPDATE NguoiDung SET TotpSecret = @TotpSecret WHERE Id = @Id", connection))
            {
                command.Parameters.AddWithValue("@TotpSecret", (object?)secret ?? DBNull.Value);
                command.Parameters.AddWithValue("@Id", userId);
                await command.ExecuteNonQueryAsync();
            }
        }
    }
}