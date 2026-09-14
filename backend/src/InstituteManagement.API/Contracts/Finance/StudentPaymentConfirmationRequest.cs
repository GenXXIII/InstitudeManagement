using InstituteManagement.Application.Features.Finance;

namespace InstituteManagement.API.Contracts.Finance;

public sealed record StudentPaymentConfirmationRequest(string? QrPayload)
{
    public StudentPaymentConfirmationDto ToDto() => new(QrPayload);
}
