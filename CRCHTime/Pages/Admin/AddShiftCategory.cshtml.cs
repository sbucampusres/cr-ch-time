using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCHTime.Models.Entities;
using CRCHTime.Services;

namespace CRCHTime.Pages.Admin;

[Authorize(Policy = "RequireAdministrator")]
public class AddShiftCategoryModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;
    private readonly ILogger<AddShiftCategoryModel> _logger;

    public AddShiftCategoryModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService,
        ILogger<AddShiftCategoryModel> logger)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
        _logger = logger;
    }

    [BindProperty]
    public ShiftCategory Category { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool IsSuccess { get; set; }

    public IActionResult OnGet()
    {
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Application is set server-side, not from the form
        Category.Application = _appContextService.GetCurrentApplication();
        Category.IsActive = true;
        ModelState.Remove("Category.Application");
        ModelState.Remove("Category.IsActive");

        if (!ModelState.IsValid)
            return Page();

        var netId = User.Identity?.Name ?? "unknown";

        var result = await _storedProcService.AddUpdateShiftCategoryAsync(Category, netId);

        if (result.Success)
        {
            _logger.LogInformation("Shift category '{Name}' added by {NetId}", Category.Name, netId);
            StatusMessage = $"Shift category '{Category.Name}' added.";
            IsSuccess = true;
            return RedirectToPage("/Admin/ManageShiftCategories");
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "An error occurred.");
        return Page();
    }
}
