using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Application.Features.Enrollment.Classrooms;
using InstituteManagement.Application.Features.Enrollment.Classrooms.GetClassroomAssignments;

namespace InstituteManagement.Application.Tests.Enrollment.Classrooms;

public sealed class ClassroomAssignmentHandlerTests
{
    [Fact]
    public async Task Get_forwards_classroom_filters()
    {
        var service = new ClassroomAssignmentServiceSpy();
        var departmentId = Guid.NewGuid();

        var result = await new GetClassroomAssignmentsHandler(service)
            .Handle(new("501", departmentId, 1), CancellationToken.None);

        Assert.Equal(("501", departmentId, 1), service.Filters);
        Assert.Same(service.Items, result);
    }

    private sealed class ClassroomAssignmentServiceSpy : IClassroomAssignmentService
    {
        public IReadOnlyList<EnrollmentItemDto> Items { get; } = [new(Guid.NewGuid(), new Dictionary<string, string>())];
        public (string? Search, Guid? DepartmentId, int? Year) Filters { get; private set; }

        public Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, CancellationToken cancellationToken)
        {
            Filters = (search, departmentId, year);
            return Task.FromResult(Items);
        }

        public Task<EnrollmentItemDto> UpdateAsync(Guid classroomId, Dictionary<string, string> values, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RemoveAsync(Guid classroomId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
