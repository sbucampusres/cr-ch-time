using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCHTime.Models.Entities;
using CRCHTime.Services;

namespace CRCHTime.Pages.Admin;

[Authorize(Policy = "RequireAdministrator")]
public class ManageDepartmentsModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;
    private readonly ILogger<ManageDepartmentsModel> _logger;

    public ManageDepartmentsModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService,
        ILogger<ManageDepartmentsModel> logger)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
        _logger = logger;
    }

    public string CurrentApplication { get; set; } = string.Empty;
    public IList<Department> Departments { get; set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool IsSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentApplication = _appContextService.GetCurrentApplication();
        Departments = (await _storedProcService.GetAllDepartmentsAdminAsync(CurrentApplication)).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostDeactivateAsync(int id)
    {
        var netId = User.Identity?.Name ?? "unknown";
        CurrentApplication = _appContextService.GetCurrentApplication();

        var result = await _storedProcService.DeactivateDepartmentAsync(id, netId);

        if (result.Success)
        {
            StatusMessage = "Department deactivated.";
            IsSuccess = true;
            _logger.LogInformation("Department {Id} deactivated by {NetId}", id, netId);
        }
        else
        {
            StatusMessage = result.ErrorMessage;
            IsSuccess = false;
        }

        return RedirectToPage();
    }
}
