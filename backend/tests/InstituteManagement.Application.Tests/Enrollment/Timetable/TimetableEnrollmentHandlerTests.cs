using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Application.Features.Enrollment.Timetable;
using InstituteManagement.Application.Features.Enrollment.Timetable.GetTimetableEnrollments;

namespace InstituteManagement.Application.Tests.Enrollment.Timetable;

public sealed class TimetableEnrollmentHandlerTests
{
    [Fact]
    public async Task Get_forwards_timetable_filters()
    {
        var service = new TimetableEnrollmentServiceSpy();
        var departmentId = Guid.NewGuid();

        var result = await new GetTimetableEnrollmentsHandler(service)
            .Handle(new("monday", departmentId, 3), CancellationToken.None);

        Assert.Equal(("monday", departmentId, 3), service.Filters);
        Assert.Same(service.Items, result);
    }

    private sealed class TimetableEnrollmentServiceSpy : ITimetableEnrollmentService
    {
        public IReadOnlyList<EnrollmentItemDto> Items { get; } = [new(Guid.NewGuid(), new Dictionary<string, string>())];
        public (string? Search, Guid? DepartmentId, int? Year) Filters { get; private set; }

        public Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, CancellationToken cancellationToken)
        {
            Filters = (search, departmentId, year);
            return Task.FromResult(Items);
        }

        public Task<EnrollmentItemDto> UpdateAsync(Guid scheduleEntryId, Dictionary<string, string> values, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RemoveAsync(Guid scheduleEntryId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
