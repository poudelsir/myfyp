using SajhaSikshya.Services.Interfaces;

namespace SajhaSikshya.Services.Email;

/// <summary>
/// Drains <see cref="EmailQueue"/> for the lifetime of the app and sends each message
/// through the real <see cref="IEmailService"/>, retrying transient failures with
/// backoff instead of giving up after one attempt the way the old synchronous,
/// in-request send effectively did. <see cref="IEmailService"/> is registered Scoped
/// (its <c>EmailSettings</c> come through <c>IOptions</c>), so a fresh
/// <see cref="IServiceScope"/> is created per message rather than resolving it once at
/// worker startup.
/// </summary>
public class EmailOutboxWorker : BackgroundService
{
    private static readonly TimeSpan[] RetryDelays =
    {
        TimeSpan.FromSeconds(5),
        TimeSpan.FromSeconds(30),
        TimeSpan.FromMinutes(2),
    };

    private readonly EmailQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmailOutboxWorker> _logger;

    public EmailOutboxWorker(EmailQueue queue, IServiceScopeFactory scopeFactory, ILogger<EmailOutboxWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var email in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            await SendWithRetryAsync(email, stoppingToken);
        }
    }

    private async Task SendWithRetryAsync(QueuedEmail email, CancellationToken stoppingToken)
    {
        for (var attempt = 0; attempt <= RetryDelays.Length; attempt++)
        {
            using var scope = _scopeFactory.CreateScope();
            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

            var sent = await emailService.SendEmailAsync(email.ToEmail, email.Subject, email.HtmlBody);
            if (sent)
            {
                if (attempt > 0)
                {
                    _logger.LogInformation("Queued email (subject '{Subject}') sent on retry {Attempt}.", email.Subject, attempt);
                }

                return;
            }

            if (attempt == RetryDelays.Length)
            {
                _logger.LogError(
                    "Queued email (subject '{Subject}') permanently failed after {Attempts} attempts.",
                    email.Subject,
                    attempt + 1);
                return;
            }

            _logger.LogWarning(
                "Queued email (subject '{Subject}') failed on attempt {Attempt}; retrying in {Delay}.",
                email.Subject,
                attempt + 1,
                RetryDelays[attempt]);

            try
            {
                await Task.Delay(RetryDelays[attempt], stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }
}
