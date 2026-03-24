using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRCardSwipe.Models.Entities;

/// <summary>
/// Visit entity - Maps to WS_FCVISITS table
/// Records card swipe visits to facilities
/// </summary>
[Table("WS_FCVISITS")]
public class Visit
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required]
    [StringLength(9)]
    [Column("SBUID")]
    [Display(Name = "SBU ID")]
    public string SBUID { get; set; } = string.Empty;

    [StringLength(50)]
    [Column("HOSTNAME")]
    public string? Hostname { get; set; }

    [StringLength(50)]
    [Column("FIRSTNAME")]
    [Display(Name = "First Name")]
    public string? FirstName { get; set; }

    [StringLength(50)]
    [Column("LASTNAME")]
    [Display(Name = "Last Name")]
    public string? LastName { get; set; }

    [StringLength(50)]
    [Column("LOCATION")]
    public string? Location { get; set; }

    [StringLength(50)]
    [Column("IP")]
    [Display(Name = "IP Address")]
    public string? IP { get; set; }

    [Column("SWIPETIME")]
    [Display(Name = "Swipe Time")]
    public DateTime SwipeTime { get; set; } = DateTime.UtcNow;

    [StringLength(10)]
    [Column("APPLICATION")]
    public string? Application { get; set; }

    [StringLength(500)]
    [Column("NOTE")]
    public string? Note { get; set; }

    [StringLength(50)]
    [Column("NETID_AUDIT")]
    [Display(Name = "Recorded By")]
    public string? NetIdAudit { get; set; }

    // Computed property for full name
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
