namespace SajhaSikshya.Services.Interfaces;

/// <summary>A single queued outbound email — the unit <see cref="IEmailQueue"/> accepts and <see cref="Email.EmailOutboxWorker"/> eventually sends.</summary>
public record QueuedEmail(string ToEmail, string Subject, string HtmlBody);

/// <summary>
/// Fire-and-forget entry point for outbound email. <see cref="EnqueueAsync"/> returns as
/// soon as the message is buffered — it never talks to an SMTP server itself, so a
/// controller action calling it never blocks on (or fails because of) a slow or
/// unreachable mail provider. <see cref="Email.EmailOutboxWorker"/> is the only consumer,
/// draining the queue in the background with its own retry policy.
/// </summary>
public interface IEmailQueue
{
    ValueTask EnqueueAsync(QueuedEmail email);
}
