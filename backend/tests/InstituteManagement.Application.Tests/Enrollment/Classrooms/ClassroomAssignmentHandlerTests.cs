using InstituteManagement.Application.Common.Pagination;
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
            .Handle(new("501", departmentId, 1, new PageRequest(2, 60)), CancellationToken.None);

        Assert.Equal(("501", departmentId, 1, new PageRequest(2, 60)), service.Filters);
        Assert.Same(service.Items, result.Items);
    }

    private sealed class ClassroomAssignmentServiceSpy : IClassroomAssignmentService
    {
        public IReadOnlyList<EnrollmentItemDto> Items { get; } = [new(Guid.NewGuid(), new Dictionary<string, string>())];
        public (string? Search, Guid? DepartmentId, int? Year, PageRequest Page) Filters { get; private set; }

        public Task<PagedResult<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, PageRequest page, CancellationToken cancellationToken)
        {
            Filters = (search, departmentId, year, page);
            return Task.FromResult(PagedResult<EnrollmentItemDto>.Create(Items, page, Items.Count));
        }

        public Task<EnrollmentItemDto> UpdateAsync(Guid classroomId, Dictionary<string, string> values, CancellationToken cancellationToken) => throw new NotSupportedException();
        public Task<bool> RemoveAsync(Guid classroomId, CancellationToken cancellationToken) => throw new NotSupportedException();
    }
}
