using System.Net;
using System.Text;
using InstituteManagement.Application.Features.Finance;
using InstituteManagement.Domain.Entities;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Administration;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Finance;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace InstituteManagement.Infrastructure.Tests.Finance;

public sealed class FinanceServiceTests
{
    [Fact]
    public async Task Close_all_payments_waits_for_every_student_then_closes_the_semester_together()
    {
        await using var db = CreateContext();
        AddSettings(db, "2026–2027", "Semester 1");
        var department = new Department { DepartmentCode = "FIN-ALL", Name = "Finance All" };
        var students = new[]
        {
            new Student { StudentCode = "STU-CLOSE-1", FullName = "Close Student One", DepartmentId = department.Id, Department = department, YearLevel = 1, Shift = "Morning" },
            new Student { StudentCode = "STU-CLOSE-2", FullName = "Close Student Two", DepartmentId = department.Id, Department = department, YearLevel = 1, Shift = "Morning" }
        };
        db.Add(department);
        db.Students.AddRange(students);
        db.StudentEnrollments.AddRange(students.Select((student, index) => new StudentEnrollment
        {
            EnrollmentCode = $"ENR-CLOSE-{index + 1}", StudentId = student.Id, Student = student, DepartmentId = department.Id, Department = department,
            YearLevel = 1, Shift = "Morning", AcademicYear = "2026–2027", Semester = "Semester 1", Status = "Active"
        }));
        await db.SaveChangesAsync();
        var service = Service(db);
        await service.DeclareAllAsync(CancellationToken.None);
        var accounts = await service.GetAsync(null, null, null, "All", CancellationToken.None);

        await service.RecordPaymentAsync(accounts[0].Id, new RecordFinancePaymentDto(accounts[0].Balance, "Cash", "ALL-1", DateTime.UtcNow), CancellationToken.None);

        Assert.False((await service.GetClosureReadinessAsync(CancellationToken.None)).CanCloseAll);
        var incomplete = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CloseAllPaymentsAsync(CancellationToken.None));
        Assert.Contains("All current student payments", incomplete.Message);

        await service.RecordPaymentAsync(accounts[1].Id, new RecordFinancePaymentDto(accounts[1].Balance, "Cash", "ALL-2", DateTime.UtcNow), CancellationToken.None);
        var ready = await service.GetClosureReadinessAsync(CancellationToken.None);

