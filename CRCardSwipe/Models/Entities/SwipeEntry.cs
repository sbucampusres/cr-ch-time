using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRCardSwipe.Models.Entities;

/// <summary>
/// SwipeEntry entity - Maps to WS_FCSWIPES table
/// Records contractor card swipe in/out times
/// </summary>
[Table("WS_FCSWIPES")]
public class SwipeEntry
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required]
    [StringLength(9)]
    [Column("SBUID")]
    [Display(Name = "SBU ID")]
    public string SBUID { get; set; } = string.Empty;

    [Column("SWIPETIME_IN")]
    [Display(Name = "Swipe In")]
    public DateTime SwipeTimeIn { get; set; }

    [Column("SWIPETIME_OUT")]
    [Display(Name = "Swipe Out")]
    public DateTime? SwipeTimeOut { get; set; }

    [StringLength(50)]
    [Column("NETID_IN")]
    [Display(Name = "Recorded In By")]
    public string? NetIdIn { get; set; }

    [StringLength(50)]
    [Column("NETID_OUT")]
    [Display(Name = "Recorded Out By")]
    public string? NetIdOut { get; set; }

    [StringLength(50)]
    [Column("HOSTNAME_IN")]
    public string? HostnameIn { get; set; }

    [StringLength(50)]
    [Column("HOSTNAME_OUT")]
    public string? HostnameOut { get; set; }

    [StringLength(10)]
    [Column("APPLICATION")]
    public string? Application { get; set; }

    [StringLength(50)]
    [Column("FIRSTNAME")]
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [StringLength(50)]
    [Column("LASTNAME")]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [StringLength(50)]
    [Column("IP_IN")]
    public string? IPIn { get; set; }

    [StringLength(50)]
    [Column("IP_OUT")]
    public string? IPOut { get; set; }

    // Computed properties
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();

    [NotMapped]
    public bool IsSwipedIn => SwipeTimeOut == null;

    [NotMapped]
    public TimeSpan? Duration => SwipeTimeOut.HasValue
        ? SwipeTimeOut.Value - SwipeTimeIn
        : DateTime.UtcNow - SwipeTimeIn;

    [NotMapped]
    public double? HoursWorked => Duration?.TotalHours;

    [NotMapped]
    public string DurationDisplay
    {
        get
        {
            if (!Duration.HasValue) return "On Site";
            var d = Duration.Value;
            return $"{(int)d.TotalHours}h {d.Minutes}m";
        }
    }
}
