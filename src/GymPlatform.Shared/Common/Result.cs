namespace GymPlatform.Shared.Common;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public string Message { get; }
    public string? WarningMessage { get; }
    public List<string> Errors { get; }

    protected Result(bool isSuccess, string message, string? warningMessage = null, List<string>? errors = null)
    {
        IsSuccess = isSuccess;
        Message = message;
        WarningMessage = warningMessage;
        Errors = errors ?? new List<string>();
    }

    public static Result Success(string message = "Operación exitosa.", string? warningMessage = null)
    {
        return new Result(true, message, warningMessage);
    }

    public static Result Failure(string errorMessage, List<string>? errors = null)
    {
        return new Result(false, errorMessage, null, errors ?? new List<string> { errorMessage });
    }
}

public class Result<T> : Result
{
    public T? Value { get; }

    protected Result(bool isSuccess, T? value, string message, string? warningMessage = null, List<string>? errors = null)
        : base(isSuccess, message, warningMessage, errors)
    {
        Value = value;
    }

    public static Result<T> Success(T value, string message = "Operación exitosa.", string? warningMessage = null)
    {
        return new Result<T>(true, value, message, warningMessage);
    }

    public new static Result<T> Failure(string errorMessage, List<string>? errors = null)
    {
        return new Result<T>(false, default, errorMessage, null, errors ?? new List<string> { errorMessage });
    }
}
