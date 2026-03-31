namespace CRCHTime.Services;

public class UserLookupService
{
    private readonly IStoredProcService _storedProcService;
    private readonly ILogger<UserLookupService> _logger;

    public UserLookupService(IStoredProcService storedProcService, ILogger<UserLookupService> logger)
    {
        _storedProcService = storedProcService;
        _logger = logger;
    }

    public async Task<UserLookupResult?> LookupUserByNetIdAsync(string netId)
    {
        if (string.IsNullOrWhiteSpace(netId))
            return null;

        try
        {
            var (name, email) = await _storedProcService.GetUserInfoAsync(netId.Trim());

            if (name == null && email == null)
            {
                _logger.LogWarning("No user info found for NetID {NetId}", netId);
                return null;
            }

            _logger.LogInformation("Found user info for NetID {NetId}: {Name}, {Email}", netId, name, email);
            return new UserLookupResult { NAME = name, EMAIL_ADDR = email };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up user info for NetID {NetId}", netId);
            return null;
        }
    }
}

public class UserLookupResult
{
    public string? NAME { get; set; }
    public string? EMAIL_ADDR { get; set; }
}
