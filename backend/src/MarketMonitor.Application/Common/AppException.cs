namespace MarketMonitor.Application.Common;

/// <summary>
/// Thrown for expected business-rule violations (e.g. insufficient cash,
/// duplicate watchlist entry). The global exception handler maps these to a
/// 400-class response, distinguishing them from unexpected 500s.
/// </summary>
public class AppException : Exception
{
    public int StatusCode { get; }

    public AppException(string message, int statusCode = 400) : base(message)
    {
        StatusCode = statusCode;
    }

    public static AppException NotFound(string message) => new(message, 404);
    public static AppException Conflict(string message) => new(message, 409);
}
