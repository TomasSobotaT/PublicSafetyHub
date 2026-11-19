using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using OpenIddict.Server.AspNetCore;
using PublicSafetyHub.Base.Results;
using PublicSafetyHub.Core.Models;
using PublicSafetyHub.Models.Auth.Responses;

namespace PublicSafetyHub.Host.Extensions;

public static class AuthResultExtension
{
    extension(TokenExchangeResult result)
    {
        public IActionResult ToOpenIddictActionResult()
        {
            return CreateProtocolResponse(result.Principal, result.Error, result.ErrorDescription);
        }
    }

    extension(ApplicationResult<AuthorizationFlowResult> result)
    {
        public IActionResult ToOpenIddictActionResult()
        {
            if (!result.IsSuccess)
            {
                return result.ToActionResult();
            }

            var authorization = result.Value;
            if (authorization.Error is not null || authorization.Principal is not null)
            {
                return CreateProtocolResponse(authorization.Principal, authorization.Error, authorization.ErrorDescription);
            }

            ApplicationResult<AuthorizationTransactionResponse> transaction = authorization.Transaction;
            return transaction.ToActionResult();
        }
    }

    private static IActionResult CreateProtocolResponse(ClaimsPrincipal principal, string error, string description)
    {
        const string scheme = OpenIddictServerAspNetCoreDefaults.AuthenticationScheme;
        if (error is null)
        {
            return new SignInResult(scheme, principal);
        }

        return new ForbidResult([scheme], new AuthenticationProperties(new Dictionary<string, string>
        {
            [OpenIddictServerAspNetCoreConstants.Properties.Error] = error,
            [OpenIddictServerAspNetCoreConstants.Properties.ErrorDescription] = description
        }));
    }
}
