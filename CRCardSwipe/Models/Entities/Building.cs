using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRCardSwipe.Models.Entities;

/// <summary>
/// Building entity - Maps to WS_FC_BUILDING table
/// Represents buildings/facilities for location tracking
/// </summary>
[Table("WS_FC_BUILDING")]
public class Building
{
    [Key]
    [Column("BUILDING_ID")]
    public int BuildingId { get; set; }

    [Required]
    [StringLength(100)]
    [Column("NAME")]
    [Display(Name = "Building Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(10)]
    [Column("APPLICATION")]
    public string? Application { get; set; }

    [Column("INACTIVE")]
    [Display(Name = "Inactive")]
    public bool Inactive { get; set; }

    // Computed property
    [NotMapped]
    public string DisplayName => Inactive ? $"{Name} (Inactive)" : Name;
}
