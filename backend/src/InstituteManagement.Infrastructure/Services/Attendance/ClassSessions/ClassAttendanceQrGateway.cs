using System.Security.Cryptography;
using System.Text.Json;
using InstituteManagement.Application.Features.Attendance.ClassSessions;
using InstituteManagement.Domain.Entities;
using Microsoft.AspNetCore.DataProtection;

namespace InstituteManagement.Infrastructure.Services.Attendance.ClassSessions;

public sealed record ClassAttendanceQrClaims(
    Guid ClassSessionStartId,
    Guid ScheduleEntryId,
    Guid TeacherId,
    string TokenId,
    DateTime ExpiresAtUtc);

public sealed class ClassAttendanceQrGateway(IDataProtectionProvider protectionProvider)
{
    private const string Prefix = "INK-CLASS-ATTENDANCE:";
    private static readonly TimeSpan Lifetime = TimeSpan.FromSeconds(30);
    private readonly IDataProtector protector = protectionProvider.CreateProtector("InstituteManagement.Attendance.ClassQr.v1");

    public ClassAttendanceQrDto Generate(ClassSessionStart session)
    {
        var generatedAtUtc = DateTime.UtcNow;
        var expiresAtUtc = generatedAtUtc.Add(Lifetime);
        var claims = new ClassAttendanceQrClaims(
            session.Id,
            session.ScheduleEntryId,
            session.TeacherId,
            Guid.NewGuid().ToString("N"),
            expiresAtUtc);
        var payload = Prefix + protector.Protect(JsonSerializer.Serialize(claims));
        return new ClassAttendanceQrDto(payload, generatedAtUtc, expiresAtUtc);
    }

    public ClassAttendanceQrClaims Validate(string? payload)
    {
        if (string.IsNullOrWhiteSpace(payload) || !payload.StartsWith(Prefix, StringComparison.Ordinal))
            throw new ArgumentException("This is not a valid class attendance QR.");

        try
        {
            var json = protector.Unprotect(payload[Prefix.Length..]);
            var claims = JsonSerializer.Deserialize<ClassAttendanceQrClaims>(json)
                ?? throw new ArgumentException("This class attendance QR is invalid.");
            if (claims.ExpiresAtUtc <= DateTime.UtcNow)
                throw new ArgumentException("This attendance QR has expired. Scan the current QR on the Teacher's screen.");
            return claims;
        }
        catch (CryptographicException)
        {
            throw new ArgumentException("This class attendance QR is invalid or was not issued by this institute.");
        }
        catch (JsonException)
        {
            throw new ArgumentException("This class attendance QR is invalid.");
        }
    }
}
