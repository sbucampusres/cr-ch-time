using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCHTime.Models.Entities;
using CRCHTime.Services;

namespace CRCHTime.Pages.Admin;

[Authorize(Policy = "RequireAdministrator")]
public class AllotmentsReportModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;
    private readonly ILogger<AllotmentsReportModel> _logger;

    public AllotmentsReportModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService,
        ILogger<AllotmentsReportModel> logger)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
        _logger = logger;
    }

    public string CurrentApplication { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public int Year { get; set; } = DateTime.Now.Year;

    public IList<Department> Departments { get; set; } = [];
    public IList<ShiftCategory> Categories { get; set; } = [];

    private Dictionary<(int DeptId, int CatId), decimal?> _allotments = [];
    private Dictionary<(int DeptId, int CatId), double> _used = [];

    public decimal? GetAllotted(int deptId, int catId)
        => _allotments.TryGetValue((deptId, catId), out var h) ? h : null;

    public double GetUsed(int deptId, int catId)
        => _used.TryGetValue((deptId, catId), out var h) ? h : 0;

    /// Returns a Bootstrap text color class based on % of allotment consumed.
    public string GetCellClass(int deptId, int catId)
    {
        var allotted = GetAllotted(deptId, catId);
        if (allotted is null) return "text-muted";
        if (allotted.Value == 0) return "text-danger fw-bold";
        var pct = GetUsed(deptId, catId) / (double)allotted.Value;
        return pct >= 1.0 ? "text-danger fw-bold"
             : pct >= 0.75 ? "text-warning fw-semibold"
             : "text-success";
    }

    /// Returns a short status label for screen readers.
    public string GetCellStatus(int deptId, int catId)
    {
        var allotted = GetAllotted(deptId, catId);
        if (allotted is null) return "no allotment";
        if (allotted.Value == 0) return "zero allotment";
        var pct = GetUsed(deptId, catId) / (double)allotted.Value;
        return pct >= 1.0 ? "at or over limit"
             : pct >= 0.75 ? "approaching limit"
             : "within allotment";
    }

    public static string FormatHours(double hours)
    {
        var h = (int)Math.Floor(hours);
        var m = (int)Math.Round((hours - h) * 60);
        return m == 0 ? $"{h}h" : $"{h}h {m}m";
    }

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentApplication = _appContextService.GetCurrentApplication();

        Departments = (await _storedProcService.GetAllDepartmentsAdminAsync(CurrentApplication))
            .Where(d => !d.Inactive)
            .OrderBy(d => d.Name)
            .ToList();

        Categories = (await _storedProcService.GetShiftCategoriesAsync(CurrentApplication))
            .OrderBy(c => c.Name)
            .ToList();

        var allotmentRows = await _storedProcService.GetAllotmentsAsync(CurrentApplication, Year);
        _allotments = allotmentRows.ToDictionary(
            a => (a.DeptId, a.CategoryId),
            a => a.Hours);

        var usedRows = await _storedProcService.GetHoursUsedAsync(CurrentApplication, Year);
        _used = usedRows.ToDictionary(
            r => (r.DeptId, r.CategoryId),
            r => r.HoursUsed);

        return Page();
    }
}
