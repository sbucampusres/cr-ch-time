using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCHTime.Services;
using StaffRecord = CRCHTime.Models.Entities.Staff;

namespace CRCHTime.Pages.Admin;

[Authorize(Policy = "RequireAdministrator")]
public class ManageStaffModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;
    private readonly UserLookupService _userLookupService;
    private readonly ILogger<ManageStaffModel> _logger;

    public record StaffDisplayItem(StaffRecord Staff, string? DisplayName, string? Role);

    public ManageStaffModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService,
        UserLookupService userLookupService,
        ILogger<ManageStaffModel> logger)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
        _userLookupService = userLookupService;
        _logger = logger;
    }

    public IList<StaffDisplayItem> StaffList { get; set; } = [];
    public string CurrentApplication { get; set; } = string.Empty;

    [BindProperty]
    public string TerminateNetId { get; set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool IsSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentApplication = _appContextService.GetCurrentApplication();
        var rawStaff = await _storedProcService.GetAllStaffAsync(CurrentApplication);

        var lookupTasks = rawStaff.Select(async s =>
        {
            var lookupTask = _userLookupService.LookupUserByNetIdAsync(s.NetId);
            var rolesTask  = _storedProcService.GetUserRolesAsync(s.NetId, CurrentApplication);
            await Task.WhenAll(lookupTask, rolesTask);
            return new StaffDisplayItem(s, lookupTask.Result?.NAME, rolesTask.Result.FirstOrDefault());
        });

        StaffList = await Task.WhenAll(lookupTasks);
        return Page();
    }

    public async Task<IActionResult> OnPostTerminateAsync()
    {
        if (string.IsNullOrWhiteSpace(TerminateNetId))
        {
            StatusMessage = "NetID is required to terminate a staff member.";
            IsSuccess = false;
            return RedirectToPage();
        }

        CurrentApplication = _appContextService.GetCurrentApplication();

        // Preserve current role and department when terminating
        var currentRoles = await _storedProcService.GetUserRolesAsync(TerminateNetId, CurrentApplication);
        var currentRole = currentRoles.FirstOrDefault();
        var currentDept = await _storedProcService.GetDepartmentForStaffAsync(TerminateNetId, CurrentApplication);

        var staff = new StaffRecord
        {
            NetId = TerminateNetId.Trim().ToLower(),
            Application = CurrentApplication,
            TerminationDate = DateTime.Today,
            Role = currentRole,
            DeptId = currentDept?.DeptId.ToString(),
            Hostname = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown"
        };

        var success = await _storedProcService.AddUpdateStaffAsync(staff);

        if (success)
        {
            StatusMessage = $"{TerminateNetId} has been terminated.";
            IsSuccess = true;
            _logger.LogInformation("Staff {NetId} terminated by {Admin} in application {Application}",
                TerminateNetId, User.Identity?.Name, CurrentApplication);
        }
        else
        {
            StatusMessage = $"Failed to terminate {TerminateNetId}. Please try again.";
            IsSuccess = false;
            _logger.LogWarning("Failed to terminate staff {NetId} by {Admin}", TerminateNetId, User.Identity?.Name);
        }

        return RedirectToPage();
    }
}
