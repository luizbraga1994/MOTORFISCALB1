namespace MOTORFISCALSAPB1.Shared.Results;

/// <summary>
/// Result pattern usado em toda a solução para evitar exceções para fluxo de controle.
/// </summary>
public class Result
{
    public bool IsSuccess { get; }
    public string? Error { get; }
    public string? ErrorCode { get; }

    protected Result(bool isSuccess, string? error, string? errorCode)
    {
        IsSuccess = isSuccess;
        Error = error;
        ErrorCode = errorCode;
    }

    public static Result Success() => new(true, null, null);
    public static Result Failure(string error, string? code = null) => new(false, error, code);

    public static Result<T> Success<T>(T value) => Result<T>.Ok(value);
    public static Result<T> Failure<T>(string error, string? code = null) => Result<T>.Fail(error, code);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, string? error, string? errorCode)
        : base(isSuccess, error, errorCode)
    {
        Value = value;
    }

    public static Result<T> Ok(T value) => new(true, value, null, null);
    public static new Result<T> Fail(string error, string? code = null) => new(false, default, error, code);
}
