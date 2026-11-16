namespace PublicSafetyHub.Base.Results;

public class ApplicationResult<T>
{
    private readonly T value;

    private ApplicationResult(T value)
    {
        this.value = value;
    }

    private ApplicationResult(ApplicationError error)
    {
        Error = error;
    }

    public bool IsSuccess => Error is null;

    public ApplicationError Error { get; }

    public T Value => IsSuccess ? value : throw new InvalidOperationException("A failed result does not contain a value.");

    public static implicit operator ApplicationResult<T>(T value) => new(value);

    public static implicit operator ApplicationResult<T>(ApplicationError error) => new(error);
}
