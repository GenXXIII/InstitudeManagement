using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[Route("api/catalog/attendance")]
public sealed class AttendanceManagementController(ISender sender) : ManagementControllerBase(sender, "attendance");
