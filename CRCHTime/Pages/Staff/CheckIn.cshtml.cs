using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCHTime.Models.Entities;
using CRCHTime.Services;

namespace CRCHTime.Pages.Staff;

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
    public IEnumerable<ShiftCategory> ShiftCategories { get; set; } = new List<ShiftCategory>();
    public IList<Models.Entities.Department> Departments { get; set; } = [];
    public string CurrentApplication { get; set; } = string.Empty;

    [BindProperty]
    public int? DepartmentId { get; set; }

    [BindProperty]
    public int? ShiftCategoryId { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool IsSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        NetId = User.Identity?.Name ?? string.Empty;
        DisplayName = User.Claims.FirstOrDefault(c => c.Type == "DisplayName")?.Value ?? NetId;
        CurrentApplication = _appContextService.GetCurrentApplication();

        ShiftCategories = await _storedProcService.GetShiftCategoriesAsync(CurrentApplication);
        Departments = (await _storedProcService.GetDepartmentsAsync(CurrentApplication)).ToList();

        var defaultDept = await _storedProcService.GetDepartmentForStaffAsync(NetId, CurrentApplication);
        if (defaultDept != null)
            DepartmentId = defaultDept.DeptId;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        NetId = User.Identity?.Name ?? string.Empty;
        DisplayName = User.Claims.FirstOrDefault(c => c.Type == "DisplayName")?.Value ?? NetId;
        CurrentApplication = _appContextService.GetCurrentApplication();

        ShiftCategories = await _storedProcService.GetShiftCategoriesAsync(CurrentApplication);
        Departments = (await _storedProcService.GetDepartmentsAsync(CurrentApplication)).ToList();

        if (Departments.Any() && DepartmentId == null)
            ModelState.AddModelError(nameof(DepartmentId), "Please select a location.");
        if (ShiftCategories.Any() && ShiftCategoryId == null)
            ModelState.AddModelError(nameof(ShiftCategoryId), "Please select a shift category.");

        if (!ModelState.IsValid)
            return Page();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var hostname = await ResolveHostnameAsync(ip);

        var result = await _storedProcService.StaffCheckinAsync(
            NetId,
            hostname,
            ip,
            CurrentApplication,
            DepartmentId,
            ShiftCategoryId);

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

    private static async Task<string> ResolveHostnameAsync(string ip)
    {
        try
        {
            var entry = await System.Net.Dns.GetHostEntryAsync(ip);
            return entry.HostName;
        }
        catch
        {
            return ip;
        }
    }
}
