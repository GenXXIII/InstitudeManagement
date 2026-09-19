using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using InstituteManagement.Domain.Entities;
using kh.gov.nbc.bakong_khqr;
using kh.gov.nbc.bakong_khqr.model;
using Microsoft.Extensions.Configuration;

namespace InstituteManagement.Infrastructure.Services.Finance;

public sealed record BakongQrResult(string Payload, string Md5, DateTime GeneratedAtUtc, DateTime ExpiresAtUtc);
public sealed record BakongVerificationResult(string TransactionHash, string FromAccountId, DateTime PaidAtUtc);

public sealed class BakongPaymentGateway(HttpClient client, IConfiguration configuration)
{
    private const int DynamicQrLifetimeMinutes = 10;

    public bool IsConfigured(FinanceSettings settings) =>
        !string.IsNullOrWhiteSpace(Token)
        && !string.IsNullOrWhiteSpace(settings.BakongAccountId)
        && !string.IsNullOrWhiteSpace(settings.BakongAccountInformation)
        && !string.IsNullOrWhiteSpace(settings.BakongAcquiringBank)
        && !string.IsNullOrWhiteSpace(settings.BakongMerchantName)
        && !string.IsNullOrWhiteSpace(settings.BakongMerchantCity);

    public BakongQrResult Generate(FinancialAccount account, decimal amount, FinanceSettings settings)
    {
        if (!settings.BakongEnabled) throw new InvalidOperationException("Bakong KHQR is disabled in Settings.");
        if (!IsConfigured(settings))
            throw new InvalidOperationException("Complete the Bakong receiver fields in Settings and configure BAKONG_API_TOKEN on the API server.");
        if (settings.Currency == "KHR" && amount != decimal.Truncate(amount))
            throw new ArgumentException("Bakong KHR declarations must use a whole-number amount.");

        var generatedAtUtc = DateTime.UtcNow;
        var qrExpiresAtUtc = new[]
        {
            generatedAtUtc.AddMinutes(DynamicQrLifetimeMinutes),
            account.ExpiresAtUtc ?? generatedAtUtc.AddMinutes(DynamicQrLifetimeMinutes)
        }.Min();
        if (qrExpiresAtUtc <= generatedAtUtc) throw new ArgumentException("The payment declaration has expired.");
        var response = BakongKHQR.GenerateIndividual(new IndividualInfo
        {
            BakongAccountID = settings.BakongAccountId,
            AccountInformation = settings.BakongAccountInformation,
            AcquiringBank = settings.BakongAcquiringBank,
            MerchantName = settings.BakongMerchantName,
            MerchantCity = settings.BakongMerchantCity,
            Currency = settings.Currency == "KHR" ? KHQRCurrency.KHR : KHQRCurrency.USD,
            Amount = decimal.ToDouble(amount),
            BillNumber = account.FinancialAccountCode,
            StoreLabel = "INK Finance",
            TerminalLabel = account.Student!.StudentCode,
            PurposeOfTransaction = account.Title,
            ExpirationTimestamp = new DateTimeOffset(qrExpiresAtUtc).ToUnixTimeMilliseconds(),
            MerchantCategoryCode = "8220"
        });
        if (response.Status.Code != 0 || response.Data is null || string.IsNullOrWhiteSpace(response.Data.QR) || string.IsNullOrWhiteSpace(response.Data.MD5))
            throw new InvalidOperationException($"Bakong KHQR generation failed: {response.Status.Message}");
        return new(response.Data.QR, response.Data.MD5, generatedAtUtc, qrExpiresAtUtc);
    }

    public async Task<BakongVerificationResult> VerifyAsync(
        FinancialAccount account,
        decimal expectedAmount,
        FinanceSettings settings,
        CancellationToken cancellationToken)
    {
        if (!IsConfigured(settings))
            throw new InvalidOperationException("Bakong is not fully configured on the API server.");
        if (string.IsNullOrWhiteSpace(account.BakongMd5)) throw new InvalidOperationException("This declaration does not have a Bakong MD5 reference.");
        if (account.QrExpiresAtUtc <= DateTime.UtcNow) throw new ArgumentException("This Bakong QR has expired. Ask Finance to regenerate it.");

        using var request = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl(settings)}/v1/check_transaction_by_md5");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Token);
        request.Content = JsonContent.Create(new { md5 = account.BakongMd5 });
        using var response = await client.SendAsync(request, cancellationToken);
        if ((int)response.StatusCode == 429) throw new InvalidOperationException("Bakong API request limit reached. Retry after the limit resets.");
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Bakong verification is unavailable ({(int)response.StatusCode}).");

        using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var root = document.RootElement;
        var responseCode = root.TryGetProperty("responseCode", out var responseCodeElement) && responseCodeElement.TryGetInt32(out var parsedCode)
            ? parsedCode
            : 1;
        if (responseCode != 0 || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object)
        {
            var message = root.TryGetProperty("responseMessage", out var messageElement) ? messageElement.GetString() : null;
            throw new ArgumentException(message ?? "Bakong has not confirmed this payment yet.");
        }

        var transactionAmount = ReadDecimal(data, "amount");
        var currency = ReadString(data, "currency");
        var recipient = ReadString(data, "toAccountId");
        var transactionHash = ReadString(data, "hash");
        if (transactionAmount != expectedAmount || !currency.Equals(account.Currency, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Bakong confirmed a transaction, but its amount or currency does not match this declaration.");
        if (!recipient.Equals(settings.BakongAccountId, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Bakong confirmed a transaction for a different receiving account.");
        if (string.IsNullOrWhiteSpace(transactionHash)) throw new InvalidOperationException("Bakong returned a payment without a transaction hash.");
        return new(transactionHash, ReadString(data, "fromAccountId"), DateTime.UtcNow);
    }

    private string Token => configuration["Bakong:Token"]?.Trim() ?? string.Empty;

    private static string BaseUrl(FinanceSettings settings) => settings.BakongEnvironment == "Production"
        ? "https://api-bakong.nbc.gov.kh"
        : "https://sit-api-bakong.nbc.gov.kh";

    private static string ReadString(JsonElement data, string name) =>
        data.TryGetProperty(name, out var value) ? value.ToString().Trim() : string.Empty;

    private static decimal ReadDecimal(JsonElement data, string name)
    {
        if (!data.TryGetProperty(name, out var value)) return -1;
        if (value.ValueKind == JsonValueKind.Number && value.TryGetDecimal(out var number)) return number;
        return decimal.TryParse(value.ToString(), NumberStyles.Number, CultureInfo.InvariantCulture, out number) ? number : -1;
    }
}
