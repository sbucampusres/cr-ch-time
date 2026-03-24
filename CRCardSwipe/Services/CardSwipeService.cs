using CRCardSwipe.Models;
using CRCardSwipe.Models.ViewModels;
using System.Text.RegularExpressions;

namespace CRCardSwipe.Services;

/// <summary>
/// Service for parsing card swipe data from magnetic stripe readers and barcodes
/// </summary>
public class CardSwipeService : ICardSwipeService
{
    private readonly ILogger<CardSwipeService> _logger;
    private readonly CardSwipeSettings _settings;

    // Regex patterns for card data parsing
    // Old SBU format: SBUID is in the PAN field position (9 digits)
    private static readonly Regex Track1Regex = new(@"%B(\d{9})\^([^/]*)/([^\^]*)\^", RegexOptions.Compiled);
    // New SBU format: 16-digit PAN, SBUID embedded in Track 1 field 3 discretionary data
    // e.g. %B6038390024157800^LASTNAME/FIRSTNAME^491212000000104575156 000?
    private static readonly Regex Track1ExtRegex = new(@"%B\d+\^([^/]*)/([^\^]*)\^[^?]*?(\d{9})(?!\d)", RegexOptions.Compiled);
    private static readonly Regex Track2Regex = new(@";(\d{9})=", RegexOptions.Compiled);
    private static readonly Regex SBUIDOnlyRegex = new(@"^(\d{9})$", RegexOptions.Compiled);
    // HID card wedge: exactly 28 lowercase hex characters, no sentinels
    private static readonly Regex HidRegex = new(@"^[0-9a-fA-F]{28}$", RegexOptions.Compiled);

    public CardSwipeService(ILogger<CardSwipeService> logger, CardSwipeSettings settings)
    {
        _logger = logger;
        _settings = settings;
    }

    public CardParseResult ParseCardData(string rawData)
    {
        if (string.IsNullOrWhiteSpace(rawData))
        {
            return new CardParseResult
            {
                IsValid = false,
                ErrorMessage = "No card data received"
            };
        }

        var trimmedData = rawData.Trim();

        // HID card wedge: exactly 28 hex chars, no sentinels
        if (IsHidCardData(trimmedData))
        {
            return ParseHidCard(trimmedData);
        }

        // Check if it's barcode data first (9 digits only)
        if (IsBarcodeData(trimmedData))
        {
            return ParseBarcode(trimmedData);
        }

        // Check if it's magnetic stripe data
        if (IsMagneticStripeData(trimmedData))
        {
            return ParseMagneticStripe(trimmedData);
        }

        // Try to extract just the SBUID if nothing else works
        var sbuidMatch = Regex.Match(trimmedData, @"(\d{9})");
        if (sbuidMatch.Success)
        {
            _logger.LogInformation("Extracted SBUID from raw data: {SBUID}", sbuidMatch.Groups[1].Value);
            return new CardParseResult
            {
                IsValid = true,
                SBUID = sbuidMatch.Groups[1].Value,
                DataType = CardDataType.ManualEntry
            };
        }

        return new CardParseResult
        {
            IsValid = false,
            ErrorMessage = "Unable to parse card data - invalid format",
            DataType = CardDataType.Unknown
        };
    }

    public CardParseResult ParseBarcode(string barcode)
    {
        if (string.IsNullOrWhiteSpace(barcode))
        {
            return new CardParseResult
            {
                IsValid = false,
                ErrorMessage = "No barcode data received"
            };
        }

        var trimmed = barcode.Trim();

        if (!ValidateSBUID(trimmed))
        {
            return new CardParseResult
            {
                IsValid = false,
                ErrorMessage = $"Invalid SBUID format. Expected {_settings.SBUIDLength} digits.",
                DataType = CardDataType.Barcode
            };
        }

        _logger.LogInformation("Parsed barcode SBUID: {SBUID}", trimmed);

        return new CardParseResult
        {
            IsValid = true,
            SBUID = trimmed,
            DataType = CardDataType.Barcode
        };
    }

