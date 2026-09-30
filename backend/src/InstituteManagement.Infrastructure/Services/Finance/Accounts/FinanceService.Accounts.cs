using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Finance;
using Microsoft.EntityFrameworkCore;

namespace InstituteManagement.Infrastructure.Services.Finance;

public sealed partial class FinanceService
{
    public async Task<IReadOnlyList<StudentPaymentDto>> GetAsync(
        string? search,
        string? academicYear,
        string? semester,
        string? status,
        Guid? departmentId,
        int? year,
        CancellationToken cancellationToken) =>
        (await GetAsync(search, academicYear, semester, status, departmentId, year, new PageRequest(1, 100), cancellationToken)).Items;

    public async Task<PagedResult<StudentPaymentDto>> GetAsync(
        string? search,
        string? academicYear,
        string? semester,
        string? status,
        Guid? departmentId,
        int? year,
        PageRequest page,
        CancellationToken cancellationToken)
    {
        await EnsureLedgerAsync(cancellationToken);
        var period = await db.SystemSettings.AsNoTracking()
            .Where(setting =>
                setting.Section == "academic-year" && setting.Key == "currentYear"
                || setting.Section == "semester" && setting.Key == "currentTerm")
            .ToDictionaryAsync(setting => $"{setting.Section}:{setting.Key}", setting => setting.Value, cancellationToken);
        var currentYear = period.GetValueOrDefault("academic-year:currentYear", "");
        var currentSemester = period.GetValueOrDefault("semester:currentTerm", "");
        var query = AccountQuery()
            .Where(account =>
                (string.IsNullOrWhiteSpace(academicYear) || account.AcademicYear == academicYear)
                && (string.IsNullOrWhiteSpace(semester) || account.Semester == semester)
                && (!departmentId.HasValue || account.StudentEnrollment!.DepartmentId == departmentId)
                && (!year.HasValue || account.StudentEnrollment!.YearLevel == year.Value));
        var penaltyAccounts = await query
            .Where(account => account.DeclaredAtUtc.HasValue
                && account.ExpiresAtUtc.HasValue
                && account.Status != "Paid"
                && account.Status != "Cancelled"
                && (account.ExpiresAtUtc <= DateTime.UtcNow || account.LatePenaltyDays != 0 || account.LatePenaltyAmount != 0))
            .ToListAsync(cancellationToken);
        await ApplyLatePenaltiesAsync(penaltyAccounts, cancellationToken);
        var term = search?.Trim();
        if (!string.IsNullOrWhiteSpace(term))
            query = query.Where(account =>
                account.FinancialAccountCode.Contains(term)
                || account.Student!.StudentCode.Contains(term)
                || account.StudentEnrollment!.PublicId.Contains(term)
                || account.Student.FullName.Contains(term)
                || account.StudentEnrollment.EnrollmentCode.Contains(term)
                || account.StudentEnrollment.Department!.Name.Contains(term)
                || account.Title.Contains(term)
                || account.PaymentPlan.Contains(term)
                || account.Payments.Any(payment => payment.PaymentCode.Contains(term)));

        var archived = query.Where(account =>
            account.ClosedAtUtc.HasValue
            && (account.AcademicYear != currentYear || account.Semester != currentSemester)
            && db.SemesterResultPublications.Any(result => result.StudentId == account.StudentId && result.AcademicYear == account.AcademicYear && result.Term == account.Semester));
        if (string.IsNullOrWhiteSpace(status) || status == "All")
            query = query.Where(account => !account.ClosedAtUtc.HasValue
                || account.AcademicYear == currentYear && account.Semester == currentSemester
                || !db.SemesterResultPublications.Any(result => result.StudentId == account.StudentId && result.AcademicYear == account.AcademicYear && result.Term == account.Semester));
        else if (status.Equals("History", StringComparison.OrdinalIgnoreCase))
            query = archived;
        else if (status.Equals("Closed", StringComparison.OrdinalIgnoreCase))
            query = query.Where(account => account.ClosedAtUtc.HasValue
                && (account.AcademicYear == currentYear
                    && account.Semester == currentSemester
                    || !db.SemesterResultPublications.Any(result => result.StudentId == account.StudentId && result.AcademicYear == account.AcademicYear && result.Term == account.Semester)));
        else
            query = query.Where(account => !account.ClosedAtUtc.HasValue && account.Status == status);

        var totalCount = await query.CountAsync(cancellationToken);
        var accounts = await query
            .OrderBy(account => account.StudentEnrollment!.YearLevel)
            .ThenBy(account => account.StudentEnrollment!.Shift == "Morning" ? 0 : account.StudentEnrollment.Shift == "Afternoon" ? 1 : account.StudentEnrollment.Shift == "Evening" ? 2 : account.StudentEnrollment.Shift == "Weekend" ? 3 : 4)
            .ThenBy(account => account.Student!.FullName)
            .ThenBy(account => account.Id)
            .Skip(page.Skip)
            .Take(page.NormalizedPageSize)
            .ToListAsync(cancellationToken);
        var mapped = await MapAsync(accounts, cancellationToken);
        return PagedResult<StudentPaymentDto>.Create(mapped, page, totalCount);
    }

