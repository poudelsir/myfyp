namespace SajhaSikshya.Utilities;

/// <summary>
/// Small set of argument guard clauses used at service/repository boundaries to fail
/// fast with a clear exception message instead of a downstream NullReferenceException.
/// </summary>
public static class Guard
{
    public static string AgainstNullOrWhiteSpace(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"'{parameterName}' cannot be null or whitespace.", parameterName);
        }

        return value;
    }

    public static T AgainstNull<T>(T? value, string parameterName) where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }

        return value;
    }

    public static int AgainstNegativeOrZero(int value, string parameterName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"'{parameterName}' must be greater than zero.");
        }

        return value;
    }
}
