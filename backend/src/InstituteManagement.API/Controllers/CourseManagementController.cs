using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[Route("api/catalog/courses")]
public sealed class CourseManagementController(ISender sender) : ManagementControllerBase(sender, "courses");
