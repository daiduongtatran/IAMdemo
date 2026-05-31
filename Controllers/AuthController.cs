using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;
using IAMDemoProject.Models;
using IAMDemoProject.Services;

namespace IAMDemoProject.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAMDemoProject.Services.IAuthenticationService _authenticationService;
    private readonly IUserDatabaseService _userDatabaseService;
    private readonly IEmailVerificationService _emailVerificationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IAMDemoProject.Services.IAuthenticationService authenticationService,
        IUserDatabaseService userDatabaseService,
        IEmailVerificationService emailVerificationService,
        ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
        _userDatabaseService = userDatabaseService;
        _emailVerificationService = emailVerificationService;
        _logger = logger;
    }

    [HttpPost("login")]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        try
        {
            if (request == null || string.IsNullOrWhiteSpace(request.TenDangNhap) || string.IsNullOrWhiteSpace(request.MatKhau))
            {
                return BadRequest(new ErrorResponse
                {
                    Message = "Username and password required",
                    ErrorCode = "INVALID_INPUT"
                });
            }

            var authResponse = await _authenticationService.AuthenticateAsync(request);
            _logger.LogInformation($"Login success: {request.TenDangNhap}");
            return Ok(authResponse);
        }
        catch (UserNotFoundException ex)
        {
            return Unauthorized(new ErrorResponse { Message = ex.Message, ErrorCode = "USER_NOT_FOUND" });
        }
        catch (AccountLockedException ex)
        {
            return Unauthorized(new ErrorResponse { Message = ex.Message, ErrorCode = "ACCOUNT_LOCKED" });
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(new ErrorResponse { Message = ex.Message, ErrorCode = "AUTH_FAILED" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return StatusCode(500, new ErrorResponse { Message = "System error", ErrorCode = "ERROR" });
        }
    }

    [HttpGet("google-login")]
    public IActionResult GoogleLogin()
    {
        var callbackUrl = Url.Action(
            nameof(GoogleCallback),
            "Auth",
            values: null,
            protocol: Request.Scheme,
            host: Request.Host.Value);

        var properties = new AuthenticationProperties
        {
            RedirectUri = callbackUrl
        };

        return Challenge(properties, GoogleDefaults.AuthenticationScheme);
    }

    [HttpGet("google-callback")]
    public async Task<IActionResult> GoogleCallback()
    {
        var result = await HttpContext.AuthenticateAsync("ExternalCookie");

        if (!result.Succeeded || result.Principal == null)
        {
            return Redirect("/?googleError=1");
        }

        var email =
            result.Principal.FindFirst(ClaimTypes.Email)?.Value
            ?? result.Principal.FindFirst("email")?.Value;

        if (string.IsNullOrWhiteSpace(email))
        {
            await HttpContext.SignOutAsync("ExternalCookie");
            return Redirect("/?googleError=2");
        }

        var user = await _userDatabaseService.GetUserByUsernameAsync(email);

        if (user == null)
        {
            var randomPassword = Guid.NewGuid().ToString("N");
            user = await _userDatabaseService.CreateUserAsync(email, randomPassword, "User");
        }
        else
        {
            await _userDatabaseService.UpdateUserRoleAsync(user.Id, "User");
            if (user.BiKhoa)
            {
                await HttpContext.SignOutAsync("ExternalCookie");
                return Redirect("/?googleError=3");
            }
        }

        var tempToken = _authenticationService.GenerateGoogleEmailVerificationToken(user, email);
        await _emailVerificationService.SendGoogleLoginCodeAsync(email);
        await HttpContext.SignOutAsync("ExternalCookie");

        var redirectUrl =
            $"/?googleEmailVerify=1" +
            $"&tempToken={Uri.EscapeDataString(tempToken)}" +
            $"&email={Uri.EscapeDataString(email)}";

        _logger.LogInformation("Google SSO: đã gửi mã xác minh email cho {Email}", email);
        return Redirect(redirectUrl);
    }

    [HttpPost("verify-google-email")]
    public async Task<IActionResult> VerifyGoogleEmail([FromBody] VerifyGoogleEmailRequest request)
    {
        try
        {
            var result = await _authenticationService.VerifyGoogleEmailAsync(request);
            return Ok(result);
        }
        catch (AuthenticationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi verify Google email");
            return StatusCode(500, new { message = "System error" });
        }
    }

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { Message = "IAM system running", IsHealthy = true, Timestamp = DateTime.UtcNow });
    }

    [HttpPost("verify-mfa")]
    public async Task<IActionResult> VerifyMfa([FromBody] VerifyMfaRequest request)
    {
        try
        {
            var result = await _authenticationService.VerifyMfaAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}
