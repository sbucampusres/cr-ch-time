using CRCardSwipe.Models.ViewModels;

namespace CRCardSwipe.Services;

/// <summary>
/// Interface for card data parsing and validation
/// </summary>
public interface ICardSwipeService
{
    /// <summary>
    /// Parse raw card swipe data and extract SBUID and name
    /// </summary>
    CardParseResult ParseCardData(string rawData);

    /// <summary>
    /// Parse barcode input (9-digit SBUID)
    /// </summary>
    CardParseResult ParseBarcode(string barcode);

    /// <summary>
    /// Validate SBUID format
    /// </summary>
    bool ValidateSBUID(string sbuid);

    /// <summary>
    /// Check if input looks like magnetic stripe data
    /// </summary>
    bool IsMagneticStripeData(string data);

    /// <summary>
    /// Check if input looks like barcode data
    /// </summary>
    bool IsBarcodeData(string data);

    /// <summary>
    /// Check if input looks like HID card wedge data (28 hex chars, no sentinels)
    /// </summary>
    bool IsHidCardData(string data);
}
