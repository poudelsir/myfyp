namespace SajhaSikshya.Services.Interfaces;

/// <summary>
/// Abstraction over outbound transactional email (account confirmation, password
/// reset, notifications). Controllers and other services depend on this interface
/// so the SMTP implementation can be swapped or mocked without touching callers.
/// </summary>
public interface IEmailService
{
    Task SendEmailAsync(string toEmail, string subject, string htmlBody);
}
