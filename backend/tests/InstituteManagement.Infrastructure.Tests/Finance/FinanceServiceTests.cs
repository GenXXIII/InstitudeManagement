using InstituteManagement.Application.Features.Finance;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Administration;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Finance;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

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
        var payment = await DeclareAsync(service, student.Id);

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
        var payment = await DeclareAsync(service, student.Id);

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
        var payment = await DeclareAsync(service, student.Id);

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

    [Fact]
    public async Task Partial_payment_updates_balance_and_finance_history()
    {
        await using var db = CreateContext();
        AddSettings(db, "2026\u20132027", "Semester 1");
        var department = new Department { DepartmentCode = "IT", Name = "Information Technology" };
        var student = new Student { StudentCode = "STU-PART", PublicId = "INK-00004", FullName = "Partial Student", DepartmentId = department.Id, YearLevel = 1, Shift = "Morning" };
        var enrollment = new StudentEnrollment { EnrollmentCode = "STU-PART-ESTU-1", StudentId = student.Id, DepartmentId = department.Id, YearLevel = 1, Shift = "Morning", AcademicYear = "2026\u20132027", Semester = "Semester 1", Status = "Active" };
        db.AddRange(department, student, enrollment);
        await db.SaveChangesAsync();
        var service = Service(db);
        var account = await DeclareAsync(service, student.Id);

        var partial = await service.RecordPaymentAsync(
            account.Id,
            new RecordFinancePaymentDto(100m, "ABA", "ABA-REF-1", DateTime.UtcNow),
            CancellationToken.None);

        Assert.Equal("Partial", partial.Status);
        Assert.Equal(100m, partial.TotalPaid);
        Assert.Equal(650m, partial.Balance);
        Assert.Equal("P-00001", Assert.Single(partial.Payments).PaymentCode);
        var history = Assert.Single(db.AuditLogs.Where(item => item.Type == "Finance" && item.Action == "Payment recorded"));
        Assert.Contains("\"oldFinancialState\":\"Pending\"", history.Details);
        Assert.Contains("\"newFinancialState\":\"Partial\"", history.Details);
    }

    [Fact]
    public async Task Payment_correction_refund_and_discount_recalculate_current_state()
    {
        await using var db = CreateContext();
        AddSettings(db, "2026\u20132027", "Semester 1");
        var department = new Department { DepartmentCode = "IT", Name = "Information Technology" };
        var student = new Student { StudentCode = "STU-EDIT", PublicId = "INK-00005", FullName = "Edited Payment Student", DepartmentId = department.Id, YearLevel = 1, Shift = "Afternoon" };
        var enrollment = new StudentEnrollment { EnrollmentCode = "STU-EDIT-ESTU-1", StudentId = student.Id, DepartmentId = department.Id, YearLevel = 1, Shift = "Afternoon", AcademicYear = "2026\u20132027", Semester = "Semester 1", Status = "Active" };
        db.AddRange(department, student, enrollment);
        await db.SaveChangesAsync();
        var service = Service(db);
        var account = await DeclareAsync(service, student.Id);
        var partial = await service.RecordPaymentAsync(account.Id, new RecordFinancePaymentDto(100m, "Cash", "Receipt 1", DateTime.UtcNow), CancellationToken.None);
        var transaction = Assert.Single(partial.Payments);

        var corrected = await service.UpdatePaymentAsync(account.Id, transaction.Id, new UpdateFinancePaymentDto(120m, "ABA", "ABA-REF-2", DateTime.UtcNow), CancellationToken.None);
        Assert.Equal(120m, corrected.TotalPaid);
        Assert.Equal(630m, corrected.Balance);
        var refunded = await service.SetPaymentStatusAsync(account.Id, transaction.Id, new FinancePaymentStatusDto("Refunded"), CancellationToken.None);
        Assert.Equal("Refunded", refunded.Status);
        Assert.Equal(0m, refunded.TotalPaid);
        Assert.Equal(750m, refunded.Balance);
        var discounted = await service.AdjustAsync(account.Id, new FinancialAdjustmentDto(-50m, "Scholarship"), CancellationToken.None);
        Assert.Equal(700m, discounted.TotalDue);
        Assert.Equal(700m, discounted.Balance);
        Assert.Contains(db.AuditLogs, item => item.Type == "Finance" && item.Action == "Payment updated" && item.Details.Contains("\"oldAmount\":100"));
        Assert.Contains(db.AuditLogs, item => item.Type == "Finance" && item.Action == "Payment refunded");
        Assert.Contains(db.AuditLogs, item => item.Type == "Finance" && item.Action == "Financial adjustment updated" && item.Details.Contains("Scholarship"));
    }

    [Fact]
    public async Task Student_sees_only_declared_cards_and_year_payment_covers_semester_two()
    {
        await using var db = CreateContext();
        AddSettings(db, "2026\u20132027", "Semester 1");
        var department = new Department { DepartmentCode = "IT", Name = "Information Technology" };
        var student = new Student { StudentCode = "STU-YEAR", PublicId = "INK-00006", FullName = "Annual Student", DepartmentId = department.Id, YearLevel = 1, Shift = "Morning" };
        var semesterOne = new StudentEnrollment { EnrollmentCode = "STU-YEAR-ESTU-1", StudentId = student.Id, DepartmentId = department.Id, YearLevel = 1, Shift = "Morning", AcademicYear = "2026\u20132027", Semester = "Semester 1", Status = "Active" };
        db.AddRange(department, student, semesterOne);
        await db.SaveChangesAsync();
        var service = Service(db);

        Assert.Empty(await service.GetStudentAsync(student.Id, CancellationToken.None));
        var account = Assert.Single(await service.GetAsync(null, null, null, null, CancellationToken.None));
        var declaration = await service.DeclareAsync(account.Id, Declaration("Year"), CancellationToken.None);
        Assert.Equal("Year", declaration.PaymentPlan);
        Assert.Single(await service.GetStudentAsync(student.Id, CancellationToken.None));
        await service.RecordPaymentAsync(account.Id, new RecordFinancePaymentDto(750m, "ABA", "YEAR-1", DateTime.UtcNow), CancellationToken.None);

        var semesterTwo = new StudentEnrollment { EnrollmentCode = semesterOne.EnrollmentCode, StudentId = student.Id, DepartmentId = department.Id, YearLevel = 1, Shift = "Morning", AcademicYear = semesterOne.AcademicYear, Semester = "Semester 2", Status = "Active" };
        db.StudentEnrollments.Add(semesterTwo);
        await db.SaveChangesAsync();
        var settings = new FinanceSettingsReader(db);
        var gate = new SemesterPaymentGate(db, new FinancialAccountSynchronizer(db, settings), settings);
        var result = await gate.EvaluateAsync(semesterTwo.AcademicYear, semesterTwo.Semester, CancellationToken.None);

        Assert.Contains(student.Id, result.PaidStudentIds);
        Assert.Equal(0, result.HeldStudents);
    }

    private static async Task<StudentPaymentDto> DeclareAsync(FinanceService service, Guid studentId)
    {
        var account = Assert.Single(await service.GetAsync(null, null, null, null, CancellationToken.None));
        await service.DeclareAsync(account.Id, Declaration("Semester"), CancellationToken.None);
        return Assert.Single(await service.GetStudentAsync(studentId, CancellationToken.None));
    }

    private static FinanceDeclarationDto Declaration(string plan) => new(
        plan == "Year" ? "Annual tuition payment" : "Semester tuition payment",
        plan,
        750m,
        DateOnly.FromDateTime(DateTime.UtcNow).AddDays(3),
        DateTime.UtcNow.AddDays(5));

    private static FinanceService Service(InstituteDbContext db)
    {
        var settings = new FinanceSettingsReader(db);
        var synchronizer = new FinancialAccountSynchronizer(db, settings);
        return new FinanceService(
            db,
            new InstituteCache(),
            synchronizer,
            new FinancialProgression(db, new ActivePeriodLedgerCreator(db)),
            settings,
            new BakongPaymentGateway(new HttpClient(), new ConfigurationBuilder().Build()));
    }

    private static void AddSettings(InstituteDbContext db, string academicYear, string semester)
    {
        db.SystemSettings.AddRange(
            Setting("academic-year", "currentYear", academicYear),
            Setting("semester", "currentTerm", semester),
            Setting("semester", "startsOn", semester == "Semester 1" ? "2026-08-01" : "2027-02-01"),
            Setting("finance", "semesterPrice", "750.00"),
            Setting("finance", "otherFee", "0.00"),
            Setting("finance", "currency", "USD"),
            Setting("finance", "paymentDueDays", "14"),
            Setting("finance", "paymentMethods", "Cash,ABA,ACLEDA,Wing,Bank Transfer,Other"),
            Setting("finance", "allowPartialPayments", "true"),
            Setting("finance", "allowOverpayment", "false"),
            Setting("finance", "maximumAdjustmentAmount", "1000000"),
            Setting("finance", "requirePaidForAdvancement", "true"));
    }

    private static SystemSetting Setting(string section, string key, string value) =>
        new() { Section = section, Key = key, Value = value };

    private static InstituteDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}
