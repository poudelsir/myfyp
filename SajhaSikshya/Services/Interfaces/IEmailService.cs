namespace SajhaSikshya.Services.Interfaces;

/// <summary>
/// Abstraction over outbound transactional email (account confirmation, password
/// reset, notifications). Controllers and other services depend on this interface
/// so the SMTP implementation can be swapped or mocked without touching callers.
/// </summary>
public interface IEmailService
{
    /// <summary>
    /// Sends a transactional email. Never throws — a delivery failure (SMTP unreachable,
    /// rejected, etc.) is logged and reported back as <c>false</c> rather than propagated,
    /// since every caller sends email as a side effect of an outcome already decided
    /// (e.g. a password-reset request's response must look identical whether or not the
    /// account exists, and must not turn into a 500 if the mail server is briefly down).
    /// </summary>
    Task<bool> SendEmailAsync(string toEmail, string subject, string htmlBody);
}
