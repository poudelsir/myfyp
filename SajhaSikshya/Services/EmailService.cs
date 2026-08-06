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

    public async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
    {
        if (string.IsNullOrWhiteSpace(_settings.SmtpHost))
        {
            _logger.LogWarning(
                "SMTP is not configured; skipping email to {ToEmail} with subject '{Subject}'.",
                toEmail,
                subject);
            return;
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
            Credentials = new NetworkCredential(_settings.Username, _settings.Password),
        };

        try
        {
            await client.SendMailAsync(message);
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "Failed to send email to {ToEmail} with subject '{Subject}'.", toEmail, subject);
            throw;
        }
    }
}
