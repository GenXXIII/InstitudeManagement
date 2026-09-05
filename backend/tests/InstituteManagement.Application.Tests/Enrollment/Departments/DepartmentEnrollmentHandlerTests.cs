using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Application.Features.Enrollment.Departments;
using InstituteManagement.Application.Features.Enrollment.Departments.GetEnrollmentDepartments;

namespace InstituteManagement.Application.Tests.Enrollment.Departments;

public sealed class DepartmentEnrollmentHandlerTests
{
    [Fact]
    public async Task Get_forwards_department_filters()
    {
        var service = new DepartmentEnrollmentServiceSpy();
        var departmentId = Guid.NewGuid();

        var result = await new GetEnrollmentDepartmentsHandler(service)
            .Handle(new("technology", departmentId, 2), CancellationToken.None);

        Assert.Equal(("technology", departmentId, 2), service.Filters);
        Assert.Same(service.Items, result);
    }

    private sealed class DepartmentEnrollmentServiceSpy : IDepartmentEnrollmentService
    {
        public IReadOnlyList<EnrollmentItemDto> Items { get; } = [new(Guid.NewGuid(), new Dictionary<string, string>())];
        public (string? Search, Guid? DepartmentId, int? Year) Filters { get; private set; }

        public Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, CancellationToken cancellationToken)
        {
            Filters = (search, departmentId, year);
            return Task.FromResult(Items);
        }
    }
}