    public async Task<IReadOnlyList<StudentPaymentDto>> GetStudentAsync(
        Guid studentId,
        CancellationToken cancellationToken)
    {
        if (!await db.Students.AsNoTracking().AnyAsync(student => student.Id == studentId && student.Status != "Inactive", cancellationToken))
            throw new KeyNotFoundException("Active student not found.");
        await EnsureLedgerAsync(cancellationToken);
        var accounts = await AccountQuery()
            .Where(account => account.StudentId == studentId && account.DeclaredAtUtc != null)
            .OrderBy(account => account.Status == "Paid")
            .ThenByDescending(account => account.AcademicYear)
            .ThenByDescending(account => account.Semester)
            .ToListAsync(cancellationToken);
        await ApplyLatePenaltiesAsync(accounts, cancellationToken);
        return await MapAsync(accounts, cancellationToken);
    }

    public async Task<FinanceOptionsDto> GetOptionsAsync(CancellationToken cancellationToken)
    {
        var settings = await settingsReader.GetAsync(cancellationToken);
        return new(
            settings.PaymentMethods,
            settings.AllowPartialPayments,
            settings.AllowOverpayment,
            settings.MaximumAdjustmentAmount,
            settings.RequirePaidForAdvancement,
            settings.PaymentDueDays,
            settings.TuitionFee,
            settings.OtherFee,
            settings.BakongEnabled,
            bakong.IsConfigured(settings),
            settings.BakongEnvironment,
            string.IsNullOrWhiteSpace(settings.BakongAcquiringBank) ? "Bakong" : settings.BakongAcquiringBank,
            settings.BakongMerchantName,
            string.IsNullOrWhiteSpace(settings.BakongAccountInformation) ? settings.BakongAccountId : settings.BakongAccountInformation,
            settings.MockPaymentEnabled);
    }

    public async Task<StudentPaymentDto> DeclareAsync(
        Guid financialAccountId,
        FinanceDeclarationDto request,
        CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(financialAccountId, cancellationToken);
        EnsureOpen(account);
        var settings = await settingsReader.GetAsync(cancellationToken);
        if (account.Status == "Cancelled") throw new ArgumentException("A cancelled financial account cannot be declared.");
        var title = request.Title?.Trim() ?? string.Empty;
        if (title.Length is < 3 or > 160) throw new ArgumentException("Declaration title must contain 3 to 160 characters.");
        var paymentPlan = request.PaymentPlan?.Trim();
        if (paymentPlan != "Semester") throw new ArgumentException("Only Semester payment declarations are available.");
        var amount = decimal.Round(request.Amount, 2);
        if (amount <= 0) throw new ArgumentException("Declaration amount must be greater than zero.");
        var expiresAtUtc = request.ExpiresAtUtc.ToUniversalTime();
        var wasDeclared = account.DeclaredAtUtc.HasValue;
        if (!wasDeclared && expiresAtUtc <= DateTime.UtcNow) throw new ArgumentException("Payment expiry must be in the future.");
        if (DateOnly.FromDateTime(expiresAtUtc) < request.DueOn) throw new ArgumentException("Payment expiry cannot be earlier than the due date.");
        if (wasDeclared && (request.DueOn != account.DueOn || !SameMinute(expiresAtUtc, account.ExpiresAtUtc)))
            throw new ArgumentException("Use Additional expiry days to extend a student payment before it expires.");
        if (amount + account.AdjustmentAmount < TotalPaid(account))
            throw new ArgumentException("Declaration amount plus its adjustment cannot be lower than money already paid.");

        var oldTitle = account.Title;
        var oldPlan = account.PaymentPlan;
        var oldAmount = account.DeclaredAmount;
        var oldDueOn = account.DueOn;
        var oldExpiresAtUtc = account.ExpiresAtUtc;
        var oldStatus = account.Status;
        var oldBalance = Balance(account);
        account.Title = title;
        account.PaymentPlan = paymentPlan;
        account.DeclaredAmount = amount;
        account.DeclaredAtUtc ??= DateTime.UtcNow;
        account.DueOn = request.DueOn;
        account.ExpiresAtUtc = expiresAtUtc;
        Recalculate(account);
        ClearQr(account);
        AddFinanceAudit(account, wasDeclared ? "Payment declaration updated" : "Payment declared", new
        {
            account.FinancialAccountCode,
            account.StudentEnrollment!.EnrollmentCode,
            oldTitle,
            newTitle = account.Title,
            oldPlan,
            newPlan = account.PaymentPlan,
            oldAmount,
            newAmount = account.DeclaredAmount,
            oldDueOn,
            newDueOn = account.DueOn,
            oldExpiresAtUtc,
            newExpiresAtUtc = account.ExpiresAtUtc,
            oldFinancialState = oldStatus,
            newFinancialState = account.Status,
            oldBalance,
            newBalance = Balance(account),
            performedBy = "Administrator"
        });
        await SaveAsync(cancellationToken);
        return AssertSingle(await MapAsync([account], cancellationToken));
    }

