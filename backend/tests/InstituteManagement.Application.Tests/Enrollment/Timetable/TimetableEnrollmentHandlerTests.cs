using InstituteManagement.Application.Common.Pagination;
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
            .Handle(new("monday", departmentId, 3, new PageRequest(2, 60)), CancellationToken.None);

        Assert.Equal(("monday", departmentId, 3, new PageRequest(2, 60)), service.Filters);
        Assert.Same(service.Items, result.Items);
    }

    private sealed class TimetableEnrollmentServiceSpy : ITimetableEnrollmentService
    {
        public IReadOnlyList<EnrollmentItemDto> Items { get; } = [new(Guid.NewGuid(), new Dictionary<string, string>())];
        public (string? Search, Guid? DepartmentId, int? Year, PageRequest Page) Filters { get; private set; }

        public Task<PagedResult<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, PageRequest page, CancellationToken cancellationToken)
        {
            Filters = (search, departmentId, year, page);
            return Task.FromResult(PagedResult<EnrollmentItemDto>.Create(Items, page, Items.Count));
        }

        public Task<EnrollmentItemDto> UpdateAsync(Guid scheduleEntryId, Dictionary<string, string> values, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RemoveAsync(Guid scheduleEntryId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
