using Microsoft.AspNetCore.Mvc;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface ITokenService
{
    Task<IActionResult> ExchangeAsync();
}
