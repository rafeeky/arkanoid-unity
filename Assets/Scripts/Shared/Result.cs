namespace Arkanoid.Shared
{
    public readonly struct Result<T, TError>
    {
        public bool IsOk { get; }
        public T Value { get; }
        public TError Error { get; }

        private Result(bool isOk, T value, TError error)
        {
            IsOk = isOk;
            Value = value;
            Error = error;
        }

        public static Result<T, TError> Ok(T value) => new Result<T, TError>(true, value, default!);
        public static Result<T, TError> Fail(TError error) => new Result<T, TError>(false, default!, error);
    }

    public static class Result
    {
        public static Result<T, string> Ok<T>(T value) => Result<T, string>.Ok(value);
        public static Result<T, string> Fail<T>(string error) => Result<T, string>.Fail(error);
    }
}
