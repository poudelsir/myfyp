namespace SajhaSikshya.Configurations;

/// <summary>
/// Strongly-typed SMTP settings bound from the "EmailSettings" section of
/// appsettings.json via the Options pattern. Keeping these out of Services
/// means credentials live only in configuration (and, in production, in an
/// environment-specific secret store), never hard-coded in application logic.
/// </summary>
public class EmailSettings
{
    public const string SectionName = "EmailSettings";

    public string SmtpHost { get; set; } = string.Empty;

    public int SmtpPort { get; set; } = 587;

    public string SenderName { get; set; } = "SajhaSikshya";

    public string SenderEmail { get; set; } = string.Empty;

    public string Username { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public bool EnableSsl { get; set; } = true;
}
