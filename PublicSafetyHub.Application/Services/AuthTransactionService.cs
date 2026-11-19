using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;
using OpenIddict.Abstractions;
using PublicSafetyHub.Application.Services.Interfaces;
using PublicSafetyHub.Application.ApplicationModels;

namespace PublicSafetyHub.Application.Services;

public sealed class AuthTransactionService : IAuthTransactionService
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
    private const int Capacity = 4096;
    private readonly Lock gate = new();
    private readonly Dictionary<string, AuthTransaction> transactions = new(StringComparer.Ordinal);

    public string CreateTransaction(OpenIddictRequest request)
    {
        var parameters = request.GetParameters().ToDictionary(entry => entry.Key, entry => entry.Value, StringComparer.Ordinal);

        lock (gate)
        {
            var now = DateTime.UtcNow;

            foreach (var key in transactions.Where(entry => entry.Value.ExpiresAt <= now).Select(entry => entry.Key).ToArray())
            {
                transactions.Remove(key);
            }

            if (transactions.Count >= Capacity)
            {
                return null;
            }

            var id = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            transactions.Add(id, new AuthTransaction(parameters, now.Add(Lifetime)));

            return id;
        }
    }

    public bool TryConsumeTransaction(string id, out OpenIddictRequest request)
    {
        request = null;

        if (id is null || id.Length != 43)
        {
            return false;
        }

        lock (gate)
        {
            if (!transactions.Remove(id, out var transaction) || transaction.ExpiresAt <= DateTime.UtcNow)
            {
                return false;
            }

            request = new OpenIddictRequest();
            foreach (var parameter in transaction.Parameters)
            {
                request.SetParameter(parameter.Key, parameter.Value);
            }
            return true;
        }
    }

}
