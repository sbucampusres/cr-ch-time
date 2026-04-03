using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using CRCHTime.Models.Entities;
using CRCHTime.Services;

namespace CRCHTime.Pages.Admin;

[Authorize(Policy = "RequireSupervisor")]
public class ViewTimesheetsModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;
    private readonly ILogger<ViewTimesheetsModel> _logger;

    public ViewTimesheetsModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService,
        ILogger<ViewTimesheetsModel> logger)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
        _logger = logger;
    }

    [BindProperty(SupportsGet = true)]
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-14);

    [BindProperty(SupportsGet = true)]
    public DateTime EndDate { get; set; } = DateTime.Today;

    [BindProperty(SupportsGet = true)]
    public string? FilterNetId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FilterCategoryId { get; set; }

    [BindProperty(SupportsGet = true)]
    public int? FilterDepartmentId { get; set; }

    public string CurrentApplication { get; set; } = string.Empty;
    public IList<TimesheetEntry> Entries { get; set; } = [];
    public IList<ShiftCategory> ShiftCategories { get; set; } = [];
    public IList<Department> Departments { get; set; } = [];

    // Per-person summary
    public IList<(string NetId, double TotalHours, int EntryCount)> Summary { get; set; } = [];

    // Per-category summary
    public IList<(string Category, double TotalHours, int EntryCount)> CategorySummary { get; set; } = [];

    public double GrandTotalHours => Summary.Sum(s => s.TotalHours);

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

        var csv = BuildCsv();
        var fileName = $"timesheets_{CurrentApplication}_{StartDate:yyyyMMdd}_{EndDate:yyyyMMdd}.csv";

        return File(Encoding.UTF8.GetBytes(csv), "text/csv", fileName);
    }

    private async Task LoadPageDataAsync()
    {
        var catTask = _storedProcService.GetShiftCategoriesAsync(CurrentApplication);
        var deptTask = _storedProcService.GetDepartmentsAsync(CurrentApplication);
        var entriesTask = _storedProcService.GetTimecardAsync(
            StartDate, EndDate,
            string.IsNullOrWhiteSpace(FilterNetId) ? null : FilterNetId.Trim().ToLower(),
            FilterDepartmentId,
            CurrentApplication);

        await Task.WhenAll(catTask, deptTask, entriesTask);

        ShiftCategories = (await catTask).OrderBy(c => c.Name).ToList();
        Departments = (await deptTask).Where(d => !d.Inactive).OrderBy(d => d.Name).ToList();

        var allEntries = await entriesTask;
        Entries = (FilterCategoryId.HasValue
            ? allEntries.Where(e => e.ShiftCategoryId == FilterCategoryId)
            : allEntries).ToList();

        Summary = Entries
            .GroupBy(e => e.NetId)
            .Select(g => (
                NetId: g.Key,
                TotalHours: g.Where(e => e.HoursWorked.HasValue).Sum(e => e.HoursWorked!.Value),
                EntryCount: g.Count()
            ))
            .OrderBy(s => s.NetId)
            .ToList();

        CategorySummary = Entries
            .GroupBy(e => e.ShiftCategoryName ?? "Uncategorized")
            .Select(g => (
                Category: g.Key,
                TotalHours: g.Where(e => e.HoursWorked.HasValue).Sum(e => e.HoursWorked!.Value),
                EntryCount: g.Count()
            ))
            .OrderBy(c => c.Category)
            .ToList();
    }

    private string BuildCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("NetID,Date,Check-In,In Computer,Check-Out,Out Computer,Hours,Location,Category,Status");

        foreach (var e in Entries)
        {
            var hours = e.HoursWorked.HasValue ? e.HoursWorked.Value.ToString("F2") : "";
            var status = e.IsCheckedIn ? "Active" : (e.HoursWorked > 12 ? "Long Shift" : "Complete");
            sb.AppendLine(string.Join(",",
                CsvEscape(e.NetId),
                e.CheckinTimestamp.ToString("MM/dd/yyyy"),
                e.CheckinTimestamp.ToString("h:mm tt"),
                CsvEscape(e.CheckinHostname ?? ""),
                e.CheckoutTimestamp?.ToString("h:mm tt") ?? "",
                CsvEscape(e.CheckoutHostname ?? ""),
                hours,
                CsvEscape(e.DepartmentName ?? ""),
                CsvEscape(e.ShiftCategoryName ?? ""),
                status));
        }

        return sb.ToString();
    }

    private static string CsvEscape(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
