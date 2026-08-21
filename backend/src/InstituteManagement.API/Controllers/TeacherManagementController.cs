using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[Route("api/catalog/teachers")]
public sealed class TeacherManagementController(ISender sender) : ManagementControllerBase(sender, "teachers");
