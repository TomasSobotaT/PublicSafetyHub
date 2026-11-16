namespace PublicSafetyHub.Base.Results;

public static class ResultHelper
{
    public static ApplicationError Validation(string message) => new(ErrorType.Validation, message);

    public static ApplicationError Unauthorized(string message) => new(ErrorType.Unauthorized, message);

    public static ApplicationError NotFound(string message) => new(ErrorType.NotFound, message);

}
