using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using CRCHTime.Models.Entities;
using CRCHTime.Services;

namespace CRCHTime.Pages.Staff;

[Authorize(Policy = "RequireOperator")]
public class CheckOutModel : PageModel
{
    private readonly IStoredProcService _storedProcService;
    private readonly IApplicationContextService _appContextService;
    private readonly ILogger<CheckOutModel> _logger;

    public CheckOutModel(
        IStoredProcService storedProcService,
        IApplicationContextService appContextService,
        ILogger<CheckOutModel> logger)
    {
        _storedProcService = storedProcService;
        _appContextService = appContextService;
        _logger = logger;
    }

    public string NetId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string CurrentApplication { get; set; } = string.Empty;
    public DateTime? CheckinTime { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public bool IsSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        NetId = User.Identity?.Name ?? string.Empty;
        DisplayName = User.Claims.FirstOrDefault(c => c.Type == "DisplayName")?.Value ?? NetId;
        CurrentApplication = _appContextService.GetCurrentApplication();

        // Look up the current open shift (check-in with no check-out)
        var entries = await _storedProcService.GetTimecardAsync(
            DateTime.Now.AddDays(-1), DateTime.Now, NetId, null, CurrentApplication);
        CheckinTime = entries.FirstOrDefault(e => e.IsCheckedIn)?.CheckinTimestamp;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        NetId = User.Identity?.Name ?? string.Empty;
        DisplayName = User.Claims.FirstOrDefault(c => c.Type == "DisplayName")?.Value ?? NetId;
        CurrentApplication = _appContextService.GetCurrentApplication();

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "Unknown";
        var hostname = await ResolveHostnameAsync(ip);

        var result = await _storedProcService.StaffCheckoutAsync(
            NetId,
            hostname,
            ip,
            CurrentApplication);

        if (result.Success)
        {
            StatusMessage = $"Successfully checked out at {DateTime.Now:h:mm tt}";
            IsSuccess = true;
            _logger.LogInformation("Staff {NetId} checked out for application {Application}", NetId, CurrentApplication);
        }
        else
        {
            StatusMessage = result.ErrorMessage;
            IsSuccess = false;
            _logger.LogWarning("Failed to check out staff {NetId}: {Error}", NetId, result.ErrorMessage);
        }

        return RedirectToPage();
    }

    private static async Task<string> ResolveHostnameAsync(string ip)
    {
        try
        {
            var entry = await System.Net.Dns.GetHostEntryAsync(ip);
            return entry.HostName;
        }
        catch
        {
            return ip;
        }
    }
}
