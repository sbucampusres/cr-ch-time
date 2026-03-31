namespace CRCHTime.Services;

/// <summary>
/// Service for managing application context and session state
/// </summary>
public class ApplicationContextService : IApplicationContextService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<ApplicationContextService> _logger;

    private const string ApplicationSessionKey = "CurrentApplication";
    private const string BuildingSessionKey = "SelectedBuildingId";
    private const string DefaultApplication = "CH";

    // Define all available applications
    private static readonly List<ApplicationInfo> Applications = new()
    {
        new ApplicationInfo
        {
            Code = "FC",
            Name = "Fitness Center",
            Description = "Fitness Center card swipes and staff check-in/out",
            RequiresBuildingSelection = false,
            HasStaffCheckIn = true,
            HasContractorSwipes = false,
            HasVisitTracking = true,
            MinimumRole = "Operator"
        },
        new ApplicationInfo
        {
            Code = "CH",
            Name = "Conference Housing",
            Description = "Conference Housing staff check-in/out with departments",
            RequiresBuildingSelection = false,
            HasStaffCheckIn = true,
            HasContractorSwipes = false,
            HasVisitTracking = false,
            MinimumRole = "Operator"
        },
        new ApplicationInfo
        {
            Code = "RE",
            Name = "Summer Renovations",
            Description = "Contractor swipes for summer renovation projects",
            RequiresBuildingSelection = false,
            HasStaffCheckIn = false,
            HasContractorSwipes = true,
            HasVisitTracking = false,
            MinimumRole = "Operator"
        },
        new ApplicationInfo
        {
            Code = "HA",
            Name = "Residence Hall Association",
            Description = "Visit tracking for RHA events",
            RequiresBuildingSelection = false,
            HasStaffCheckIn = false,
            HasContractorSwipes = false,
            HasVisitTracking = true,
            MinimumRole = "Viewer"
        },
        new ApplicationInfo
        {
            Code = "RT",
            Name = "Residential Tutoring Center",
            Description = "Staff check-in/out for tutoring centers",
            RequiresBuildingSelection = false,
            HasStaffCheckIn = true,
            HasContractorSwipes = false,
            HasVisitTracking = false,
            MinimumRole = "Operator"
        },
        new ApplicationInfo
        {
            Code = "FD",
            Name = "RSP Front Desk",
            Description = "Card swipes with building selection and notes",
            RequiresBuildingSelection = true,
            HasStaffCheckIn = false,
            HasContractorSwipes = false,
            HasVisitTracking = true,
            MinimumRole = "Operator"
        },
        new ApplicationInfo
        {
            Code = "ET",
            Name = "Event Tracking",
            Description = "Visit logging for campus events",
            RequiresBuildingSelection = false,
            HasStaffCheckIn = false,
            HasContractorSwipes = false,
            HasVisitTracking = true,
            MinimumRole = "Viewer"
        }
    };

    public ApplicationContextService(
        IHttpContextAccessor httpContextAccessor,
        ILogger<ApplicationContextService> logger)
    {
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    public string GetCurrentApplication()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            _logger.LogWarning("Session not available, returning default application");
            return DefaultApplication;
        }

        var app = session.GetString(ApplicationSessionKey);
        return string.IsNullOrWhiteSpace(app) ? DefaultApplication : app;
    }

    public void SetCurrentApplication(string application)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            _logger.LogWarning("Session not available, cannot set application");
            return;
        }

        // Validate application code
        var appInfo = GetApplicationInfo(application);
        if (appInfo == null)
        {
            _logger.LogWarning("Invalid application code: {Application}", application);
            return;
        }

        session.SetString(ApplicationSessionKey, application.ToUpper());
        _logger.LogInformation("Application set to: {Application}", application);

        // Clear building selection if not required by new application
        if (!appInfo.RequiresBuildingSelection)
        {
            SetSelectedBuildingId(null);
        }
    }

    public int? GetSelectedBuildingId()
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
            return null;

        var buildingId = session.GetInt32(BuildingSessionKey);
        return buildingId;
    }

    public void SetSelectedBuildingId(int? buildingId)
    {
        var session = _httpContextAccessor.HttpContext?.Session;
        if (session == null)
        {
            _logger.LogWarning("Session not available, cannot set building");
            return;
        }

        if (buildingId.HasValue)
        {
            session.SetInt32(BuildingSessionKey, buildingId.Value);
            _logger.LogInformation("Building set to: {BuildingId}", buildingId);
        }
        else
        {
            session.Remove(BuildingSessionKey);
            _logger.LogInformation("Building selection cleared");
        }
    }

    public IEnumerable<ApplicationInfo> GetAvailableApplications()
    {
        return Applications.AsReadOnly();
    }

    public ApplicationInfo? GetApplicationInfo(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            return null;

        return Applications.FirstOrDefault(a =>
            a.Code.Equals(code, StringComparison.OrdinalIgnoreCase));
    }

    public bool RequiresBuildingSelection()
    {
        var currentApp = GetCurrentApplication();
        var appInfo = GetApplicationInfo(currentApp);
        return appInfo?.RequiresBuildingSelection ?? false;
    }
}
