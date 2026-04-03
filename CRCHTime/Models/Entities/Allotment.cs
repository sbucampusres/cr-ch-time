using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRCHTime.Models.Entities;

/// <summary>
/// Annual hourly allotment per department + shift category combination.
/// Maps to WS_CR_CS_ALLOTMENTS table.
/// </summary>
[Table("WS_CR_CS_ALLOTMENTS")]
public class Allotment
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Column("DEPT_ID")]
    public int DeptId { get; set; }

    [Column("CATEGORY_ID")]
    public int CategoryId { get; set; }

    [Column("YEAR")]
    public int Year { get; set; }

    [Column("HOURS")]
    public decimal? Hours { get; set; }

    [Required]
    [StringLength(10)]
    [Column("APPLICATION")]
    public string Application { get; set; } = string.Empty;

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
