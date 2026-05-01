namespace Minify.Communication.Responses;

public class ResultJson
{
    public bool IsSuccess { get; }
    public List<string> Errors { get; }

    protected ResultJson(bool isSuccess, List<string> errors)
    {
        IsSuccess = isSuccess;
        Errors = errors;
    }

    public static ResultJson Success()
        => new ResultJson(true, []);

    public static ResultJson Failure(List<string> errors)
        => new ResultJson(false, errors);
}

public class ResultJson<T> : ResultJson
{
    public T Data { get; }

    protected ResultJson(T data, bool isSuccess, List<string> errors)
        : base(isSuccess, errors)
    {
        Data = data;
    }

    public static ResultJson<T> Success(T data)
        => new ResultJson<T>(data, true, []);

    public new static ResultJson<T> Failure(List<string> errors)
        => new ResultJson<T>(default!, false, errors);
}
