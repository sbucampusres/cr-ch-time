using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRCHTime.Models.Entities;

/// <summary>
/// IdAssociation entity - Maps to WS_FCIDASSOCNAME table
/// Links names to SBUIDs for contractors without student records
/// </summary>
[Table("WS_FCIDASSOCNAME")]
public class IdAssociation
{
    [Key]
    [Column("SBUID")]
    [StringLength(9)]
    [Display(Name = "SBU ID")]
    public string SBUID { get; set; } = string.Empty;

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

    // Computed property
    [NotMapped]
    public string FullName => $"{FirstName} {LastName}".Trim();
}