    public async Task<BulkFinanceDeclarationResultDto> DeclareAllAsync(
        Guid? departmentId,
        int? year,
        CancellationToken cancellationToken)
    {
        var period = await db.SystemSettings.AsNoTracking()
            .Where(setting =>
                setting.Section == "academic-year" && setting.Key == "currentYear"
                || setting.Section == "semester" && setting.Key == "currentTerm")
            .ToDictionaryAsync(setting => $"{setting.Section}:{setting.Key}", setting => setting.Value, cancellationToken);
        var academicYear = period.GetValueOrDefault("academic-year:currentYear", string.Empty);
        var semester = period.GetValueOrDefault("semester:currentTerm", string.Empty);
        if (string.IsNullOrWhiteSpace(academicYear) || string.IsNullOrWhiteSpace(semester))
            throw new InvalidOperationException("Set the current academic year and semester before declaring student payments.");

        await synchronizer.EnsurePeriodAsync(academicYear, semester, cancellationToken);
        if (db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(cancellationToken);

        var accounts = await AccountQuery()
            .Where(account =>
                account.AcademicYear == academicYear
                && account.Semester == semester
                && (!departmentId.HasValue || account.StudentEnrollment!.DepartmentId == departmentId)
                && (!year.HasValue || account.StudentEnrollment!.YearLevel == year.Value)
                && account.Status == "Pending"
                && !account.Payments.Any(payment => payment.Status == "Completed")
                && account.StudentEnrollment!.Status == "Active"
                && account.Student!.Status != "Inactive")
            .OrderBy(account => account.Student!.FullName)
            .ToListAsync(cancellationToken);
        var settings = await settingsReader.GetAsync(cancellationToken);
        const string paymentPlan = "Semester";
        var announcedAtUtc = DateTime.UtcNow;
        var dueOn = DateOnly.FromDateTime(announcedAtUtc).AddDays(settings.PaymentDueDays);
        var expiresAtUtc = DateTime.SpecifyKind(dueOn.ToDateTime(TimeOnly.MaxValue), DateTimeKind.Utc);
        var declaredCount = 0;

        foreach (var account in accounts)
        {
            var amount = decimal.Round(settings.TuitionFee + settings.OtherFee, 2);
            if (amount <= 0) continue;
            var wasDeclared = account.DeclaredAtUtc.HasValue;
            var oldTitle = account.Title;
            var oldPlan = account.PaymentPlan;
            var oldAmount = account.DeclaredAmount;
            var oldDeclaredAtUtc = account.DeclaredAtUtc;
            var oldDueOn = account.DueOn;
            var oldExpiresAtUtc = account.ExpiresAtUtc;
            var oldLatePenaltyDays = account.LatePenaltyDays;
            var oldLatePenaltyAmount = account.LatePenaltyAmount;
            var oldStatus = account.Status;
            var oldBalance = Balance(account);
            account.Title = $"{semester} payment";
            account.PaymentPlan = paymentPlan;
            account.DeclaredAmount = amount;
            account.DeclaredAtUtc = announcedAtUtc;
            account.DueOn = dueOn;
            account.ExpiresAtUtc = expiresAtUtc;
            account.LatePenaltyDays = 0;
            account.LatePenaltyAmount = 0;
            Recalculate(account);
            ClearQr(account);
            AddFinanceAudit(account, wasDeclared ? "Payment redeclared" : "Payment declared", new
            {
                account.FinancialAccountCode,
                account.StudentEnrollment!.EnrollmentCode,
                oldTitle,
                newTitle = account.Title,
                oldPlan,
                newPlan = account.PaymentPlan,
                oldAmount,
                newAmount = account.DeclaredAmount,
                oldDeclaredAtUtc,
                announcedAtUtc,
                oldDueOn,
                newDueOn = account.DueOn,
                oldExpiresAtUtc,
                newExpiresAtUtc = account.ExpiresAtUtc,
                oldLatePenaltyDays,
                oldLatePenaltyAmount,
                oldFinancialState = oldStatus,
                newFinancialState = account.Status,
                oldBalance,
                newBalance = Balance(account),
                performedBy = "Administrator",
                declarationScope = "All current students"
            });
            declaredCount++;
        }

        if (declaredCount > 0) await SaveAsync(cancellationToken);
        return new(declaredCount, announcedAtUtc, dueOn, expiresAtUtc);
    }

    public async Task<FinanceClosureReadinessDto> GetClosureReadinessAsync(
        Guid? departmentId,
        int? year,
        CancellationToken cancellationToken)
    {
        var period = await db.SystemSettings.AsNoTracking()
            .Where(item => item.Section == "academic-year" && item.Key == "currentYear"
                || item.Section == "semester" && item.Key == "currentTerm")
            .ToDictionaryAsync(item => $"{item.Section}:{item.Key}", item => item.Value, cancellationToken);
        var academicYear = period.GetValueOrDefault("academic-year:currentYear", string.Empty);
        var semester = period.GetValueOrDefault("semester:currentTerm", string.Empty);
        if (string.IsNullOrWhiteSpace(academicYear) || string.IsNullOrWhiteSpace(semester))
            throw new InvalidOperationException("Set the current academic year and semester before closing payments.");
        await synchronizer.EnsurePeriodAsync(academicYear, semester, cancellationToken);
        if (db.ChangeTracker.HasChanges()) await SaveAsync(cancellationToken);
        var rows = db.FinancialAccounts.AsNoTracking()
            .Where(account => account.AcademicYear == academicYear
                && account.Semester == semester
                && (!departmentId.HasValue || account.StudentEnrollment!.DepartmentId == departmentId)
                && (!year.HasValue || account.StudentEnrollment!.YearLevel == year.Value)
                && account.StudentEnrollment!.Status == "Active"
                && account.Student!.Status != "Inactive")
            .Select(account => new
            {
                account.Status,
                account.ClosedAtUtc,
                Declared = account.DeclaredAtUtc.HasValue,
                account.Currency,
                Due = (account.DeclaredAmount ?? account.TuitionFee + account.OtherFee) + account.AdjustmentAmount + account.LatePenaltyAmount < 0m
                    ? 0m
                    : (account.DeclaredAmount ?? account.TuitionFee + account.OtherFee) + account.AdjustmentAmount + account.LatePenaltyAmount,
                Paid = account.Payments.Where(payment => payment.Status == "Completed").Sum(payment => (decimal?)payment.Amount) ?? 0m
            })
            .Select(account => new
            {
                account.Status,
                account.ClosedAtUtc,
                account.Declared,
                account.Currency,
                account.Due,
                account.Paid,
                Balance = account.Due - account.Paid < 0m ? 0m : account.Due - account.Paid
            });
        var aggregate = await rows.GroupBy(_ => 1).Select(group => new FinanceReadinessAggregate(
            group.Count(),
            group.Count(account => account.Status == "Paid" && account.Balance <= 0m),
            group.Count(account => account.Status == "Paid" && account.Balance <= 0m && !account.ClosedAtUtc.HasValue),
            group.Sum(account => account.Declared ? account.Due : 0m),
            group.Sum(account => account.Declared ? account.Paid : 0m),
            group.Sum(account => account.Declared ? account.Balance : 0m),
            group.Max(account => account.Currency) ?? "USD")).FirstOrDefaultAsync(cancellationToken);
        if (aggregate is null) return new(0, 0, 0, false, 0m, 0m, 0m, "USD");
        return new(
            aggregate.TotalAccounts,
            aggregate.PaidAccounts,
            aggregate.OpenPaidAccounts,
            aggregate.TotalAccounts > 0 && aggregate.PaidAccounts == aggregate.TotalAccounts && aggregate.OpenPaidAccounts > 0,
            aggregate.TotalDue,
            aggregate.TotalCollected,
            aggregate.TotalOutstanding,
            aggregate.Currency);
    }

    private sealed record FinanceReadinessAggregate(
        int TotalAccounts,
        int PaidAccounts,
        int OpenPaidAccounts,
        decimal TotalDue,
        decimal TotalCollected,
        decimal TotalOutstanding,
        string Currency);

    public async Task<BulkFinanceClosureResultDto> CloseAllPaymentsAsync(
        Guid? departmentId,
        int? year,
        CancellationToken cancellationToken)
    {
        var accounts = await CurrentActiveAccountsAsync(departmentId, year, cancellationToken);
        if (accounts.Count == 0) throw new InvalidOperationException("No current student payments are available to close.");
        if (accounts.Any(account => account.Status != "Paid" || Balance(account) > 0m))
            throw new InvalidOperationException("All current student payments in the selected scope must be fully paid before payments can be closed together.");

        var closedAtUtc = DateTime.UtcNow;
        var openAccounts = accounts.Where(account => !account.ClosedAtUtc.HasValue).ToList();
        foreach (var account in openAccounts)
        {
            account.ClosedAtUtc = closedAtUtc;
            account.UpdatedAtUtc = closedAtUtc;
            ClearQr(account);
            AddFinanceAudit(account, "Payment closed", new
            {
                account.FinancialAccountCode,
                account.StudentEnrollment!.EnrollmentCode,
                account.AcademicYear,
                account.Semester,
                account.Status,
                account.ClosedAtUtc,
                closureScope = departmentId.HasValue || year.HasValue ? "Current filtered students" : "All current students",
                historyState = "Waiting for Semester Result publication and semester end",
                performedBy = "Administrator"
            });
        }
        if (openAccounts.Count > 0) await SaveAsync(cancellationToken);
        return new(openAccounts.Count, accounts.Count, closedAtUtc);
    }

    public async Task<StudentPaymentDto> ExtendExpiryAsync(
        Guid financialAccountId,
        FinanceExpiryExtensionDto request,
        CancellationToken cancellationToken)
    {
        var account = await FindAccountAsync(financialAccountId, cancellationToken);
        EnsureOpen(account);
        if (!account.DeclaredAtUtc.HasValue || !account.ExpiresAtUtc.HasValue)
            throw new ArgumentException("Declare the student payment before adding expiry days.");
        if (IsExpired(account))
            throw new ArgumentException("An expired payment cannot receive additional days.");
        if (account.Status is "Paid" or "Cancelled")
            throw new ArgumentException("A completed or cancelled payment cannot receive additional days.");
        if (request.Days is < 1 or > 365)
            throw new ArgumentException("Additional expiry days must be between 1 and 365.");
        var reason = request.Reason?.Trim() ?? string.Empty;
        if (reason.Length is < 3 or > 500)
            throw new ArgumentException("An extension reason containing 3 to 500 characters is required.");

        var oldDueOn = account.DueOn;
        var oldExpiresAtUtc = account.ExpiresAtUtc.Value;
        account.DueOn = account.DueOn.AddDays(request.Days);
        account.ExpiresAtUtc = account.ExpiresAtUtc.Value.AddDays(request.Days);
        account.UpdatedAtUtc = DateTime.UtcNow;
        ClearQr(account);
        AddFinanceAudit(account, "Payment expiry extended", new
        {
            account.FinancialAccountCode,
            account.StudentEnrollment!.EnrollmentCode,
            additionalDays = request.Days,
            reason,
            oldDueOn,
            newDueOn = account.DueOn,
            oldExpiresAtUtc,
            newExpiresAtUtc = account.ExpiresAtUtc,
            performedBy = "Administrator"
        });
        await SaveAsync(cancellationToken);
        return AssertSingle(await MapAsync([account], cancellationToken));
    }
}
