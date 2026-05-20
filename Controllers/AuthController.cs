using Microsoft.AspNetCore.Mvc;
using IAMDemoProject.Models;
using IAMDemoProject.Services;

namespace IAMDemoProject.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IAuthenticationService authenticationService, ILogger<AuthController> logger)
    {
        _authenticationService = authenticationService;
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

    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        return Ok(new { Message = "IAM system running", IsHealthy = true, Timestamp = DateTime.UtcNow });
    }
    // THÊM API NÀY ĐỂ NHẬN 6 SỐ OTP TỪ FRONTEND
    [HttpPost("verify-mfa")]
    public async Task<IActionResult> VerifyMfa([FromBody] IAMDemoProject.Services.VerifyMfaRequest request)
    {
        try
        {
            var result = await _authenticationService.VerifyMfaAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            // Trả về thông báo lỗi nếu mã OTP sai hoặc vé chờ hết hạn
            return BadRequest(new { message = ex.Message });
        }
    }
}

public class ErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
}