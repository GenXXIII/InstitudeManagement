using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Application.Features.Enrollment.Teachers;
using InstituteManagement.Application.Features.Enrollment.Teachers.GetTeacherAssignments;

namespace InstituteManagement.Application.Tests.Enrollment.Teachers;

public sealed class TeacherAssignmentHandlerTests
{
    [Fact]
    public async Task Get_forwards_teacher_filters()
    {
        var service = new TeacherAssignmentServiceSpy();
        var departmentId = Guid.NewGuid();

        var result = await new GetTeacherAssignmentsHandler(service)
            .Handle(new("sok", departmentId, 4), CancellationToken.None);

        Assert.Equal(("sok", departmentId, 4), service.Filters);
        Assert.Same(service.Items, result);
    }

    private sealed class TeacherAssignmentServiceSpy : ITeacherAssignmentService
    {
        public IReadOnlyList<EnrollmentItemDto> Items { get; } = [new(Guid.NewGuid(), new Dictionary<string, string>())];
        public (string? Search, Guid? DepartmentId, int? Year) Filters { get; private set; }

        public Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, CancellationToken cancellationToken)
        {
            Filters = (search, departmentId, year);
            return Task.FromResult(Items);
        }

        public Task<EnrollmentItemDto> UpdateAsync(Guid teacherId, Dictionary<string, string> values, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RemoveAsync(Guid teacherId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
