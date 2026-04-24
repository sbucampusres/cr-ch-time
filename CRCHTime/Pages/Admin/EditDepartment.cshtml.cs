using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCHTime.Models.Entities;
using CRCHTime.Services;

namespace CRCHTime.Pages.Admin;

[Authorize(Policy = "RequireAdministrator")]
public class EditDepartmentModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;
    private readonly ILogger<EditDepartmentModel> _logger;

    public EditDepartmentModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService,
        ILogger<EditDepartmentModel> logger)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
        _logger = logger;
    }

    [BindProperty]
    public Department Department { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var application = _appContextService.GetCurrentApplication();
        var departments = await _storedProcService.GetAllDepartmentsAdminAsync(application);
        var department = departments.FirstOrDefault(d => d.DeptId == id);

        if (department is null)
            return NotFound();

        Department = department;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (id <= 0)
            return NotFound();

        Department.DeptId = id;
        Department.Application = _appContextService.GetCurrentApplication();
        ModelState.Remove("Department.DeptId");
        ModelState.Remove("Department.Application");

        if (!ModelState.IsValid)
            return Page();

        var netId = User.Identity?.Name ?? "unknown";

        var result = await _storedProcService.AddUpdateDepartmentAsync(Department, netId);

        if (result.Success)
        {
            _logger.LogInformation("Department {Id} updated by {NetId}", Department.DeptId, netId);
            return RedirectToPage("/Admin/ManageDepartments");
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "An error occurred.");
        return Page();
    }
}
