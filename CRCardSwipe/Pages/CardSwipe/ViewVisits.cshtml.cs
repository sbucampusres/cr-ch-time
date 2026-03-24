using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using CRCardSwipe.Models.Entities;
using CRCardSwipe.Services;

namespace CRCardSwipe.Pages.CardSwipe;

[Authorize(Policy = "RequireViewer")]
public class ViewVisitsModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;
    private readonly ILogger<ViewVisitsModel> _logger;

    public ViewVisitsModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService,
        ILogger<ViewVisitsModel> logger)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.Today;

    [BindProperty(SupportsGet = true)]
    public string? SBUID { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? BuildingId { get; set; }

    public IEnumerable<Visit> Visits { get; set; } = new List<Visit>();
    public IEnumerable<Building> Buildings { get; set; } = new List<Building>();
    public string CurrentApplication { get; set; } = string.Empty;
    public int TotalVisits => Visits.Count();
    public int UniqueVisitors => Visits.Select(v => v.SBUID).Distinct().Count();

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentApplication = _appContextService.GetCurrentApplication();
        await LoadPageDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync()
    {
        CurrentApplication = _appContextService.GetCurrentApplication();
        await LoadPageDataAsync();

        var sb = new StringBuilder();
        sb.AppendLine("Date/Time,SBUID,First Name,Last Name,Location,Note,Recorded By,Computer");
        foreach (var v in Visits)
        {
            sb.AppendLine(string.Join(",",
                CsvEscape(v.SwipeTime.ToString("MM/dd/yyyy h:mm tt")),
                CsvEscape(v.SBUID),
                CsvEscape(v.FirstName ?? ""),
                CsvEscape(v.LastName ?? ""),
                CsvEscape(v.Location ?? ""),
                CsvEscape(v.Note ?? ""),
                CsvEscape(v.NetIdAudit ?? ""),
                CsvEscape(v.Hostname ?? "")));
        }

        var fileName = $"visits_{CurrentApplication}_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.csv";
        return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", fileName);
    }

    private async Task LoadPageDataAsync()
    {
        Buildings = await _storedProcService.GetBuildingsAsync(CurrentApplication);

        string? location = null;
        if (BuildingId.HasValue)
        {
            var building = Buildings.FirstOrDefault(b => b.BuildingId == BuildingId);
            location = building?.Name;
        }

        Visits = await _storedProcService.GetVisitsAsync(StartDate, EndDate, SBUID, CurrentApplication, location);
    }

    private static string CsvEscape(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
