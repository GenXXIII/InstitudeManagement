using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace InstituteManagement.API.Controllers;

[Route("api/catalog/grades")]
public sealed class GradeManagementController(ISender sender) : ManagementControllerBase(sender, "grades");
