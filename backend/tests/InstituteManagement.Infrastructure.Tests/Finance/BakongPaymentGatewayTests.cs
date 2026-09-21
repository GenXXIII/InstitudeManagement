using System.Net;
using System.Text;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Services.Finance;
using Microsoft.Extensions.Configuration;

namespace InstituteManagement.Infrastructure.Tests.Finance;

public sealed class BakongPaymentGatewayTests
{
    [Fact]
    public void Generate_creates_dynamic_khqr_with_md5_and_ten_minute_maximum_lifetime()
    {
        var gateway = Gateway(new StubHandler());
        var account = Account();
        var before = DateTime.UtcNow;

        var result = gateway.Generate(account, 25.50m, Settings());

        Assert.StartsWith("000201", result.Payload);
        Assert.Equal(32, result.Md5.Length);
        Assert.InRange(result.GeneratedAtUtc, before, DateTime.UtcNow);
        Assert.InRange(result.ExpiresAtUtc, result.GeneratedAtUtc.AddMinutes(9), result.GeneratedAtUtc.AddMinutes(10));
    }

    [Fact]
    public void Generate_individual_khqr_does_not_require_merchant_or_optional_bank_fields()
    {
        var gateway = Gateway(new StubHandler());
        var personal = Settings() with
        {
            BakongAccountInformation = string.Empty,
            BakongAcquiringBank = string.Empty,
            BakongMerchantCity = string.Empty
        };

        var result = gateway.Generate(Account(), 0.01m, personal);

        Assert.True(gateway.IsConfigured(personal));
        Assert.StartsWith("000201", result.Payload);
        Assert.Equal(32, result.Md5.Length);
    }

    [Fact]
    public async Task Verify_uses_bearer_token_and_returns_matching_bakong_transaction()
    {
        var handler = new StubHandler();
        var gateway = Gateway(handler);
        var account = Account();
        account.BakongMd5 = "0392d8524343ca319a05c54dbdea24e6";
        account.QrExpiresAtUtc = DateTime.UtcNow.AddMinutes(5);

        var result = await gateway.VerifyAsync(account, 25.50m, Settings(), CancellationToken.None);

        Assert.Equal("transaction-hash", result.TransactionHash);
        Assert.Equal("student@devb", result.FromAccountId);
        Assert.Equal("Bearer test-server-token", handler.Authorization);
        Assert.Equal("https://sit-api-bakong.nbc.gov.kh/v1/check_transaction_by_md5", handler.RequestUri);
        Assert.Contains(account.BakongMd5, handler.RequestBody);
    }

    private static BakongPaymentGateway Gateway(HttpMessageHandler handler) => new(
        new HttpClient(handler),
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Bakong:Token"] = "test-server-token" })
            .Build());

    private static FinancialAccount Account() => new()
    {
        FinancialAccountCode = "FIN-00001",
        StudentId = Guid.NewGuid(),
        StudentEnrollmentId = Guid.NewGuid(),
        Student = new Student { StudentCode = "STU-001", FullName = "Test Student" },
        AcademicYear = "2026-2027",
        Semester = "Semester 1",
        Title = "Semester tuition",
        PaymentPlan = "Semester",
        Currency = "USD",
        DeclaredAmount = 25.50m,
        DeclaredAtUtc = DateTime.UtcNow,
        ExpiresAtUtc = DateTime.UtcNow.AddDays(1)
    };

    private static FinanceSettings Settings() => new(
        TuitionFee: 25.50m,
        OtherFee: 0m,
        Currency: "USD",
        PaymentDueDays: 14,
        LatePenaltyPerDay: 0m,
        PaymentMethods: ["Bakong"],
        AllowPartialPayments: true,
        AllowOverpayment: false,
        MaximumAdjustmentAmount: 1000m,
        RequirePaidForAdvancement: true,
        BakongEnabled: true,
        BakongEnvironment: "SIT",
        BakongAccountId: "institute@devb",
        BakongAccountInformation: "85512345678",
        BakongAcquiringBank: "Dev Bank",
        BakongMerchantName: "Institute of New Khmer",
        BakongMerchantCity: "Phnom Penh",
        PaymentProviders: []);

    private sealed class StubHandler : HttpMessageHandler
    {
        public string Authorization { get; private set; } = string.Empty;
        public string RequestUri { get; private set; } = string.Empty;
        public string RequestBody { get; private set; } = string.Empty;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Authorization = request.Headers.Authorization?.ToString() ?? string.Empty;
            RequestUri = request.RequestUri?.ToString() ?? string.Empty;
            RequestBody = request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "responseCode": 0,
                      "responseMessage": "Getting transaction successfully.",
                      "data": {
                        "hash": "transaction-hash",
                        "fromAccountId": "student@devb",
                        "toAccountId": "institute@devb",
                        "currency": "USD",
                        "amount": 25.50,
                        "description": "Semester tuition"
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            };
        }
    }
}
