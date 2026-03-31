using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCHTime.Models.Entities;
using CRCHTime.Services;

namespace CRCHTime.Pages.Admin;

[Authorize(Policy = "RequireAdministrator")]
public class ManageShiftCategoriesModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;
    private readonly ILogger<ManageShiftCategoriesModel> _logger;

    public ManageShiftCategoriesModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService,
        ILogger<ManageShiftCategoriesModel> logger)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
        _logger = logger;
    }

    public string CurrentApplication { get; set; } = string.Empty;
    public IList<ShiftCategory> Categories { get; set; } = [];

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool IsSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentApplication = _appContextService.GetCurrentApplication();
        Categories = (await _storedProcService.GetShiftCategoriesAsync(CurrentApplication)).ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostDeleteAsync(int id)
    {
        var netId = User.Identity?.Name ?? "unknown";
        CurrentApplication = _appContextService.GetCurrentApplication();

        var result = await _storedProcService.DeleteShiftCategoryAsync(id, netId);

        if (result.Success)
        {
            StatusMessage = "Shift category removed.";
            IsSuccess = true;
            _logger.LogInformation("Shift category {Id} deleted by {NetId}", id, netId);
        }
        else
        {
            StatusMessage = result.ErrorMessage;
            IsSuccess = false;
        }

        return RedirectToPage();
    }
}
