namespace SajhaSikshya.Constants;

/// <summary>
/// Account-security tuning values shared between Identity configuration
/// (<see cref="Extensions.ServiceCollectionExtensions.AddApplicationIdentity"/>),
/// session configuration (Program.cs), and future OTP-based flows, so the same
/// numbers aren't hand-typed in more than one place.
/// </summary>
public static class SecurityConstants
{
    /// <summary>Failed sign-in attempts allowed before an account is locked out.</summary>
    public const int MaxLoginAttempts = 5;

    /// <summary>How long an account stays locked out after exceeding <see cref="MaxLoginAttempts"/>.</summary>
    public const int LockoutMinutes = 15;

    /// <summary>
    /// How long a one-time password (email/SMS verification code) stays valid.
    /// Not yet wired to any flow — reserved for the future OTP-based verification feature.
    /// </summary>
    public const int OtpExpirationMinutes = 10;

    /// <summary>Idle time before an authenticated session expires and the user must sign in again.</summary>
    public const int SessionTimeoutMinutes = 30;
}
