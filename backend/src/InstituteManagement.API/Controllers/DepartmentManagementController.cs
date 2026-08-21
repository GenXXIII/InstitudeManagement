using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[Route("api/catalog/departments")]
public sealed class DepartmentManagementController(ISender sender) : ManagementControllerBase(sender, "departments");
