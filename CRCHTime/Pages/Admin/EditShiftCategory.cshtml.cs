using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCHTime.Models.Entities;
using CRCHTime.Services;

namespace CRCHTime.Pages.Admin;

[Authorize(Policy = "RequireAdministrator")]
public class EditShiftCategoryModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;
    private readonly ILogger<EditShiftCategoryModel> _logger;

    public EditShiftCategoryModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService,
        ILogger<EditShiftCategoryModel> logger)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
        _logger = logger;
    }

    [BindProperty]
    public ShiftCategory Category { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var application = _appContextService.GetCurrentApplication();
        var categories = await _storedProcService.GetShiftCategoriesAsync(application);
        var category = categories.FirstOrDefault(c => c.Id == id);

        if (category is null)
            return NotFound();

        Category = category;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Category.Application = _appContextService.GetCurrentApplication();
        ModelState.Remove("Category.Application");

        if (!ModelState.IsValid)
            return Page();

        var netId = User.Identity?.Name ?? "unknown";

        var result = await _storedProcService.AddUpdateShiftCategoryAsync(Category, netId);

        if (result.Success)
        {
            _logger.LogInformation("Shift category {Id} updated by {NetId}", Category.Id, netId);
            return RedirectToPage("/Admin/ManageShiftCategories");
        }

        ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "An error occurred.");
        return Page();
    }
}
