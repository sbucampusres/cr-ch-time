using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRCHTime.Models.Entities;

/// <summary>
/// Department entity - Maps to WS_FC_DEPARTMENTS table
/// Represents departments for organizational grouping
/// </summary>
[Table("WS_FC_DEPARTMENTS")]
public class Department
{
    [Key]
    [Column("DEPT_ID")]
    public int DeptId { get; set; }

    [Required]
    [StringLength(100)]
    [Column("NAME")]
    [Display(Name = "Department Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(10)]
    [Column("APPLICATION")]
    public string? Application { get; set; }

    [Column("INACTIVE")]
    [Display(Name = "Inactive")]
    public bool Inactive { get; set; }

    // Navigation property
    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();

    // Computed property
    [NotMapped]
    public string DisplayName => Inactive ? $"{Name} (Inactive)" : Name;
}
