using InstituteManagement.Application.Features.Enrollment;
using InstituteManagement.Application.Features.Enrollment.Students;
using InstituteManagement.Application.Features.Enrollment.Students.GetStudentEnrollments;
using InstituteManagement.Application.Features.Enrollment.Students.RemoveStudentEnrollment;
using InstituteManagement.Application.Features.Enrollment.Students.UpdateStudentEnrollment;

namespace InstituteManagement.Application.Tests.Enrollment.Students;

public sealed class StudentEnrollmentHandlerTests
{
    [Fact]
    public async Task Get_forwards_student_filters()
    {
        var service = new StudentEnrollmentServiceSpy();
        var departmentId = Guid.NewGuid();

        await new GetStudentEnrollmentsHandler(service)
            .Handle(new("kim", departmentId, 3), CancellationToken.None);

        Assert.Equal(("kim", departmentId, 3), service.Filters);
    }

    [Fact]
    public async Task Update_forwards_student_id_and_values()
    {
        var service = new StudentEnrollmentServiceSpy();
        var studentId = Guid.NewGuid();
        var values = new Dictionary<string, string> { ["year"] = "2" };

        var result = await new UpdateStudentEnrollmentHandler(service)
            .Handle(new(studentId, values), CancellationToken.None);

        Assert.Equal(studentId, service.UpdatedId);
        Assert.Same(values, service.UpdatedValues);
        Assert.Equal(service.Result, result);
    }

    [Fact]
    public async Task Remove_forwards_student_id()
    {
        var service = new StudentEnrollmentServiceSpy();
        var studentId = Guid.NewGuid();

        var result = await new RemoveStudentEnrollmentHandler(service)
            .Handle(new(studentId), CancellationToken.None);

        Assert.True(result);
        Assert.Equal(studentId, service.RemovedId);
    }

    private sealed class StudentEnrollmentServiceSpy : IStudentEnrollmentService
    {
        public EnrollmentItemDto Result { get; } = new(Guid.NewGuid(), new Dictionary<string, string>());
        public (string? Search, Guid? DepartmentId, int? Year) Filters { get; private set; }
        public Guid? UpdatedId { get; private set; }
        public Dictionary<string, string>? UpdatedValues { get; private set; }
        public Guid? RemovedId { get; private set; }

        public Task<IReadOnlyList<EnrollmentItemDto>> GetAsync(string? search, Guid? departmentId, int? year, CancellationToken cancellationToken)
        {
            Filters = (search, departmentId, year);
            return Task.FromResult<IReadOnlyList<EnrollmentItemDto>>([Result]);
        }

        public Task<EnrollmentItemDto> UpdateAsync(Guid studentId, Dictionary<string, string> values, CancellationToken cancellationToken)
        {
            UpdatedId = studentId;
            UpdatedValues = values;
            return Task.FromResult(Result);
        }

        public Task<bool> RemoveAsync(Guid studentId, CancellationToken cancellationToken)
        {
            RemovedId = studentId;
            return Task.FromResult(true);
        }
    }
}
