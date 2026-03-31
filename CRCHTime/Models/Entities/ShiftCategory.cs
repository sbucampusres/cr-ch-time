using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRCHTime.Models.Entities;

/// <summary>
/// ShiftCategory entity - Maps to WS_CR_CS_SHIFT_CATEGORIES table.
/// Defines the list of shift types selectable at check-in.
/// </summary>
[Table("WS_CR_CS_SHIFT_CATEGORIES")]
public class ShiftCategory
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required]
    [StringLength(100)]
    [Column("NAME")]
    [Display(Name = "Category Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    [Column("DESCRIPTION")]
    public string? Description { get; set; }

    [Required]
    [StringLength(10)]
    [Column("APPLICATION")]
    public string Application { get; set; } = string.Empty;

    [Column("IS_ACTIVE")]
    public bool IsActive { get; set; } = true;

    [Column("CREATED_AT")]
    public DateTime CreatedAt { get; set; }

    [StringLength(50)]
    [Column("CREATED_BY")]
    public string? CreatedBy { get; set; }

    [Column("MODIFIED_AT")]
    public DateTime? ModifiedAt { get; set; }

    [StringLength(50)]
    [Column("MODIFIED_BY")]
    public string? ModifiedBy { get; set; }
}
