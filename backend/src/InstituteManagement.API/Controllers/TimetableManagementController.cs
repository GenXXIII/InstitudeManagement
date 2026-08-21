using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[Route("api/catalog/timetable")]
public sealed class TimetableManagementController(ISender sender) : ManagementControllerBase(sender, "timetable");
