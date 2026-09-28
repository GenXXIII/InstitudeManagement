using InstituteManagement.Application.Features.Finance;
using InstituteManagement.Application.Features.Enrollment.Students.Progression;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Common;

namespace InstituteManagement.Infrastructure.Services.Finance;

public sealed partial class FinanceService : IFinanceService
{
    private readonly InstituteDbContext db;
    private readonly InstituteCache cache;
    private readonly FinancialAccountSynchronizer synchronizer;
    private readonly IStudentEnrollmentProgression progression;
    private readonly FinanceSettingsReader settingsReader;
    private readonly BakongPaymentGateway bakong;
    private readonly MockPaymentQrGateway mockPayments;

    public FinanceService(
        InstituteDbContext db,
        InstituteCache cache,
        FinancialAccountSynchronizer synchronizer,
        IStudentEnrollmentProgression progression,
        FinanceSettingsReader settingsReader,
        BakongPaymentGateway bakong,
        MockPaymentQrGateway mockPayments)
    {
        this.db = db;
        this.cache = cache;
        this.synchronizer = synchronizer;
        this.progression = progression;
        this.settingsReader = settingsReader;
        this.bakong = bakong;
        this.mockPayments = mockPayments;
    }
}
