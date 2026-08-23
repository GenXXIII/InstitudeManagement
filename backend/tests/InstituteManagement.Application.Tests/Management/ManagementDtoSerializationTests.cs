using System.Text.Json;
using InstituteManagement.Application.DTOs.Management;
using InstituteManagement.Application.DTOs.Management.Students;
using InstituteManagement.Application.DTOs.Management.Classrooms;

namespace InstituteManagement.Application.Tests.Management;

public sealed class ManagementDtoSerializationTests
{
    [Fact]
    public void Resource_specific_values_are_preserved_through_shared_management_boundary()
    {
        IReadOnlyList<IManagementItemDto> items =
        [
            new StudentResponseDto(
                Guid.NewGuid(),
                new StudentValuesDto("photo", "ST-001", "Sok Dara", "sok@example.edu", Guid.NewGuid().ToString(), "IT", "2", "Morning", "Active", "2026-08-22"))
        ];

        var json = JsonSerializer.Serialize(items);

        Assert.Contains("\"StudentCode\":\"ST-001\"", json);
        Assert.Contains("\"Shift\":\"Morning\"", json);
        Assert.Contains("\"CreateAt\":\"2026-08-22\"", json);
        Assert.Contains("\"Values\"", json);
    }

    [Fact]
    public void Classroom_contract_preserves_learning_space_type()
    {
        IManagementItemDto item = new ClassroomResponseDto(
            Guid.NewGuid(),
            new ClassroomValuesDto("501", "Main Building", "Meeting Room", Guid.NewGuid().ToString(), "IT", "50", "Available", "In Study", "true", "2026-08-22"));

        var json = JsonSerializer.Serialize(item);

        Assert.Contains("\"RoomType\":\"Meeting Room\"", json);
        Assert.Contains("\"ClassroomCode\":\"501\"", json);
        Assert.Contains("\"StudyStatus\":\"In Study\"", json);
    }
}
