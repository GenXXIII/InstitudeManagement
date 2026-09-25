using System.Security.Cryptography;
using System.Text.Json;
using InstituteManagement.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;

namespace InstituteManagement.Infrastructure.Services.Finance;

public sealed record MockPaymentQrResult(string Payload, string PublicId, DateTime GeneratedAtUtc, DateTime ExpiresAtUtc);

public sealed record MockPaymentQrClaims(
    Guid FinancialAccountId,
    Guid StudentId,
    string PublicId,
    decimal Amount,
    string Currency,
    string TokenId,
    DateTime ExpiresAtUtc);

public sealed class MockPaymentQrGateway(IDataProtectionProvider protectionProvider)
{
    private const string Prefix = "INK-MOCK-PAY:";
    private const int LifetimeMinutes = 10;
    private readonly IDataProtector protector = protectionProvider.CreateProtector("InstituteManagement.Finance.MockPaymentQr.v1");

    public MockPaymentQrResult Generate(FinancialAccount account, string publicId, decimal amount)
    {
        if (string.IsNullOrWhiteSpace(publicId))
            throw new InvalidOperationException("Assign a Student Public ID before generating a mock payment QR.");

        var generatedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = new[]
        {
            generatedAtUtc.AddMinutes(LifetimeMinutes),
            account.ExpiresAtUtc ?? generatedAtUtc.AddMinutes(LifetimeMinutes)
        }.Min();
        if (expiresAtUtc <= generatedAtUtc) throw new ArgumentException("The payment declaration has expired.");

        var claims = new MockPaymentQrClaims(
            account.Id,
            account.StudentId,
            publicId.Trim(),
            decimal.Round(amount, 2),
            account.Currency,
            Guid.NewGuid().ToString("N"),
            expiresAtUtc);
        var payload = Prefix + protector.Protect(JsonSerializer.Serialize(claims));
        return new(payload, claims.PublicId, generatedAtUtc, expiresAtUtc);
    }

    public MockPaymentQrClaims Validate(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload) || !payload.StartsWith(Prefix, StringComparison.Ordinal))
            throw new ArgumentException("This is not a valid Institute mock payment QR.");

        try
        {
            var json = protector.Unprotect(payload[Prefix.Length..]);
            var claims = JsonSerializer.Deserialize<MockPaymentQrClaims>(json)
                ?? throw new ArgumentException("This mock payment QR is invalid.");
            if (claims.ExpiresAtUtc <= DateTime.UtcNow)
                throw new ArgumentException("This mock payment QR has expired. Ask Finance to generate a new one.");
            return claims;
        }
        catch (CryptographicException)
        {
            throw new ArgumentException("This mock payment QR is invalid or was not issued by this institute.");
        }
        catch (JsonException)
        {
            throw new ArgumentException("This mock payment QR is invalid.");
        }
    }
}
