namespace PublicSafetyHub.Base.Results;

public static class ResultHelper
{
    public static ApplicationError Validation(string message) => new(ErrorType.Validation, message);

    public static ApplicationError Unauthorized(string message) => new(ErrorType.Unauthorized, message);

    public static ApplicationError Forbidden(string message) => new(ErrorType.Forbidden, message);

    public static ApplicationError NotFound(string message) => new(ErrorType.NotFound, message);

    public static ApplicationError Unavailable(string message) => new(ErrorType.Unavailable, message);

}
