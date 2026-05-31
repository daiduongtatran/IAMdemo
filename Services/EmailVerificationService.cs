using System.Collections.Concurrent;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace IAMDemoProject.Services;

public interface IEmailVerificationService
{
    Task SendGoogleLoginCodeAsync(string email);
    bool VerifyCode(string email, string code);
}

public class EmailVerificationService : IEmailVerificationService
{
    private static readonly ConcurrentDictionary<string, (string Code, DateTime ExpiresAt)> Codes = new();
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(
        IConfiguration configuration,
        ILogger<EmailVerificationService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SendGoogleLoginCodeAsync(string email)
    {
        var code = Random.Shared.Next(100000, 999999).ToString("D6");
        var expiresAt = DateTime.UtcNow.AddMinutes(10);
        Codes[email.ToLowerInvariant()] = (code, expiresAt);

        var sent = await TrySendEmailAsync(email, code);
        if (!sent)
        {
            _logger.LogWarning(
                "[DEV] Mã xác minh Google cho {Email}: {Code} (hết hạn sau 10 phút).",
                email, code);
        }
    }

    public bool VerifyCode(string email, string code)
    {
        if (!Codes.TryGetValue(email.ToLowerInvariant(), out var entry))
            return false;

        if (DateTime.UtcNow > entry.ExpiresAt)
        {
            Codes.TryRemove(email.ToLowerInvariant(), out _);
            return false;
        }

        var valid = string.Equals(entry.Code, code.Trim(), StringComparison.Ordinal);
        if (valid)
            Codes.TryRemove(email.ToLowerInvariant(), out _);

        return valid;
    }

    private async Task<bool> TrySendEmailAsync(string toEmail, string code)
    {
        var host = _configuration["Email:SmtpHost"];
        var from = _configuration["Email:FromAddress"];
        var username = _configuration["Email:Username"];
        var password = _configuration["Email:Password"];

        if (string.IsNullOrWhiteSpace(host) || string.IsNullOrWhiteSpace(from) ||
            string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        var port = _configuration.GetValue("Email:SmtpPort", 587);
        var fromName = _configuration["Email:FromName"] ?? "IAM Demo";

        try
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, from));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Mã xác minh đăng nhập Google - IAM Demo";
            message.Body = new TextPart("plain")
            {
                Text = $"Mã xác minh của bạn là: {code}\n\nMã có hiệu lực trong 10 phút. Không chia sẻ mã này với ai."
            };

            using var client = new SmtpClient();
            await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(username, password);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);

            _logger.LogInformation("Đã gửi mã xác minh Google tới {Email}", toEmail);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Không gửi được email xác minh tới {Email}", toEmail);
            return false;
        }
    }
}
