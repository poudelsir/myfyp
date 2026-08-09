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

    /// <summary>
    /// How long a password reset link stays valid, via Identity's built-in
    /// <c>DataProtectionTokenProviderOptions.TokenLifespan</c> (default is 1 day —
    /// shortened here since a reset link is a higher-stakes, short-lived flow than the
    /// default token lifespan was designed for).
    /// </summary>
    public const int PasswordResetTokenLifespanHours = 2;

    /// <summary>
    /// How often Identity's <c>SecurityStampValidator</c> re-checks an authenticated
    /// cookie against the user's current <c>SecurityStamp</c> in the database. Left at
    /// Identity's (very long) default, an already-signed-in user's cookie keeps working
    /// even after an admin suspends the account — this closes that gap: suspending a
    /// user bumps their stamp (<c>UserManager.UpdateSecurityStampAsync</c>), and within
    /// this interval their next request re-validates, fails, and forces a fresh sign-in
    /// (which then correctly fails the existing <c>IsActive</c> login check).
    /// </summary>
    public const int SecurityStampValidationIntervalMinutes = 5;
}
