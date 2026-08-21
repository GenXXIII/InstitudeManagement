using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[Route("api/catalog/classrooms")]
public sealed class ClassroomManagementController(ISender sender) : ManagementControllerBase(sender, "classrooms");
