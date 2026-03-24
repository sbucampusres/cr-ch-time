using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCardSwipe.Models.Entities;
using CRCardSwipe.Services;

namespace CRCardSwipe.Pages.Staff;

[Authorize(Policy = "RequireOperator")]
public class CheckInModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;
    private readonly ILogger<CheckInModel> _logger;

    public CheckInModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService,
        ILogger<CheckInModel> logger)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
        _logger = logger;
    }

    public string NetId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public IEnumerable<Department> Departments { get; set; } = new List<Department>();
    public string CurrentApplication { get; set; } = string.Empty;
    public bool RequiresDepartment { get; set; }

    [BindProperty]
    public int? DepartmentId { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool IsSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        NetId = User.Identity?.Name ?? string.Empty;
        DisplayName = User.Claims.FirstOrDefault(c => c.Type == "DisplayName")?.Value ?? NetId;
        CurrentApplication = _appContextService.GetCurrentApplication();

        // Check if Conference Housing (requires department selection)
        var appInfo = _appContextService.GetApplicationInfo(CurrentApplication);
        RequiresDepartment = CurrentApplication == "CH";

        if (RequiresDepartment)
        {
            Departments = await _storedProcService.GetDepartmentsAsync(CurrentApplication);
        }

        // Don't pre-check if already checked in - let the procedure handle it
        // The procedure will return an error message if already checked in

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        NetId = User.Identity?.Name ?? string.Empty;
        DisplayName = User.Claims.FirstOrDefault(c => c.Type == "DisplayName")?.Value ?? NetId;
        CurrentApplication = _appContextService.GetCurrentApplication();

        // Validate department for Conference Housing
        if (CurrentApplication == "CH" && !DepartmentId.HasValue)
        {
            StatusMessage = "Please select a department.";
            IsSuccess = false;
            return RedirectToPage();
        }

        // Perform check-in - let CRFCCS_STAFF_CHECKIN procedure handle validation
        var hostname = Environment.MachineName;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";

        var result = await _storedProcService.StaffCheckinAsync(
            NetId,
            hostname,
            ip,
            CurrentApplication,
            DepartmentId);

        if (result.Success)
        {
            StatusMessage = $"Successfully checked in at {DateTime.Now:h:mm tt}";
            IsSuccess = true;
            _logger.LogInformation("Staff {NetId} checked in for application {Application}", NetId, CurrentApplication);
        }
        else
        {
            StatusMessage = result.ErrorMessage;
            IsSuccess = false;
            _logger.LogWarning("Failed to check in staff {NetId}: {Error}", NetId, result.ErrorMessage);
        }

        return RedirectToPage();
    }
}
