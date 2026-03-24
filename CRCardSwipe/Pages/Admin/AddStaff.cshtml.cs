using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCardSwipe.Services;
using Department = CRCardSwipe.Models.Entities.Department;
using StaffRecord = CRCardSwipe.Models.Entities.Staff;

namespace CRCardSwipe.Pages.Admin;

[Authorize(Policy = "RequireAdministrator")]
public class AddStaffModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;
    private readonly UserLookupService _userLookupService;
    private readonly ILogger<AddStaffModel> _logger;

    public AddStaffModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService,
        UserLookupService userLookupService,
        ILogger<AddStaffModel> logger)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
        _userLookupService = userLookupService;
        _logger = logger;
    }

    public IEnumerable<string> Roles { get; set; } = new List<string>();
    public IEnumerable<Department> Departments { get; set; } = new List<Department>();
    public string CurrentApplication { get; set; } = string.Empty;

    [BindProperty]
    public string NetId { get; set; } = string.Empty;

    [BindProperty]
    public string? Role { get; set; }

    [BindProperty]
    public string? DeptId { get; set; }

    [BindProperty]
    public DateTime? TerminationDate { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool IsSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentApplication = _appContextService.GetCurrentApplication();
        Roles = await _storedProcService.GetAllRolesAsync(CurrentApplication);
        Departments = await _storedProcService.GetDepartmentsAsync(CurrentApplication);
        return Page();
    }

    public async Task<IActionResult> OnGetLookupNetIdAsync(string netId)
    {
        if (string.IsNullOrWhiteSpace(netId))
            return new JsonResult(new { error = "NetID is required" });

        var result = await _userLookupService.LookupUserByNetIdAsync(netId.Trim().ToLower());
        if (result == null)
            return new JsonResult(new { error = "NetID not found in directory" });

        return new JsonResult(new { name = result.NAME, email = result.EMAIL_ADDR });
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(NetId))
        {
            StatusMessage = "NetID is required.";
            IsSuccess = false;
            return RedirectToPage();
        }

        CurrentApplication = _appContextService.GetCurrentApplication();

        var staff = new StaffRecord
        {
            NetId = NetId.Trim().ToLower(),
            Application = CurrentApplication,
            Role = Role,
            DeptId = DeptId,
            TerminationDate = TerminationDate,
            Hostname = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        };

        var success = await _storedProcService.AddUpdateStaffAsync(staff);

        if (success)
        {
            StatusMessage = $"{staff.NetId} has been added to the system.";
            IsSuccess = true;
            _logger.LogInformation("Staff {NetId} added by {Admin} in application {Application}",
                staff.NetId, User.Identity?.Name, CurrentApplication);
            return RedirectToPage("ManageStaff");
        }

        StatusMessage = $"Failed to add {staff.NetId}. Please try again.";
        IsSuccess = false;
        _logger.LogWarning("Failed to add staff {NetId} by {Admin}", staff.NetId, User.Identity?.Name);

        // Reload dropdowns for re-render
        Roles = await _storedProcService.GetAllRolesAsync(CurrentApplication);
        Departments = await _storedProcService.GetDepartmentsAsync(CurrentApplication);
        return Page();
    }
}
