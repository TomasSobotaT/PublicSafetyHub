using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace PublicSafetyHub.Base.Results;

public static class ResultExtension
{
    public static IActionResult ToActionResult<T>(this ApplicationResult<T> result)
    {
        if (result.IsSuccess)
        {
            if (typeof(T) == typeof(NoValueResult) || result.Value is null)
            {
                return new OkResult();
            }

            return new OkObjectResult(result.Value);
        }

        var error = result.Error;
        var status = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        var problem = new ProblemDetails { Status = status, Title = error.Message };
        var response = new ObjectResult(problem) { StatusCode = status };
        response.ContentTypes.Add("application/problem+json");

        return response;
    }
}
