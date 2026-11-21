using PublicSafetyHub.Base.OpenIddictAuth;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface ITokenService
{
    Task<TokenExchangeResult> ExchangeAsync();
}
