namespace InstituteManagement.Application.DTOs;

public sealed record ActivityDto(string Time, string Title, string Detail, string Tone = "blue", string NotificationCode = "");
