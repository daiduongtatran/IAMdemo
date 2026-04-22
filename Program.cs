using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Đăng ký các dịch vụ
builder.Services.AddControllers();

// 2. Cấu hình Authentication (JWT)
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("Key_Sieu_Bao_Mat_Nhom_Minh_123456")),
            ValidateIssuer = false,
            ValidateAudience = false,
            // Đây là dòng quan trọng nhất để hệ thống nhận diện Role của bạn
            RoleClaimType = "role" 
        };
    });

// 3. Đăng ký Authorization
builder.Services.AddAuthorization();

var app = builder.Build();

// 4. Cấu hình Middleware (Thứ tự cực kỳ quan trọng)
app.UseRouting();

app.UseAuthentication(); // Phải đặt trước Authorization
app.UseAuthorization();  // Kiểm tra quyền sau khi đã biết danh tính

app.MapControllers();

app.Run();