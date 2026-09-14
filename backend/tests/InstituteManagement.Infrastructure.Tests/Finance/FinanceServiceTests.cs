using InstituteManagement.Application.Features.Finance;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Administration;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Finance;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Tests.Finance;

public sealed class FinanceServiceTests
{
    [Fact]
    public async Task Student_qr_scan_releases_previous_semester_payment_hold()
    {
        await using var db = CreateContext();
        AddSettings(db, "2026\u20132027", "Semester 2");
        var department = new Department { DepartmentCode = "IT", Name = "Information Technology" };
        var student = new Student
        {
            StudentCode = "STU-1",
            PublicId = "INK-00001",
            FullName = "Student One",
            DepartmentId = department.Id,
            YearLevel = 1,
            Shift = "Morning"
        };
        var enrollment = new StudentEnrollment
        {
            EnrollmentCode = "STU-1-ESTU-1",
            StudentId = student.Id,
            DepartmentId = department.Id,
            YearLevel = 1,
            Shift = "Morning",
            AcademicYear = "2026\u20132027",
            Semester = "Semester 1",
            Status = "Active"
        };
        db.AddRange(department, student, enrollment);
        await db.SaveChangesAsync();
        var service = Service(db);
        var payment = Assert.Single(await service.GetStudentAsync(student.Id, CancellationToken.None));

        var confirmed = await service.ConfirmAsync(
            student.Id,
            payment.Id,
            new StudentPaymentConfirmationDto(payment.QrPayload),
            CancellationToken.None);

        Assert.Equal("Paid", confirmed.Status);
        Assert.Equal("Student QR scan", confirmed.ConfirmationMethod);
        Assert.Contains(db.StudentEnrollments, item =>
            item.StudentId == student.Id
            && item.AcademicYear == "2026\u20132027"
            && item.Semester == "Semester 2"
            && item.Status == "Active");
        Assert.Equal(2, await db.StudentEnrollments.CountAsync());
    }

    [Fact]
    public async Task Qr_confirmation_rejects_another_payload_and_accepts_own_payment_qr()
    {
        await using var db = CreateContext();
        AddSettings(db, "2026\u20132027", "Semester 1");
        var department = new Department { DepartmentCode = "IT", Name = "Information Technology" };
        var student = new Student
        {
            StudentCode = "STU-QR",
            PublicId = "INK-00002",
            FullName = "QR Student",
            DepartmentId = department.Id,
            YearLevel = 1,
            Shift = "Morning"
        };
        var enrollment = new StudentEnrollment
        {
            EnrollmentCode = "STU-QR-ESTU-1",
            StudentId = student.Id,
            DepartmentId = department.Id,
            YearLevel = 1,
            Shift = "Morning",
            AcademicYear = "2026\u20132027",
            Semester = "Semester 1",
            Status = "Active"
        };
        db.AddRange(department, student, enrollment);
        await db.SaveChangesAsync();
        var service = Service(db);
        var payment = Assert.Single(await service.GetStudentAsync(student.Id, CancellationToken.None));

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.ConfirmAsync(
            Guid.NewGuid(),
            payment.Id,
            new StudentPaymentConfirmationDto(payment.QrPayload),
            CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.ConfirmAsync(
            student.Id,
            payment.Id,
            new StudentPaymentConfirmationDto("INK-PAY|wrong"),
            CancellationToken.None));
        var confirmed = await service.ConfirmAsync(
            student.Id,
            payment.Id,
            new StudentPaymentConfirmationDto(payment.QrPayload),
            CancellationToken.None);

        Assert.Equal("Paid", confirmed.Status);
        Assert.Equal("Student QR scan", confirmed.ConfirmationMethod);
    }

    [Fact]
    public async Task Late_paid_confirmation_advances_year_once_and_preserves_old_enrollment()
    {
        await using var db = CreateContext();
        AddSettings(db, "2027\u20132028", "Semester 1");
        var department = new Department { DepartmentCode = "IT", Name = "Information Technology" };
        var student = new Student
        {
            StudentCode = "STU-LATE",
            PublicId = "INK-00003",
            FullName = "Late Payment Student",
            DepartmentId = department.Id,
            YearLevel = 1,
            Shift = "Afternoon"
        };
        var enrollment = new StudentEnrollment
        {
            EnrollmentCode = "STU-LATE-ESTU-1",
            StudentId = student.Id,
            DepartmentId = department.Id,
            YearLevel = 1,
            Shift = "Afternoon",
            AcademicYear = "2026\u20132027",
            Semester = "Semester 2",
            Status = "Active"
        };
        db.AddRange(department, student, enrollment);
        await db.SaveChangesAsync();
        var service = Service(db);
        var payment = Assert.Single(await service.GetStudentAsync(student.Id, CancellationToken.None));

        await service.ConfirmAsync(
            student.Id,
            payment.Id,
            new StudentPaymentConfirmationDto(payment.QrPayload),
            CancellationToken.None);

        Assert.Equal(2, student.YearLevel);
        Assert.Equal(2, await db.StudentEnrollments.CountAsync());
        Assert.Contains(db.StudentEnrollments, item => item.Id == enrollment.Id && item.YearLevel == 1);
        Assert.Contains(db.StudentEnrollments, item =>
            item.Id != enrollment.Id
            && item.AcademicYear == "2027\u20132028"
            && item.Semester == "Semester 1"
            && item.YearLevel == 2);
    }

    private static FinanceService Service(InstituteDbContext db)
    {
        var settings = new FinanceSettingsReader(db);
        var synchronizer = new StudentPaymentSynchronizer(db, settings);
        return new FinanceService(
            db,
            new InstituteCache(),
            synchronizer,
            new StudentPaymentProgression(db, new ActivePeriodLedgerCreator(db)));
    }

    private static void AddSettings(InstituteDbContext db, string academicYear, string semester)
    {
        db.SystemSettings.AddRange(
            Setting("academic-year", "currentYear", academicYear),
            Setting("semester", "currentTerm", semester),
            Setting("semester", "startsOn", semester == "Semester 1" ? "2026-08-01" : "2027-02-01"),
            Setting("finance", "semesterPrice", "750.00"),
            Setting("finance", "currency", "USD"),
            Setting("finance", "paymentDueDays", "14"));
    }

    private static SystemSetting Setting(string section, string key, string value) =>
        new() { Section = section, Key = key, Value = value };

    private static InstituteDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
