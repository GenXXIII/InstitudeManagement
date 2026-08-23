using InstituteManagement.Application.Abstractions;
using InstituteManagement.Infrastructure.Persistence;
using InstituteManagement.Infrastructure.Services.Administration;
using InstituteManagement.Infrastructure.Services.Attendance;
using InstituteManagement.Infrastructure.Services.Common;
using InstituteManagement.Infrastructure.Services.Dashboard;
using InstituteManagement.Infrastructure.Services.History;
using InstituteManagement.Infrastructure.Services.Grades;
using InstituteManagement.Infrastructure.Services.Management;
using InstituteManagement.Infrastructure.Services.Management.Attendance;
using InstituteManagement.Infrastructure.Services.Management.Classrooms;
using InstituteManagement.Infrastructure.Services.Management.Courses;
using InstituteManagement.Infrastructure.Services.Management.Departments;
using InstituteManagement.Infrastructure.Services.Management.Grades;
using InstituteManagement.Infrastructure.Services.Management.Students;
using InstituteManagement.Infrastructure.Services.Management.Teachers;
using InstituteManagement.Infrastructure.Services.Management.Timetable;
using InstituteManagement.Infrastructure.Services.Notifications;
using InstituteManagement.Infrastructure.Services.Operations;
using InstituteManagement.Infrastructure.Services.Record;
using InstituteManagement.Infrastructure.Services.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;

namespace InstituteManagement.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        if (bool.TryParse(configuration["Database:UseInMemory"], out var useInMemory) && useInMemory)
            services.AddDbContext<InstituteDbContext>(options => options.UseInMemoryDatabase("InstituteManagement"));
        else
        {
            services.AddDbContext<InstituteDbContext>(options => options.UseSqlServer(configuration.GetConnectionString("Database"), sql => sql.EnableRetryOnFailure()));
            var redisConnection = configuration.GetConnectionString("Redis");
            if (!string.IsNullOrWhiteSpace(redisConnection))
                services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConnection));
        }

        services.AddScoped<InstituteCache>();
        services.AddScoped<IDashboardQueryService, DashboardQueryService>();
        services.AddScoped<IOperationQueryService, OperationQueryService>();
        services.AddScoped<OperationContextService>();
        services.AddScoped<IOperationModuleReader, DashboardOperationReader>();
        services.AddScoped<IOperationModuleReader, StudentOperationReader>();
        services.AddScoped<IOperationModuleReader, TeacherOperationReader>();
        services.AddScoped<IOperationModuleReader, ClassroomOperationReader>();
        services.AddScoped<IOperationModuleReader, CourseOperationReader>();
        services.AddScoped<IOperationModuleReader, TimetableOperationReader>();
        services.AddScoped<IOperationalRecordQueryService, OperationalRecordQueryService>();
        services.AddScoped<IOperationalRecordEditService, OperationalRecordEditService>();
        services.AddScoped<ClassSessionRecorderService>();
        services.AddHostedService<ClassSessionRecorderHostedService>();
        services.AddScoped<IOperationalRecordReader, StudentOperationalRecordReader>();
        services.AddScoped<IOperationalRecordReader, TeacherOperationalRecordReader>();
        services.AddScoped<IOperationalRecordReader, ClassroomOperationalRecordReader>();
        services.AddScoped<IOperationalRecordReader, CourseOperationalRecordReader>();
        services.AddScoped<IOperationalRecordReader, ClassSessionOperationalRecordReader>();
        services.AddScoped<IHistoryQueryService, HistoryQueryService>();
        services.AddScoped<IResultQueryService, ResultQueryService>();
        services.AddScoped<IHistorySnapshotProvider, StudentHistorySnapshotProvider>();
        services.AddScoped<IHistorySnapshotProvider, TeacherHistorySnapshotProvider>();
        services.AddScoped<IHistorySnapshotProvider, ClassroomHistorySnapshotProvider>();
        services.AddScoped<IHistorySnapshotProvider, CourseHistorySnapshotProvider>();
        services.AddScoped<IHistorySnapshotProvider, TimetableHistorySnapshotProvider>();
        services.AddScoped<IHistorySnapshotProvider, AttendanceHistorySnapshotProvider>();
        services.AddScoped<IHistorySnapshotProvider, DepartmentHistorySnapshotProvider>();
        services.AddScoped<IHistorySnapshotProvider, GradeHistorySnapshotProvider>();
        services.AddScoped<IManagementService, ManagementService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<INotificationCenterService, NotificationCenterService>();
        services.AddScoped<AcademicCalendarRolloverService>();
        services.AddHostedService<AcademicCalendarHostedService>();
        services.AddScoped<IAttendanceService, AttendanceService>();
        services.AddScoped<IGradeService, GradeService>();
        services.AddScoped<IManagementFeature, StudentManagementFeature>();
        services.AddScoped<IManagementFeature, TeacherManagementFeature>();
        services.AddScoped<IManagementFeature, ClassroomManagementFeature>();
        services.AddScoped<IManagementFeature, CourseManagementFeature>();
        services.AddScoped<IManagementFeature, TimetableManagementFeature>();
        services.AddScoped<IManagementFeature, AttendanceManagementFeature>();
        services.AddScoped<IManagementFeature, DepartmentManagementFeature>();
        services.AddScoped<IManagementFeature, GradeManagementFeature>();
        return services;
    }
}
