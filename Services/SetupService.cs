using Microsoft.Data.SqlClient;
using BCrypt.Net;

namespace IAMDemoProject.Services;

public interface ISetupService
{
    Task InitializeSecurityAsync();
}

public class SetupService : ISetupService
{
    private readonly string _connectionString;

    public SetupService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Connection string not found");
    }

    public async Task InitializeSecurityAsync()
    {
        try
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                const string selectQuery = @"
                    SELECT Id, TenDangNhap, MatKhauHash FROM NguoiDung
                    WHERE MatKhauHash IS NOT NULL 
                    AND MatKhauHash NOT LIKE '$2a$*'
                    AND MatKhauHash NOT LIKE '$2b$*'
                    AND MatKhauHash NOT LIKE '$2y$*'";

                var usersToUpdate = new List<(int Id, string Username, string OldHash)>();

                using (var command = new SqlCommand(selectQuery, connection))
                {
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            usersToUpdate.Add((
                                (int)reader["Id"],
                                reader["TenDangNhap"].ToString() ?? string.Empty,
                                reader["MatKhauHash"].ToString() ?? string.Empty
                            ));
                        }
                    }
                }

                foreach (var (id, username, oldHash) in usersToUpdate)
                {
                    if (oldHash == "123" || oldHash == "admin" || oldHash == "user")
                    {
                        string hashedPassword = BCrypt.Net.BCrypt.HashPassword(oldHash, workFactor: 12);
                        using (var command = new SqlCommand("UPDATE NguoiDung SET MatKhauHash = @HashedPassword WHERE Id = @Id", connection))
                        {
                            command.Parameters.AddWithValue("@HashedPassword", hashedPassword);
                            command.Parameters.AddWithValue("@Id", id);
                            await command.ExecuteNonQueryAsync();
                        }
                    }
                }
            }
        }
        catch (SqlException ex)
        {
            throw new Exception("Database error", ex);
        }
    }
}
