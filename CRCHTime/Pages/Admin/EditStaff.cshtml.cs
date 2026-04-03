using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCHTime.Services;
using StaffRecord = CRCHTime.Models.Entities.Staff;

namespace CRCHTime.Pages.Admin;

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

    public string CurrentApplication { get; set; } = string.Empty;

     [BindProperty(SupportsGet = true)]
    public string NetId { get; set; } = string.Empty;

     [BindProperty]
    public string? Role { get; set; }

     [BindProperty]
    public DateTime? TerminationDate { get; set; }

     [BindProperty]
    public int? DeptId { get; set; }

    public IList<Models.Entities.Department> Departments { get; set; } = [];

     [TempData]
    public string? StatusMessage { get; set; }

     [TempData]
    public bool IsSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
     {
        if (string.IsNullOrWhiteSpace(NetId))
            return RedirectToPage("ManageStaff");

        CurrentApplication = _appContextService.GetCurrentApplication();

        var allStaff = await _storedProcService.GetAllStaffAsync(CurrentApplication);
        var staffMember = allStaff.FirstOrDefault(s =>
            string.Equals(s.NetId, NetId, StringComparison.OrdinalIgnoreCase));
        if (staffMember != null)
        {
            TerminationDate = staffMember.TerminationDate;
            Role = staffMember.Role;
            if (int.TryParse(staffMember.DeptId, out int deptId))
                DeptId = deptId;
        }

        Departments = (await _storedProcService.GetAllDepartmentsAdminAsync(CurrentApplication)).ToList();

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
            TerminationDate = TerminationDate,
            DeptId = DeptId?.ToString(),
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
