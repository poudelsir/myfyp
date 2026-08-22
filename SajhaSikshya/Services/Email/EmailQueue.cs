using System.Threading.Channels;
using SajhaSikshya.Services.Interfaces;

namespace SajhaSikshya.Services.Email;

/// <summary>
/// In-process outbox backed by an unbounded <see cref="Channel{T}"/> — sufficient for
/// this app's actual volume (password-reset emails only, at the time this was added).
/// A future move to real durability (survive an app restart with mail still in flight)
/// would swap this for a table-backed queue behind the same <see cref="IEmailQueue"/>
/// interface; callers wouldn't need to change.
/// </summary>
public class EmailQueue : IEmailQueue
{
    private readonly Channel<QueuedEmail> _channel = Channel.CreateUnbounded<QueuedEmail>();

    public ChannelReader<QueuedEmail> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(QueuedEmail email) => _channel.Writer.WriteAsync(email);
}
