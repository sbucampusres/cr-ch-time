using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text;
using CRCardSwipe.Models.Entities;
using CRCardSwipe.Services;

namespace CRCardSwipe.Pages.Admin;

[Authorize(Policy = "RequireAdministrator")]
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
    public int? FilterDeptId { get; set; }

    public string CurrentApplication { get; set; } = string.Empty;
    public IList<TimesheetEntry> Entries { get; set; } = [];
    public IList<Department> Departments { get; set; } = [];
    public Dictionary<int, string> DeptNames { get; set; } = [];

    // Per-person summary: NetId → total hours
    public IList<(string NetId, double TotalHours, int EntryCount)> Summary { get; set; } = [];

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
        var deptTask = _storedProcService.GetDepartmentsAsync(CurrentApplication);
        var entriesTask = _storedProcService.GetTimecardAsync(
            StartDate, EndDate,
            string.IsNullOrWhiteSpace(FilterNetId) ? null : FilterNetId.Trim().ToLower(),
            FilterDeptId,
            CurrentApplication);

        await Task.WhenAll(deptTask, entriesTask);

        Departments = (await deptTask).OrderBy(d => d.Name).ToList();
        DeptNames = Departments.ToDictionary(d => d.DeptId, d => d.Name);

        Entries = (await entriesTask).ToList();

        Summary = Entries
            .GroupBy(e => e.NetId)
            .Select(g => (
                NetId: g.Key,
                TotalHours: g.Where(e => e.HoursWorked.HasValue).Sum(e => e.HoursWorked!.Value),
                EntryCount: g.Count()
            ))
            .OrderBy(s => s.NetId)
            .ToList();
    }

    private string BuildCsv()
    {
        var sb = new StringBuilder();
        sb.AppendLine("NetID,Date,Check-In,In Computer,Check-Out,Out Computer,Hours,Department,Status");

        foreach (var e in Entries)
        {
            var dept = e.DepartmentId.HasValue && DeptNames.TryGetValue(e.DepartmentId.Value, out var dn) ? dn : "";
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
                CsvEscape(dept),
                status));
        }

        return sb.ToString();
    }

    private static string CsvEscape(string value) =>
        value.Contains(',') || value.Contains('"') || value.Contains('\n')
            ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
}
