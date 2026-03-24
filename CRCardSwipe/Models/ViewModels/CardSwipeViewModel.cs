using System.ComponentModel.DataAnnotations;
using CRCardSwipe.Models.Entities;

namespace CRCardSwipe.Models.ViewModels;

/// <summary>
/// View model for the card swipe page
/// </summary>
public class CardSwipeViewModel
{
    [Display(Name = "Card Data")]
    public string? CardData { get; set; }

    [Display(Name = "Building")]
    public int? SelectedBuildingId { get; set; }

    [Display(Name = "Note")]
    [StringLength(500)]
    public string? Note { get; set; }

    public IEnumerable<Building> Buildings { get; set; } = new List<Building>();

    public IEnumerable<Visit> RecentVisits { get; set; } = new List<Visit>();

    public string? StatusMessage { get; set; }

    public bool IsSuccess { get; set; }
}

/// <summary>
/// Result of parsing card swipe data
/// </summary>
public class CardParseResult
{
    public bool IsValid { get; set; }
    public string? SBUID { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? ErrorMessage { get; set; }
    public CardDataType DataType { get; set; }
    /// <summary>
    /// Set when DataType is HidCard. Requires a DB lookup to resolve to SBUID.
    /// </summary>
    public string? HidNum { get; set; }
}

/// <summary>
/// Type of card data input
/// </summary>
public enum CardDataType
{
    Unknown,
    MagneticStripe,
    Barcode,
    ManualEntry,
    HidCard
}
