using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCHTime.Models.Entities;
using CRCHTime.Services;

namespace CRCHTime.Pages.Admin;

[Authorize(Policy = "RequireAdministrator")]
public class AddDepartmentModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;
    private readonly ILogger<AddDepartmentModel> _logger;

    public AddDepartmentModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService,
        ILogger<AddDepartmentModel> logger)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
        _logger = logger;
    }

    [BindProperty]
    public Department Department { get; set; } = new();

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Department.Application = _appContextService.GetCurrentApplication();
        Department.Inactive = false;
        ModelState.Remove("Department.Application");
        ModelState.Remove("Department.Inactive");

        if (!ModelState.IsValid)
            return Page();

        var netId = User.Identity?.Name ?? "unknown";

        var result = await _storedProcService.AddUpdateDepartmentAsync(Department, netId);

        if (result.Success)
        {
            _logger.LogInformation("Department '{Name}' added by {NetId}", Department.Name, netId);
            return RedirectToPage("/Admin/ManageDepartments");
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "An error occurred.");
        return Page();
    }
}
