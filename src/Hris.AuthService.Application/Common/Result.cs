namespace Hris.AuthService.Application.Common;

public sealed record Result<T>(bool IsSuccess, T? Value, string? Error, int StatusCode)
{
    public static Result<T> Ok(T value) => new(true, value, null, 200);
    public static Result<T> Fail(string error, int statusCode) => new(false, default, error, statusCode);
}
