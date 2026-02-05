namespace Cfa.ACHInterbank.Application.Common;

public enum ErrorType
{
    Validation,
    Conflict,
    NotFound,
    Unauthorized,
    Forbidden,
    Unexpected
}

public sealed record ErrorDetail(string Code, string Message, ErrorType Type = ErrorType.Unexpected);

public class Result
{
    public bool IsSuccess { get; }
    public IReadOnlyCollection<ErrorDetail> Errors { get; }

    protected Result(bool isSuccess, IReadOnlyCollection<ErrorDetail>? errors = null)
    {
        IsSuccess = isSuccess;
        Errors = errors ?? [];
    }

    public static Result Success() => new(true);

    public static Result Failure(params ErrorDetail[] errors) => new(false, errors);

    public static Result Failure(string code, string message, ErrorType type = ErrorType.Unexpected)
        => new(false, [new ErrorDetail(code, message, type)]);
}

public sealed class Result<T> : Result
{
    public T? Value { get; }

    private Result(bool isSuccess, T? value, IReadOnlyCollection<ErrorDetail>? errors = null)
        : base(isSuccess, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value) => new(true, value);

    public static Result<T> Failure(params ErrorDetail[] errors) => new(false, default, errors);

    public static Result<T> Failure(string code, string message, ErrorType type = ErrorType.Unexpected)
        => new(false, default, [new ErrorDetail(code, message, type)]);
}