    public bool ValidateSBUID(string sbuid)
    {
        if (string.IsNullOrWhiteSpace(sbuid))
            return false;

        var trimmed = sbuid.Trim();

        // Must be exactly 9 digits
        return trimmed.Length == _settings.SBUIDLength
            && trimmed.All(char.IsDigit);
    }

    public bool IsMagneticStripeData(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return false;

        // Magnetic stripe data typically starts with % or ;
        // and contains track delimiters
        return data.Length >= _settings.CardDataMinLength
            && (data.Contains('%') || data.Contains(';'))
            && (data.Contains('^') || data.Contains('='));
    }

    public bool IsHidCardData(string data)
    {
        if (string.IsNullOrWhiteSpace(data)) return false;
        return HidRegex.IsMatch(data.Trim());
    }

    public bool IsBarcodeData(string data)
    {
        if (string.IsNullOrWhiteSpace(data))
            return false;

        var trimmed = data.Trim();

        // Barcode is exactly 9 digits
        return SBUIDOnlyRegex.IsMatch(trimmed);
    }

    private CardParseResult ParseHidCard(string data)
    {
        // Last 3 bytes (6 hex chars) contain the card number shifted left by 1 bit.
        // HIDNUM = last_6_hex_value >> 1
        var tail = data[^6..];
        var tailVal = Convert.ToInt32(tail, 16);
        var hidNum = (tailVal >> 1).ToString();

        _logger.LogInformation("Parsed HID card: raw tail={Tail}, HIDNUM={HidNum}", tail, hidNum);

        return new CardParseResult
        {
            IsValid = true,
            HidNum = hidNum,
            DataType = CardDataType.HidCard
            // SBUID left null — requires async DB lookup via CRFCCS_GET_EMPLID_FROM_HID
        };
    }

    private CardParseResult ParseMagneticStripe(string data)
    {
        string? sbuid = null;
        string? firstName = null;
        string? lastName = null;

        // Try Track 1 format first: %B<SBUID>^<LASTNAME>/<FIRSTNAME>^...
        var track1Match = Track1Regex.Match(data);
        if (track1Match.Success)
        {
            sbuid = track1Match.Groups[1].Value;
            lastName = track1Match.Groups[2].Value.Trim();
            firstName = track1Match.Groups[3].Value.Trim();

            _logger.LogInformation("Parsed Track 1: SBUID={SBUID}, Name={FirstName} {LastName}",
                sbuid, firstName, lastName);
        }

        // Try Track 1 extended format: 16-digit PAN, SBUID in field 3 discretionary data
        if (sbuid == null)
        {
            var track1ExtMatch = Track1ExtRegex.Match(data);
            if (track1ExtMatch.Success)
            {
                lastName = track1ExtMatch.Groups[1].Value.Trim();
                firstName = track1ExtMatch.Groups[2].Value.Trim();
                sbuid = track1ExtMatch.Groups[3].Value;
                _logger.LogInformation("Parsed Track 1 (extended): SBUID={SBUID}, Name={FirstName} {LastName}",
                    sbuid, firstName, lastName);
            }
        }

        // Try Track 2 format: ;<SBUID>=...
        if (sbuid == null)
        {
            var track2Match = Track2Regex.Match(data);
            if (track2Match.Success)
            {
                sbuid = track2Match.Groups[1].Value;
                _logger.LogInformation("Parsed Track 2: SBUID={SBUID}", sbuid);
            }
        }

        if (sbuid == null || !ValidateSBUID(sbuid))
        {
            return new CardParseResult
            {
                IsValid = false,
                ErrorMessage = "Unable to extract valid SBUID from card data",
                DataType = CardDataType.MagneticStripe
            };
        }

        return new CardParseResult
        {
            IsValid = true,
            SBUID = sbuid,
            FirstName = CleanName(firstName),
            LastName = CleanName(lastName),
            DataType = CardDataType.MagneticStripe
        };
    }

    private static string? CleanName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        // Remove extra whitespace and convert to title case
        var cleaned = Regex.Replace(name.Trim(), @"\s+", " ");

        if (cleaned.Length == 0)
            return null;

        // Convert to title case
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(cleaned.ToLower());
    }
}
