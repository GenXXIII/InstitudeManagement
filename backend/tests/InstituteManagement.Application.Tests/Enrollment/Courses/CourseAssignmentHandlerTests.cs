using InstituteManagement.Application.Common.Pagination;
using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Application.Features.Enrollment.Courses;
using InstituteManagement.Application.Features.Enrollment.Courses.GetCourseAssignments;

namespace InstituteManagement.Application.Tests.Enrollment.Courses;

public sealed class CourseAssignmentHandlerTests
{
    [Fact]
    public async Task Get_forwards_course_filters()
    {
        var service = new CourseAssignmentServiceSpy();
        var departmentId = Guid.NewGuid();

        var result = await new GetCourseAssignmentsHandler(service)
            .Handle(new("database", departmentId, 2, new PageRequest(2, 60)), CancellationToken.None);

        Assert.Equal(("database", departmentId, 2, new PageRequest(2, 60)), service.Filters);
        Assert.Same(service.Items, result.Items);
    }

    private sealed class CourseAssignmentServiceSpy : ICourseAssignmentService
    {
        public IReadOnlyList<EnrollmentItemDto> Items { get; } = [new(Guid.NewGuid(), new Dictionary<string, string>())];
        public (string? Search, Guid? DepartmentId, int? Year, PageRequest Page) Filters { get; private set; }

        public Task<PagedResult<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, PageRequest page, CancellationToken cancellationToken)
        {
            Filters = (search, departmentId, year, page);
            return Task.FromResult(PagedResult<EnrollmentItemDto>.Create(Items, page, Items.Count));
        }

        public Task<EnrollmentItemDto> UpdateAsync(Guid courseId, Dictionary<string, string> values, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RemoveAsync(Guid courseId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
