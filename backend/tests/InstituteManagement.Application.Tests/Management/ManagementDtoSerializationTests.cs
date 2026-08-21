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
                new StudentValuesDto("photo", "ST-001", "Sok Dara", "sok@example.edu", Guid.NewGuid().ToString(), "IT", "2", "Active"))
        ];

        var json = JsonSerializer.Serialize(items);

        Assert.Contains("\"Number\":\"ST-001\"", json);
        Assert.Contains("\"Values\"", json);
    }

    [Fact]
    public void Classroom_contract_preserves_learning_space_type()
    {
        IManagementItemDto item = new ClassroomResponseDto(
            Guid.NewGuid(),
            new ClassroomValuesDto("501", "Main Building", "Meeting Room", Guid.NewGuid().ToString(), "IT", "50", "Available", "In Study", "true"));

        var json = JsonSerializer.Serialize(item);

        Assert.Contains("\"RoomType\":\"Meeting Room\"", json);
        Assert.Contains("\"StudyStatus\":\"In Study\"", json);
    }
}
