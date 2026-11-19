using PublicSafetyHub.Core.Models;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface ITokenService
{
    Task<TokenExchangeResult> ExchangeAsync();
}
