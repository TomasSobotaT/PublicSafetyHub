using OpenIddict.Abstractions;

namespace PublicSafetyHub.Application.Services.Interfaces;

public interface IAuthTransactionService
{
    string CreateTransaction(OpenIddictRequest request);

    bool TryConsumeTransaction(string id, out OpenIddictRequest request);
}
