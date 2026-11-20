using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.Extensions.Primitives;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Base.Auth;
using PublicSafetyHub.Base.Results;

namespace PublicSafetyHub.Host.Middlewares;

public sealed class AuthorizationLoginMiddleware(IAuthTransactionService authTransactions, IAuthOptions options) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        var request = context.Request;

        if (!string.Equals(request.Path.Value?.TrimEnd('/'), "/connect/authorize", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        context.Response.Headers.CacheControl = "no-store";
        context.Response.Headers.Pragma = "no-cache";
        context.Response.Headers.ContentSecurityPolicy = "frame-ancestors 'none'; base-uri 'none'";
        context.Response.Headers.XFrameOptions = "DENY";
        context.Response.Headers.XContentTypeOptions = "nosniff";
        context.Response.Headers["Referrer-Policy"] = "no-referrer";

        var issuerOrigin = new Uri(options.Issuer).GetLeftPart(UriPartial.Authority);
        var requestOrigin = $"{request.Scheme}://{request.Host}";

        if (!request.IsHttps || !string.Equals(requestOrigin, issuerOrigin, StringComparison.Ordinal))
        {
            await RejectAsync(context, ResultHelper.Validation("Invalid authorization request."));
            return;
        }

        if (!HttpMethods.IsPost(request.Method))
        {
            await next(context);
            return;
        }

        var origin = request.Headers.Origin;

        if (origin.Count != 1 || !string.Equals(origin[0], issuerOrigin, StringComparison.Ordinal))
        {
            await RejectAsync(context, ResultHelper.Forbidden("Invalid request origin."));
            return;
        }

        if (!string.Equals(request.ContentType?.Split(';')[0].Trim(), "application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase) ||
            request.QueryString.HasValue || request.ContentLength > 16384)
        {
            await RejectAsync(context, ResultHelper.Validation("Invalid authorization request."));
            return;
        }

        var bodyLimit = context.Features.Get<IHttpMaxRequestBodySizeFeature>();
        if (bodyLimit is { IsReadOnly: false })
        {
            bodyLimit.MaxRequestBodySize = 16384;
        }

        IFormCollection form;

        try
        {
            form = await request.ReadFormAsync();
        }
        catch (Exception exception) when (exception is InvalidDataException or BadHttpRequestException)
        {
            await RejectAsync(context, ResultHelper.Validation("Invalid authorization request."));
            return;
        }

        if (form.Files.Count != 0 || form.Any(field => field.Value.Count != 1) || !HasExpectedFields(form) || !authTransactions.TryConsumeTransaction(form["transaction_id"].ToString(), out var originalRequest))
        {
            await RejectAsync(context, ResultHelper.Validation("Invalid or expired authorization transaction. Start authorization again."));
            return;
        }

        var submission = new AuthorizationLoginSubmission(form["intent"].ToString(), form["email"].ToString(),
            form["password"].ToString(), form["confirm_password"].ToString());
        context.Items[typeof(AuthorizationLoginSubmission)] = submission;
        request.Form = new FormCollection(originalRequest.GetParameters().ToDictionary(parameter => parameter.Key, parameter => (StringValues)parameter.Value, StringComparer.Ordinal));

        try
        {
            await next(context);
        }
        finally
        {
            context.Items.Remove(typeof(AuthorizationLoginSubmission));
        }
    }

    private static bool HasExpectedFields(IFormCollection form)
    {
        string[] expectedFields = form["intent"].ToString() switch
        {
            "login" => ["transaction_id", "intent", "email", "password"],
            "register" => ["transaction_id", "intent", "email", "password", "confirm_password"],
            _ => []
        };

        return expectedFields.Length != 0 && new HashSet<string>(form.Keys, StringComparer.Ordinal).SetEquals(expectedFields);
    }

    private static Task RejectAsync(HttpContext context, ApplicationError error)
    {
        ApplicationResult<NoValueResult> result = error;
        return result.ToActionResult().ExecuteResultAsync(new ActionContext(context, context.GetRouteData(), new ActionDescriptor()));
    }
}
