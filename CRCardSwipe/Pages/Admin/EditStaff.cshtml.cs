using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCardSwipe.Services;
using Department = CRCardSwipe.Models.Entities.Department;
using StaffRecord = CRCardSwipe.Models.Entities.Staff;

namespace CRCardSwipe.Pages.Admin;

[Authorize(Policy = "RequireAdministrator")]
public class EditStaffModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;
    private readonly ILogger<EditStaffModel> _logger;

    public EditStaffModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService,
        ILogger<EditStaffModel> logger)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
        _logger = logger;
    }

    public IEnumerable<string> Roles { get; set; } = new List<string>();
    public IEnumerable<Department> Departments { get; set; } = new List<Department>();
    public string CurrentApplication { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
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
        if (string.IsNullOrWhiteSpace(NetId))
            return RedirectToPage("ManageStaff");

        CurrentApplication = _appContextService.GetCurrentApplication();

        // Load dropdowns
        Roles = await _storedProcService.GetAllRolesAsync(CurrentApplication);
        Departments = await _storedProcService.GetDepartmentsAsync(CurrentApplication);

        // Pre-populate termination date from staff list
        var allStaff = await _storedProcService.GetAllStaffAsync(CurrentApplication);
        var staffMember = allStaff.FirstOrDefault(s =>
            string.Equals(s.NetId, NetId, StringComparison.OrdinalIgnoreCase));
        if (staffMember != null)
            TerminationDate = staffMember.TerminationDate;

        // Pre-populate role (only returned for non-terminated staff by the procedure)
        var currentRoles = await _storedProcService.GetUserRolesAsync(NetId, CurrentApplication);
        Role = currentRoles.FirstOrDefault();

        // Pre-populate department
        var currentDept = await _storedProcService.GetDepartmentForStaffAsync(NetId, CurrentApplication);
        DeptId = currentDept?.DeptId.ToString();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (string.IsNullOrWhiteSpace(NetId))
            return RedirectToPage("ManageStaff");

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
            StatusMessage = $"{NetId} has been updated.";
            IsSuccess = true;
            _logger.LogInformation("Staff {NetId} updated by {Admin} in application {Application}",
                NetId, User.Identity?.Name, CurrentApplication);
        }
        else
        {
            StatusMessage = $"Failed to update {NetId}. Please try again.";
            IsSuccess = false;
            _logger.LogWarning("Failed to update staff {NetId} by {Admin}", NetId, User.Identity?.Name);
        }

        return RedirectToPage("ManageStaff");
    }
}
