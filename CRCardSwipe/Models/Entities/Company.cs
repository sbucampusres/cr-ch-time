using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRCardSwipe.Models.Entities;

/// <summary>
/// Company entity - Maps to WS_FC_COMPANY table
/// Represents contractor companies
/// </summary>
[Table("WS_FC_COMPANY")]
public class Company
{
    [Key]
    [Column("COMPANY_ID")]
    public int CompanyId { get; set; }

    [Required]
    [StringLength(100)]
    [Column("NAME")]
    [Display(Name = "Company Name")]
    public string Name { get; set; } = string.Empty;

    [StringLength(10)]
    [Column("APPLICATION")]
    public string? Application { get; set; }
}
