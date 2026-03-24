namespace CRCardSwipe.Models;

/// <summary>
/// Configuration settings specific to CardSwipe functionality.
/// Loaded from appsettings.json "CardSwipe" section.
/// </summary>
public class CardSwipeSettings
{
    /// <summary>
    /// Minutes to wait before allowing duplicate entry for same SBUID
    /// </summary>
    public int DuplicateEntryWindowMinutes { get; set; } = 1;

    /// <summary>
    /// Maximum allowed shift hours before warning (default applications)
    /// </summary>
    public int MaxShiftHours { get; set; } = 12;

    /// <summary>
    /// Maximum allowed shift hours for Conference Housing
    /// </summary>
    public int MaxShiftHoursConferenceHousing { get; set; } = 24;

    /// <summary>
    /// Expected length of SBU ID (9 digits)
    /// </summary>
    public int SBUIDLength { get; set; } = 9;

    /// <summary>
    /// Minimum length for card data to be considered valid magnetic stripe data
    /// </summary>
    public int CardDataMinLength { get; set; } = 100;
}
