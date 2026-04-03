using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCHTime.Models.Entities;
using CRCHTime.Services;

namespace CRCHTime.Pages.Admin;

[Authorize(Policy = "RequireAdministrator")]
public class ManageAllotmentsModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;
    private readonly ILogger<ManageAllotmentsModel> _logger;

    public ManageAllotmentsModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService,
        ILogger<ManageAllotmentsModel> logger)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
        _logger = logger;
    }

    public string CurrentApplication { get; set; } = string.Empty;

    [BindProperty(SupportsGet = true)]
    public int Year { get; set; } = DateTime.Now.Year;

    [BindProperty]
    public List<AllotmentCell> Cells { get; set; } = [];

    public IList<Department> Departments { get; set; } = [];
    public IList<ShiftCategory> Categories { get; set; } = [];

    // Lookup populated on GET so the view can pre-fill inputs
    private Dictionary<(int DeptId, int CatId), decimal?> _existing = [];

    public decimal? GetExistingHours(int deptId, int catId)
        => _existing.TryGetValue((deptId, catId), out var h) ? h : null;

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool IsSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentApplication = _appContextService.GetCurrentApplication();
        await LoadGridDataAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        CurrentApplication = _appContextService.GetCurrentApplication();
        var user = User.Identity?.Name ?? "unknown";
        var errors = new List<string>();

        foreach (var cell in Cells)
        {
            var result = await _storedProcService.UpsertAllotmentAsync(
                CurrentApplication, Year, cell.DeptId, cell.CategoryId, cell.Hours, user);

            if (!result.Success)
                errors.Add($"Dept {cell.DeptId} / Cat {cell.CategoryId}: {result.ErrorMessage}");
        }

        if (errors.Count == 0)
        {
            StatusMessage = $"Allotments for {Year} saved successfully.";
            IsSuccess = true;
            _logger.LogInformation("Allotments saved for {Application} {Year} by {User}",
                CurrentApplication, Year, user);
        }
        else
        {
            StatusMessage = $"Saved with {errors.Count} error(s): {string.Join("; ", errors)}";
            IsSuccess = false;
        }

        return RedirectToPage(new { Year });
    }

    private async Task LoadGridDataAsync()
    {
        Departments = (await _storedProcService.GetAllDepartmentsAdminAsync(CurrentApplication))
            .Where(d => !d.Inactive)
            .OrderBy(d => d.Name)
            .ToList();

        Categories = (await _storedProcService.GetShiftCategoriesAsync(CurrentApplication))
            .OrderBy(c => c.Name)
            .ToList();

        var allotments = await _storedProcService.GetAllotmentsAsync(CurrentApplication, Year);
        _existing = allotments.ToDictionary(
            a => (a.DeptId, a.CategoryId),
            a => a.Hours);
    }

    public class AllotmentCell
    {
        public int DeptId { get; set; }
        public int CategoryId { get; set; }
        public decimal? Hours { get; set; }
    }
}
