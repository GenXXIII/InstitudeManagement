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
            .Handle(new("database", departmentId, 2), CancellationToken.None);

        Assert.Equal(("database", departmentId, 2), service.Filters);
        Assert.Same(service.Items, result);
    }

    private sealed class CourseAssignmentServiceSpy : ICourseAssignmentService
    {
        public IReadOnlyList<EnrollmentItemDto> Items { get; } = [new(Guid.NewGuid(), new Dictionary<string, string>())];
        public (string? Search, Guid? DepartmentId, int? Year) Filters { get; private set; }

        public Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, CancellationToken cancellationToken)
        {
            Filters = (search, departmentId, year);
            return Task.FromResult(Items);
        }

        public Task<EnrollmentItemDto> UpdateAsync(Guid courseId, Dictionary<string, string> values, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RemoveAsync(Guid courseId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
