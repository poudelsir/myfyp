namespace SajhaSikshya.Services;

/// <summary>
/// Outcome of a service-layer write operation (create/update/delete) that can fail
/// for business reasons (validation, uniqueness, referential integrity) without
/// throwing. Controllers inspect <see cref="Succeeded"/> and surface <see cref="Errors"/>
/// as model errors instead of every service needing its own bespoke result tuple.
/// </summary>
public class ServiceResult
{
    public bool Succeeded { get; protected init; }

    public IReadOnlyList<string> Errors { get; protected init; } = Array.Empty<string>();

    public static ServiceResult Success() => new() { Succeeded = true };

    public static ServiceResult Failure(params string[] errors) => new() { Succeeded = false, Errors = errors };
}

/// <summary>Same as <see cref="ServiceResult"/> but carries data back on success (e.g. a new entity's Id).</summary>
public sealed class ServiceResult<T> : ServiceResult
{
    public T? Data { get; private init; }

    public static ServiceResult<T> Success(T data) => new() { Succeeded = true, Data = data };

    public static new ServiceResult<T> Failure(params string[] errors) => new() { Succeeded = false, Errors = errors };
}
