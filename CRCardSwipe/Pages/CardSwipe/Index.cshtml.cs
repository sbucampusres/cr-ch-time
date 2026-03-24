using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCardSwipe.Models.Entities;
using CRCardSwipe.Models.ViewModels;
using CRCardSwipe.Services;

namespace CRCardSwipe.Pages.CardSwipe;

[Authorize(Policy = "RequireOperator")]
public class IndexModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly ICardSwipeService _cardSwipeService;
    private readonly IApplicationContextService _appContextService;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        IStoredProcService storedProcService,
        ICardSwipeService cardSwipeService,
        IApplicationContextService appContextService,
        ILogger<IndexModel> logger)
    {
        _storedProcService = storedProcService;
        _cardSwipeService = cardSwipeService;
        _appContextService = appContextService;
        _logger = logger;
    }

    [BindProperty]
    public string? CardData { get; set; }

    [BindProperty]
    public int? SelectedBuildingId { get; set; }

    [BindProperty]
    public string? Note { get; set; }

    public IEnumerable<Building> Buildings { get; set; } = new List<Building>();
    public IEnumerable<Visit> RecentVisits { get; set; } = new List<Visit>();
    public bool RequiresBuildingSelection { get; set; }
    public string CurrentApplication { get; set; } = string.Empty;

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool IsSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        CurrentApplication = _appContextService.GetCurrentApplication();
        RequiresBuildingSelection = _appContextService.RequiresBuildingSelection();
        SelectedBuildingId = _appContextService.GetSelectedBuildingId();

        if (RequiresBuildingSelection)
        {
            Buildings = await _storedProcService.GetBuildingsAsync(CurrentApplication);
        }

        // Get recent visits for today — uses fast procedure with no booking joins
        RecentVisits = await _storedProcService.GetRecentVisitsAsync(CurrentApplication, DateTime.Today);

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        CurrentApplication = _appContextService.GetCurrentApplication();
        RequiresBuildingSelection = _appContextService.RequiresBuildingSelection();

        if (string.IsNullOrWhiteSpace(CardData))
        {
            StatusMessage = "Please swipe a card or enter an SBUID.";
            IsSuccess = false;
            return RedirectToPage();
        }

        // Parse the card data
        var parseResult = _cardSwipeService.ParseCardData(CardData);

        if (!parseResult.IsValid)
        {
            _logger.LogWarning("Card parse failed for input (length={Length}): {Error}", CardData.Length, parseResult.ErrorMessage);
            StatusMessage = parseResult.ErrorMessage ?? "Invalid card data.";
            IsSuccess = false;
            return RedirectToPage();
        }

        // HID card: resolve HIDNUM → EMPLID via PS_IDCARD
        if (parseResult.DataType == CardDataType.HidCard)
        {
            var emplId = await _storedProcService.GetEmplIdFromHidAsync(parseResult.HidNum!);
            if (string.IsNullOrWhiteSpace(emplId))
            {
                StatusMessage = $"HID card not found in student records (HIDNUM {parseResult.HidNum}).";
                IsSuccess = false;
                return RedirectToPage();
            }
            parseResult.SBUID = emplId;
        }

        // Get building location if required
        string? location = null;
        if (RequiresBuildingSelection)
        {
            if (SelectedBuildingId.HasValue)
            {
                _appContextService.SetSelectedBuildingId(SelectedBuildingId);
                var buildings = await _storedProcService.GetBuildingsAsync(CurrentApplication);
                var building = buildings.FirstOrDefault(b => b.BuildingId == SelectedBuildingId);
                location = building?.Name;
            }
            else
            {
                var savedBuildingId = _appContextService.GetSelectedBuildingId();
                if (savedBuildingId.HasValue)
                {
                    var buildings = await _storedProcService.GetBuildingsAsync(CurrentApplication);
                    var building = buildings.FirstOrDefault(b => b.BuildingId == savedBuildingId);
                    location = building?.Name;
                }
            }
        }

        // Verify student is a current resident
        var room = await _storedProcService.GetRoomAsync(parseResult.SBUID!);
        if (string.IsNullOrWhiteSpace(room))
        {
            _logger.LogWarning("Residency check failed for SBUID {SBUID} — no room assignment found", parseResult.SBUID);
            StatusMessage = "Access denied: no active room assignment found for this ID.";
            IsSuccess = false;
            return RedirectToPage();
        }

        // Get name from parse result or try to look up associated name
        var firstName = parseResult.FirstName;
        var lastName = parseResult.LastName;

        if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
        {
            var assocName = await _storedProcService.GetAssociatedNameAsync(parseResult.SBUID!, CurrentApplication);
            if (assocName != null)
            {
                firstName = assocName.FirstName;
                lastName = assocName.LastName;
            }
            else
            {
                // Fall back to external student directory lookup
                var (sFirstName, sLastName) = await _storedProcService.GetStudentNameAsync(parseResult.SBUID!);
                if (!string.IsNullOrWhiteSpace(sFirstName) || !string.IsNullOrWhiteSpace(sLastName))
                {
                    firstName = sFirstName;
                    lastName = sLastName;
                }
            }
        }

        // Get hostname and IP
        var hostname = Environment.MachineName;
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var netIdAudit = User.Identity?.Name ?? "Unknown";

        // Log the visit
        var success = await _storedProcService.LogVisitAsync(
            parseResult.SBUID!,
            firstName ?? "Unknown",
            lastName ?? "Unknown",
            hostname,
            ip,
            location ?? "Main",
            CurrentApplication,
            Note,
            netIdAudit);

        if (success)
        {
            var name = !string.IsNullOrWhiteSpace(firstName) ? $"{firstName} {lastName}" : parseResult.SBUID;
            StatusMessage = $"Visit logged for {name}";
            IsSuccess = true;
            _logger.LogInformation("Visit logged for SBUID {SBUID} by {NetId}", parseResult.SBUID, netIdAudit);
        }
        else
        {
            StatusMessage = "Error logging visit. Please try again.";
            IsSuccess = false;
            _logger.LogError("Failed to log visit for SBUID {SBUID}", parseResult.SBUID);
        }

        return RedirectToPage();
    }
}