        Assert.True(ready.CanCloseAll);
        Assert.Equal(2, ready.OpenPaidAccounts);
        var closed = await service.CloseAllPaymentsAsync(CancellationToken.None);
        Assert.Equal(2, closed.ClosedCount);
        Assert.All(db.FinancialAccounts, account => Assert.NotNull(account.ClosedAtUtc));
        Assert.False((await service.GetClosureReadinessAsync(CancellationToken.None)).CanCloseAll);
    }

    [Fact]
    public async Task Bulk_declaration_announces_to_all_current_students_and_starts_expiry_countdown()
    {
        await using var db = CreateContext();
        AddSettings(db, "2026\u20132027", "Semester 1");
        var department = new Department { DepartmentCode = "IT", Name = "Information Technology" };
        var currentStudents = new[]
        {
            new Student { StudentCode = "STU-BULK-1", PublicId = "INK-BULK-1", FullName = "Bulk Student One", DepartmentId = department.Id, YearLevel = 1, Shift = "Morning" },
            new Student { StudentCode = "STU-BULK-2", PublicId = "INK-BULK-2", FullName = "Bulk Student Two", DepartmentId = department.Id, YearLevel = 1, Shift = "Morning" }
        };
        var retainedStudent = new Student { StudentCode = "STU-OLD", PublicId = "INK-OLD", FullName = "Retained Student", DepartmentId = department.Id, YearLevel = 1, Shift = "Morning" };
        var enrollments = currentStudents.Select((student, index) => new StudentEnrollment
        {
            EnrollmentCode = $"STU-BULK-{index + 1}-ESTU-1",
            StudentId = student.Id,
            DepartmentId = department.Id,
            YearLevel = 1,
            Shift = "Morning",
            AcademicYear = "2026\u20132027",
            Semester = "Semester 1",
            Status = "Active"
        }).ToList();
        enrollments.Add(new StudentEnrollment
        {
            EnrollmentCode = "STU-OLD-ESTU-1",
            StudentId = retainedStudent.Id,
            DepartmentId = department.Id,
            YearLevel = 1,
            Shift = "Morning",
            AcademicYear = "2025\u20132026",
            Semester = "Semester 2",
            Status = "Active"
        });
        db.AddRange(department);
        db.Students.AddRange(currentStudents.Append(retainedStudent));
        db.StudentEnrollments.AddRange(enrollments);
        await db.SaveChangesAsync();
        var service = Service(db);
        await service.GetAsync(null, null, null, null, CancellationToken.None);

        var result = await service.DeclareAllAsync(CancellationToken.None);

        Assert.Equal(2, result.DeclaredCount);
        Assert.Equal(DateOnly.FromDateTime(result.AnnouncedAtUtc).AddDays(14), result.DueOn);
        Assert.Equal(result.DueOn, DateOnly.FromDateTime(result.ExpiresAtUtc));
        var accounts = await db.FinancialAccounts.OrderBy(account => account.AcademicYear).ToListAsync();
        Assert.All(accounts.Where(account => account.AcademicYear == "2026\u20132027"), account =>
        {
            Assert.NotNull(account.DeclaredAtUtc);
            Assert.Equal(result.DueOn, account.DueOn);
            Assert.Equal("Semester 1 payment", account.Title);
            Assert.Equal("Semester", account.PaymentPlan);
            Assert.Equal(750m, account.DeclaredAmount);
        });
        Assert.Null(Assert.Single(accounts, account => account.AcademicYear == "2025\u20132026").DeclaredAtUtc);
        Assert.Equal(2, db.AuditLogs.Count(item => item.Type == "Finance" && item.Action == "Payment declared"));
        Assert.Equal(2, (await service.DeclareAllAsync(CancellationToken.None)).DeclaredCount);
        Assert.Equal(2, db.AuditLogs.Count(item => item.Type == "Finance" && item.Action == "Payment redeclared"));
    }

    [Fact]
    public async Task Updated_settings_stay_inactive_until_declare_reprices_and_renews_unpaid_payment()
    {
        await using var db = CreateContext();
        AddSettings(db, "2026\u20132027", "Semester 1");
        var department = new Department { DepartmentCode = "IT", Name = "Information Technology" };
        var student = new Student { StudentCode = "STU-PRICE", PublicId = "INK-PRICE", FullName = "Price Test Student", DepartmentId = department.Id, YearLevel = 1, Shift = "Morning" };
        var enrollment = new StudentEnrollment { EnrollmentCode = "STU-PRICE-ESTU-1", StudentId = student.Id, DepartmentId = department.Id, YearLevel = 1, Shift = "Morning", AcademicYear = "2026\u20132027", Semester = "Semester 1", Status = "Active" };
        db.AddRange(department, student, enrollment);
        await db.SaveChangesAsync();
        var service = Service(db);
        await service.DeclareAllAsync(CancellationToken.None);
        var original = Assert.Single(await service.GetStudentAsync(student.Id, CancellationToken.None));
        var account = await db.FinancialAccounts.SingleAsync(item => item.Id == original.Id);
        account.DueOn = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-2);
        account.ExpiresAtUtc = DateTime.UtcNow.AddDays(-1);

        (await db.SystemSettings.SingleAsync(setting => setting.Section == "finance" && setting.Key == "semesterPrice")).Value = "0.01";
        await db.SaveChangesAsync();

        var beforeDeclare = Assert.Single(await service.GetStudentAsync(student.Id, CancellationToken.None));
        Assert.Equal(750m, beforeDeclare.DeclaredAmount);
        Assert.True(beforeDeclare.IsExpired);

        var result = await service.DeclareAllAsync(CancellationToken.None);
        var updated = Assert.Single(await service.GetStudentAsync(student.Id, CancellationToken.None));

        Assert.Equal(1, result.DeclaredCount);
        Assert.Equal(0.01m, updated.DeclaredAmount);
        Assert.Equal(0.01m, updated.TotalDue);
        Assert.False(updated.IsExpired);
        Assert.Equal(DateOnly.FromDateTime(DateTime.UtcNow).AddDays(14), updated.DueOn);
        Assert.True(updated.ExpiresAtUtc > DateTime.UtcNow);
        Assert.Contains(db.AuditLogs, item => item.Action == "Payment redeclared" && item.Details.Contains("0.01"));
    }

    [Fact]
    public async Task Verified_dynamic_khqr_stays_open_until_administrator_closes_previous_semester_payment()
    {
        await using var db = CreateContext();
        AddSettings(db, "2026\u20132027", "Semester 2", dynamicQr: true);
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
        db.SemesterResultPublications.Add(new SemesterResultPublication { StudentId = student.Id, Student = student, AcademicYear = "2026\u20132027", Term = "Semester 1", PublishedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var service = Service(db, new SuccessfulBakongHandler());
        var payment = await DeclareAsync(service, student.Id);

        var generated = await service.GenerateStudentQrAsync(student.Id, payment.Id, CancellationToken.None);
        var confirmed = await service.VerifyStudentPaymentAsync(student.Id, payment.Id, CancellationToken.None);

        Assert.StartsWith("000201", generated.QrPayload);
        Assert.Equal("Paid", confirmed.Status);
        Assert.Equal("dynamic-test-transaction", confirmed.ConfirmationMethod);
        Assert.Null(confirmed.ClosedAtUtc);
        Assert.True(confirmed.CanClosePayment);
        Assert.DoesNotContain(db.StudentEnrollments, item =>
            item.StudentId == student.Id
            && item.AcademicYear == "2026\u20132027"
            && item.Semester == "Semester 2");

        var closed = await service.ClosePaymentAsync(payment.Id, CancellationToken.None);

        Assert.NotNull(closed.ClosedAtUtc);
        Assert.Equal("Retained", closed.PeriodState);
        Assert.Contains(await service.GetAsync(null, null, null, "History", CancellationToken.None), item => item.Id == payment.Id);
        Assert.DoesNotContain(await service.GetAsync(null, null, null, "All", CancellationToken.None), item => item.Id == payment.Id);
        Assert.Contains(db.StudentEnrollments, item =>
            item.StudentId == student.Id
            && item.AcademicYear == "2026\u20132027"
            && item.Semester == "Semester 2"
            && item.Status == "Active");
        Assert.Equal(2, await db.StudentEnrollments.CountAsync());
    }

    [Fact]
    public async Task Dynamic_qr_is_generated_for_its_student_and_only_bank_verification_marks_it_paid()
    {
        await using var db = CreateContext();
        AddSettings(db, "2026\u20132027", "Semester 1", dynamicQr: true);
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
        var service = Service(db, new SuccessfulBakongHandler());
        var payment = await DeclareAsync(service, student.Id);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => service.GenerateStudentQrAsync(
            Guid.NewGuid(),
            payment.Id,
            CancellationToken.None));
        await Assert.ThrowsAsync<ArgumentException>(() => service.VerifyStudentPaymentAsync(student.Id, payment.Id, CancellationToken.None));
        var generated = await service.GenerateStudentQrAsync(student.Id, payment.Id, CancellationToken.None);
        var confirmed = await service.VerifyStudentPaymentAsync(student.Id, payment.Id, CancellationToken.None);

        Assert.StartsWith("000201", generated.QrPayload);
        Assert.Equal(750m, generated.Balance);
        Assert.Equal("Paid", confirmed.Status);
        Assert.Equal("dynamic-test-transaction", confirmed.ConfirmationMethod);
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
        db.SemesterResultPublications.Add(new SemesterResultPublication { StudentId = student.Id, Student = student, AcademicYear = "2026\u20132027", Term = "Semester 2", PublishedAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var service = Service(db);
        var payment = await DeclareAsync(service, student.Id);

        var paid = await service.RecordPaymentAsync(
            payment.Id,
            new RecordFinancePaymentDto(payment.Balance, "Cash", "LATE-PAYMENT", DateTime.UtcNow),
            CancellationToken.None);

        Assert.Equal("Paid", paid.Status);
        Assert.Null(paid.ClosedAtUtc);
        Assert.Equal(1, student.YearLevel);
        Assert.Single(db.StudentEnrollments);

        await service.ClosePaymentAsync(payment.Id, CancellationToken.None);

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
            new RecordFinancePaymentDto(100m, "Cash", "ABA-REF-1", DateTime.UtcNow),
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

        var corrected = await service.UpdatePaymentAsync(account.Id, transaction.Id, new UpdateFinancePaymentDto(120m, "Cash", "ABA-REF-2", DateTime.UtcNow), CancellationToken.None);
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
    public async Task Student_sees_only_declared_cards_and_each_semester_requires_its_own_payment()
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
        var declaration = await service.DeclareAsync(account.Id, Declaration("Semester"), CancellationToken.None);
        Assert.Equal("Semester", declaration.PaymentPlan);
        Assert.Single(await service.GetStudentAsync(student.Id, CancellationToken.None));
        await service.RecordPaymentAsync(account.Id, new RecordFinancePaymentDto(750m, "Cash", "YEAR-1", DateTime.UtcNow), CancellationToken.None);

        var semesterTwo = new StudentEnrollment { EnrollmentCode = semesterOne.EnrollmentCode, StudentId = student.Id, DepartmentId = department.Id, YearLevel = 1, Shift = "Morning", AcademicYear = semesterOne.AcademicYear, Semester = "Semester 2", Status = "Active" };
        db.StudentEnrollments.Add(semesterTwo);
        await db.SaveChangesAsync();
        var settings = new FinanceSettingsReader(db);
        var gate = new SemesterPaymentGate(db, new FinancialAccountSynchronizer(db, settings), settings);
        var result = await gate.EvaluateAsync(semesterTwo.AcademicYear, semesterTwo.Semester, CancellationToken.None);
        await db.SaveChangesAsync();

        Assert.DoesNotContain(student.Id, result.CompletedStudentIds);
        Assert.Equal(1, result.HeldStudents);
        Assert.Equal(2, await db.FinancialAccounts.CountAsync(item => item.StudentId == student.Id));
    }

    [Fact]
    public async Task Expiry_can_only_be_extended_before_expiration_with_days_and_reason()
    {
        await using var db = CreateContext();
        AddSettings(db, "2026\u20132027", "Semester 1");
        var department = new Department { DepartmentCode = "IT", Name = "Information Technology" };
        var student = new Student { StudentCode = "STU-EXT", PublicId = "INK-EXT", FullName = "Extension Student", DepartmentId = department.Id, YearLevel = 1, Shift = "Morning" };
        var enrollment = new StudentEnrollment { EnrollmentCode = "STU-EXT-ESTU-1", StudentId = student.Id, DepartmentId = department.Id, YearLevel = 1, Shift = "Morning", AcademicYear = "2026\u20132027", Semester = "Semester 1", Status = "Active" };
        db.AddRange(department, student, enrollment);
        await db.SaveChangesAsync();
        var service = Service(db);
        var declared = await DeclareAsync(service, student.Id);

        await Assert.ThrowsAsync<ArgumentException>(() => service.DeclareAsync(
            declared.Id,
            new FinanceDeclarationDto(declared.Title, declared.PaymentPlan, declared.DeclaredAmount!.Value, declared.DueOn.AddDays(1), declared.ExpiresAtUtc!.Value.AddDays(1)),
            CancellationToken.None));

        var extended = await service.ExtendExpiryAsync(
            declared.Id,
            new FinanceExpiryExtensionDto(3, "Student is away for a family responsibility."),
            CancellationToken.None);

        Assert.Equal(declared.DueOn.AddDays(3), extended.DueOn);
        Assert.Equal(declared.ExpiresAtUtc!.Value.AddDays(3), extended.ExpiresAtUtc);
        Assert.Contains(db.AuditLogs, item => item.Action == "Payment expiry extended" && item.Details.Contains("family responsibility"));

        var account = await db.FinancialAccounts.SingleAsync(item => item.Id == declared.Id);
        account.ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1);
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<ArgumentException>(() => service.ExtendExpiryAsync(
            declared.Id,
            new FinanceExpiryExtensionDto(1, "Late request"),
            CancellationToken.None));
    }

    [Fact]
    public async Task Ten_expired_days_add_ten_daily_punishments_and_payment_remains_allowed()
    {
        await using var db = CreateContext();
        AddSettings(db, "2026\u20132027", "Semester 1", latePenaltyPerDay: "2.50");
        var department = new Department { DepartmentCode = "IT", Name = "Information Technology" };
        var student = new Student { StudentCode = "STU-LATE-10", PublicId = "INK-LATE-10", FullName = "Late Fee Student", DepartmentId = department.Id, YearLevel = 1, Shift = "Morning" };
        var enrollment = new StudentEnrollment { EnrollmentCode = "STU-LATE-10-ESTU-1", StudentId = student.Id, DepartmentId = department.Id, YearLevel = 1, Shift = "Morning", AcademicYear = "2026\u20132027", Semester = "Semester 1", Status = "Active" };
        db.AddRange(department, student, enrollment);
        await db.SaveChangesAsync();
        var service = Service(db);
        var declared = await DeclareAsync(service, student.Id);
        var account = await db.FinancialAccounts.SingleAsync(item => item.Id == declared.Id);
        account.ExpiresAtUtc = DateTime.UtcNow.Date.AddDays(-10).AddHours(12);
        await db.SaveChangesAsync();

        var late = Assert.Single(await service.GetStudentAsync(student.Id, CancellationToken.None));
        Assert.True(late.IsExpired);
        Assert.Equal(10, late.LatePenaltyDays);
        Assert.Equal(25m, late.LatePenaltyAmount);
        Assert.Equal(775m, late.TotalDue);

        var paid = await service.RecordPaymentAsync(
            late.Id,
            new RecordFinancePaymentDto(775m, "Cash", "LATE-10-DAYS", DateTime.UtcNow),
            CancellationToken.None);
        Assert.Equal("Paid", paid.Status);
        Assert.Equal(0m, paid.Balance);
    }

    [Fact]
    public async Task Closed_current_semester_stays_read_only_in_finance_until_semester_ends()
    {
        await using var db = CreateContext();
        AddSettings(db, "2026\u20132027", "Semester 1");
        var department = new Department { DepartmentCode = "IT", Name = "Information Technology" };
        var student = new Student { StudentCode = "STU-OPEN", PublicId = "INK-OPEN", FullName = "Open Payment Student", DepartmentId = department.Id, YearLevel = 1, Shift = "Morning" };
        var enrollment = new StudentEnrollment { EnrollmentCode = "STU-OPEN-ESTU-1", StudentId = student.Id, DepartmentId = department.Id, YearLevel = 1, Shift = "Morning", AcademicYear = "2026\u20132027", Semester = "Semester 1", Status = "Active" };
        db.AddRange(department, student, enrollment);
        await db.SaveChangesAsync();
        var service = Service(db);
        var account = await DeclareAsync(service, student.Id);

        Assert.Equal("Pending", account.Status);
        Assert.False(account.CanClosePayment);
        var pendingReason = await Assert.ThrowsAsync<ArgumentException>(() => service.ClosePaymentAsync(account.Id, CancellationToken.None));
        Assert.Contains("full balance is paid", pendingReason.Message);
        Assert.Single(db.StudentEnrollments);

        var paid = await service.RecordPaymentAsync(account.Id, new RecordFinancePaymentDto(account.Balance, "Cash", "OPEN-PAID", DateTime.UtcNow), CancellationToken.None);

        Assert.Equal("Paid", paid.Status);
        Assert.Null(paid.ClosedAtUtc);
        Assert.True(paid.CanClosePayment);
        Assert.Contains(await service.GetAsync(null, null, null, "All", CancellationToken.None), item => item.Id == account.Id);
        Assert.Empty(await service.GetAsync(null, null, null, "History", CancellationToken.None));

        var closed = await service.ClosePaymentAsync(account.Id, CancellationToken.None);

        Assert.NotNull(closed.ClosedAtUtc);
        Assert.Equal("Current", closed.PeriodState);
        Assert.Contains(await service.GetAsync(null, null, null, "All", CancellationToken.None), item => item.Id == account.Id && item.ClosedAtUtc.HasValue);
        Assert.Contains(await service.GetAsync(null, null, null, "Closed", CancellationToken.None), item => item.Id == account.Id);
        Assert.Empty(await service.GetAsync(null, null, null, "History", CancellationToken.None));
        var reason = await Assert.ThrowsAsync<ArgumentException>(() => service.AdjustAsync(account.Id, new FinancialAdjustmentDto(-1m, "Closed account correction"), CancellationToken.None));
        Assert.Contains("closed and read-only", reason.Message);
        Assert.Single(db.StudentEnrollments);
    }

    [Fact]
    public async Task Mock_scan_qr_is_bound_to_public_id_and_paid_account_becomes_read_only()
    {
        await using var db = CreateContext();
        AddSettings(db, "2026\u20132027", "Semester 1", mockPayment: true);
        var department = new Department { DepartmentCode = "IT", Name = "Information Technology" };
        var student = new Student { StudentCode = "STU-MOCK", FullName = "Mock Payment Student", DepartmentId = department.Id, YearLevel = 1, Shift = "Morning" };
        var enrollment = new StudentEnrollment
        {
            EnrollmentCode = "ENR-MOCK", PublicId = "STU-PUBLIC-MOCK", StudentId = student.Id, Student = student,
            DepartmentId = department.Id, Department = department, YearLevel = 1, Shift = "Morning",
            AcademicYear = "2026\u20132027", Semester = "Semester 1", Status = "Active"
        };
        db.AddRange(department, student, enrollment);
        await db.SaveChangesAsync();
        var service = Service(db);
        var account = await DeclareAsync(service, student.Id);
        var qr = await service.GenerateMockPaymentQrAsync(account.Id, CancellationToken.None);

        Assert.Equal(enrollment.PublicId, qr.PublicId);
        Assert.StartsWith("INK-MOCK-PAY:", qr.QrPayload);
        await Assert.ThrowsAsync<ArgumentException>(() => service.ScanMockPaymentQrAsync(
            student.Id, account.Id, new MockPaymentScanDto(qr.QrPayload + "tampered"), CancellationToken.None));

        var paid = await service.ScanMockPaymentQrAsync(
            student.Id, account.Id, new MockPaymentScanDto(qr.QrPayload), CancellationToken.None);

        Assert.Equal("Paid", paid.Status);
        Assert.Equal("Mock QR", Assert.Single(paid.Payments).Method);
        var readOnly = await Assert.ThrowsAsync<ArgumentException>(() => service.AdjustAsync(
            account.Id, new FinancialAdjustmentDto(-1m, "Must stay read-only"), CancellationToken.None));
        Assert.Contains("Paid and read-only", readOnly.Message);
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

    private static FinanceService Service(InstituteDbContext db, HttpMessageHandler? handler = null)
    {
        var settings = new FinanceSettingsReader(db);
        var synchronizer = new FinancialAccountSynchronizer(db, settings);
        return new FinanceService(
            db,
            new InstituteCache(),
            synchronizer,
            new FinancialProgression(db, new ActivePeriodLedgerCreator(db)),
            settings,
            new BakongPaymentGateway(
                new HttpClient(handler ?? new HttpClientHandler()),
                new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Bakong:Token"] = "test-server-token" }).Build()),
            new MockPaymentQrGateway(new EphemeralDataProtectionProvider()));
    }

    private static void AddSettings(
        InstituteDbContext db,
        string academicYear,
        string semester,
        string latePenaltyPerDay = "0.00",
        bool dynamicQr = false,
        bool mockPayment = false)
    {
        db.SystemSettings.AddRange(
            Setting("academic-year", "currentYear", academicYear),
            Setting("semester", "currentTerm", semester),
            Setting("semester", "startsOn", semester == "Semester 1" ? "2026-08-01" : "2027-02-01"),
            Setting("semester", "endsOn", semester == "Semester 1" ? "2027-01-31" : "2027-06-30"),
            Setting("finance", "semesterPrice", "750.00"),
            Setting("finance", "otherFee", "0.00"),
            Setting("finance", "currency", "USD"),
            Setting("finance", "paymentDueDays", "14"),
            Setting("finance", "latePenaltyPerDay", latePenaltyPerDay),
            Setting("finance", "paymentMethods", "Cash,Bakong,Wing,Bank Transfer,Other"),
            Setting("finance", "allowPartialPayments", "true"),
            Setting("finance", "allowOverpayment", "false"),
            Setting("finance", "maximumAdjustmentAmount", "1000000"),
            Setting("finance", "requirePaidForAdvancement", "true"),
            Setting("finance", "bakongEnabled", dynamicQr ? "true" : "false"),
            Setting("finance", "bakongEnvironment", "SIT"),
            Setting("finance", "bakongAccountId", dynamicQr ? "institute@devb" : ""),
            Setting("finance", "bakongAccountInformation", dynamicQr ? "85512345678" : ""),
            Setting("finance", "bakongAcquiringBank", dynamicQr ? "Dev Bank" : ""),
            Setting("finance", "bakongMerchantName", "Institude of New Khmer"),
            Setting("finance", "bakongMerchantCity", "Phnom Penh"),
            Setting("finance", "mockPaymentEnabled", mockPayment ? "true" : "false"));
    }

    private static SystemSetting Setting(string section, string key, string value) =>
        new() { Section = section, Key = key, Value = value };

    private static InstituteDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<InstituteDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);

    private sealed class SuccessfulBakongHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "responseCode": 0,
                      "responseMessage": "Success",
                      "data": {
                        "hash": "dynamic-test-transaction",
                        "fromAccountId": "student@devb",
                        "toAccountId": "institute@devb",
                        "currency": "USD",
                        "amount": 750.00
                      }
                    }
                    """,
                    Encoding.UTF8,
                    "application/json")
            });
    }
}
