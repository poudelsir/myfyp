using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using SajhaSikshya.Configurations;
using SajhaSikshya.Services.Interfaces;

namespace SajhaSikshya.Services;

/// <summary>
/// SMTP-based implementation of <see cref="IEmailService"/> using the built-in
/// <see cref="SmtpClient"/>. Settings are bound from configuration via
/// <see cref="EmailSettings"/> rather than hard-coded, so credentials never live in code.
/// </summary>
public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            _logger.LogWarning(
                "SMTP is not configured (EmailSettings:SmtpHost is empty); skipping email with subject '{Subject}'. " +
                "See appsettings.Development.json / user-secrets to configure a provider or local dev mail catcher.",
                subject);
            return false;
        }

        using var message = new MailMessage
        {
            From = new MailAddress(_settings.SenderEmail, _settings.SenderName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
        {
            EnableSsl = _settings.EnableSsl,
            Credentials = string.IsNullOrEmpty(_settings.Username) ? null : new NetworkCredential(_settings.Username, _settings.Password),
        };

        try
        {
            await client.SendMailAsync(message);
            return true;
        }
        catch (SmtpException ex)
        {
            // Swallowed, not rethrown: every current and future caller sends email as a
            // side effect of an already-decided outcome (e.g. Forgot Password's "check
            // your email" response is shown regardless of whether the account exists —
            // see AccountController's remarks). A transient SMTP failure must not turn
            // that into a 500, and must never surface details that could help an
            // attacker distinguish "account exists but email failed" from "no account".
            // Never log toEmail/subject content here beyond what's already safe above —
            // this exception is the provider's own connection/protocol error, not user data.
            _logger.LogError(ex, "Failed to send email (subject '{Subject}').", subject);
            return false;
        }
    }
}
