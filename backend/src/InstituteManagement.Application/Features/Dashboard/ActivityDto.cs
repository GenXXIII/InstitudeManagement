namespace InstituteManagement.Application.Features.Dashboard;

public sealed record ActivityDto(string Time, string Title, string Detail, string Tone = "blue", string NotificationCode = "");
