namespace CRCHTime.Services;

/// <summary>
/// Interface for managing application context (FC, CH, RE, HA, RT, FD, ET)
/// </summary>
public interface IApplicationContextService
{
    /// <summary>
    /// Get the current application code
    /// </summary>
    string GetCurrentApplication();

    /// <summary>
    /// Set the current application code
    /// </summary>
    void SetCurrentApplication(string application);

    /// <summary>
    /// Get the current selected building (for FD application)
    /// </summary>
    int? GetSelectedBuildingId();

    /// <summary>
    /// Set the current selected building
    /// </summary>
    void SetSelectedBuildingId(int? buildingId);

    /// <summary>
    /// Get all available applications
    /// </summary>
    IEnumerable<ApplicationInfo> GetAvailableApplications();

    /// <summary>
    /// Get application info by code
    /// </summary>
    ApplicationInfo? GetApplicationInfo(string code);

    /// <summary>
    /// Check if the current application requires building selection
    /// </summary>
    bool RequiresBuildingSelection();
}

/// <summary>
/// Information about an application context
/// </summary>
public class ApplicationInfo
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public bool RequiresBuildingSelection { get; set; }
    public bool HasStaffCheckIn { get; set; }
    public bool HasContractorSwipes { get; set; }
    public bool HasVisitTracking { get; set; }
    public string MinimumRole { get; set; } = "Viewer";
}
