using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using IAMDemoProject.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] 
    ?? throw new InvalidOperationException("Jwt:SecretKey not found");
var key = Encoding.UTF8.GetBytes(jwtSecretKey);

builder.Services.AddControllers();

// ĐÃ SỬA: Ép hệ thống 100% sử dụng SQL Server, không dùng RAM (InMemory) nữa
builder.Services.AddScoped<IUserDatabaseService, UserDatabaseService>();
builder.Services.AddScoped<ISetupService, SetupService>();

builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultSignInScheme = "ExternalCookie";
    })
    .AddCookie("ExternalCookie")
    .AddGoogle("Google", options =>
    {
        options.ClientId = builder.Configuration["Authentication:Google:ClientId"]
            ?? throw new InvalidOperationException("Google ClientId not found");

        options.ClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
            ?? throw new InvalidOperationException("Google ClientSecret not found");

        options.CallbackPath = "/signin-google";
        options.SignInScheme = "ExternalCookie";

        options.Scope.Add("email");
        options.Scope.Add("profile");
        options.SaveTokens = true;
    })
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),

            ValidateIssuer = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],

            ValidateAudience = true,
            ValidAudience = builder.Configuration["Jwt:Audience"],

            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,

            RoleClaimType = "role"
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Logging.ClearProviders().AddConsole();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

app.UseRouting();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

using (var scope = app.Services.CreateScope())
{
    try
    {
        var setupService = scope.ServiceProvider.GetRequiredService<ISetupService>();
        await setupService.InitializeSecurityAsync();
    }
    catch (Exception ex)
    {
        app.Logger.LogError($"Error initializing security: {ex.Message}");
    }
}

await app.RunAsync();