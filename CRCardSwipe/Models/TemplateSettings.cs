namespace CRCardSwipe.Models;

/// <summary>
/// Configuration settings for customizing the application for different projects.
/// These values are loaded from appsettings.json and allow the template to be
/// easily adapted for new projects without code changes.
/// </summary>
public class TemplateSettings
{
    /// <summary>
    /// Short application name (e.g., "CR FLEET", "PROCUREMENT")
    /// Used in navbar and page titles
    /// </summary>
    public string AppName { get; set; } = "APP NAME";

    /// <summary>
    /// Full application name (e.g., "Campus Residences Fleet Management")
    /// Used in footer and documentation
    /// </summary>
    public string AppFullName { get; set; } = "Application Full Name";

    /// <summary>
    /// Support contact email address
    /// </summary>
    public string SupportEmail { get; set; } = "support@example.edu";

    /// <summary>
    /// Support contact person name
    /// </summary>
    public string SupportContact { get; set; } = "Support Contact";

    /// <summary>
    /// Database table prefix (e.g., "WS_FLEET_", "WS_PROC_")
    /// All application tables will use this prefix
    /// </summary>
    public string TablePrefix { get; set; } = "WS_APP_";

    /// <summary>
    /// Oracle schema name (e.g., "CRADMIN")
    /// </summary>
    public string SchemaName { get; set; } = "SCHEMA";

    /// <summary>
    /// Oracle tablespace name (e.g., "CAMPUSRES")
    /// </summary>
    public string TablespaceName { get; set; } = "TABLESPACE";

    /// <summary>
    /// NetID lookup view name (e.g., "JS_V_NETIDALL")
    /// Used by UserLookupService
    /// </summary>
    public string NetIdLookupView { get; set; } = "JS_V_NETIDALL";

    /// <summary>
    /// Email address lookup view name (e.g., "JS_V_ALLEMAILADDR")
    /// Used by UserLookupService
    /// </summary>
    public string EmailLookupView { get; set; } = "JS_V_ALLEMAILADDR";
}
