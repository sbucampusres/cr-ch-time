using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCardSwipe.Models.ViewModels;
using CRCardSwipe.Services;
using System.Text.Json;

namespace CRCardSwipe.Pages;

[Authorize(Policy = "RequireViewer")]
public class IndexModel : PageModel
{
    private readonly IApplicationContextService _appContextService;
    private readonly IStoredProcService _storedProcService;

    public IndexModel(IApplicationContextService appContextService, IStoredProcService storedProcService)
    {
        _appContextService = appContextService;
        _storedProcService = storedProcService;
    }

    private static readonly JsonSerializerOptions _camelCase = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public string CurrentApplication { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;

    // Chart data serialized to JSON for inline script use
    public string VisitsByHostJson { get; set; } = "[]";
    public string DailyVisitsJson { get; set; } = "[]";

    public async Task OnGetAsync()
    {
        CurrentApplication = _appContextService.GetCurrentApplication();
        var appInfo = _appContextService.GetApplicationInfo(CurrentApplication);
        ApplicationName = appInfo?.Name ?? CurrentApplication;

        var prevMonth = DateTime.Today.AddMonths(-1);

        var byHost = await _storedProcService.GetVisitsByHostAsync(prevMonth.Year, prevMonth.Month, CurrentApplication);
        VisitsByHostJson = JsonSerializer.Serialize(byHost, _camelCase);

        var dailyEnd = DateTime.Today;
        var dailyStart = dailyEnd.AddDays(-89); // 90-day window
        var daily = await _storedProcService.GetDailyVisitsAsync(dailyStart, dailyEnd, CurrentApplication);
        DailyVisitsJson = JsonSerializer.Serialize(daily, _camelCase);
    }

    // AJAX handler: ?handler=VisitsByHost&year=2025&month=3
    public async Task<IActionResult> OnGetVisitsByHostAsync(int year, int month)
    {
        var application = _appContextService.GetCurrentApplication();
        var data = await _storedProcService.GetVisitsByHostAsync(year, month, application);
        return new JsonResult(data, _camelCase);
    }
}
