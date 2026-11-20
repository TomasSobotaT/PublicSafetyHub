using PublicSafetyHub.Base.Auth;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface ITokenService
{
    Task<TokenExchangeResult> ExchangeAsync();
}
