using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[Route("api/catalog/students")]
public sealed class StudentManagementController(ISender sender) : ManagementControllerBase(sender, "students");
