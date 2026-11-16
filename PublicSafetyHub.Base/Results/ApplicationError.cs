namespace PublicSafetyHub.Base.Results;

public class ApplicationError(ErrorType type, string message)
{
    public ErrorType Type { get; } = type;

    public string Message { get; } = message;
}
