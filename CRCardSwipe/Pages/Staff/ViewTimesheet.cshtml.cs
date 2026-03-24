using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCardSwipe.Models.Entities;
using CRCardSwipe.Services;

namespace CRCardSwipe.Pages.Staff;

[Authorize(Policy = "RequireViewer")]
public class ViewTimesheetModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;

    public ViewTimesheetModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-7);

    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.Today;

    public string NetId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public IEnumerable<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();
    public double TotalHours => Entries.Where(e => e.HoursWorked.HasValue).Sum(e => e.HoursWorked!.Value);

    public async Task<IActionResult> OnGetAsync()
    {
        NetId = User.Identity?.Name ?? string.Empty;
        DisplayName = User.Claims.FirstOrDefault(c => c.Type == "DisplayName")?.Value ?? NetId;
        var currentApplication = _appContextService.GetCurrentApplication();

        Entries = await _storedProcService.GetTimecardAsync(StartDate, EndDate, NetId, null, currentApplication);

        return Page();
    }
}
