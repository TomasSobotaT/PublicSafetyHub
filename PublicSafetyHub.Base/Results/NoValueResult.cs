namespace PublicSafetyHub.Base.Results;

public class NoValueResult
{
    private NoValueResult()
    {
    }

    public static NoValueResult Value { get; } = new();
}
