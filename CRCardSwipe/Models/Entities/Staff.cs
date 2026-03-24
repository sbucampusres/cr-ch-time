using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRCardSwipe.Models.Entities;

/// <summary>
/// Staff entity - Maps to WS_FCSTAFF table
/// Represents staff members who can check in/out
/// Uses composite key (NETID, APPLICATION)
/// </summary>
[Table("WS_FCSTAFF")]
public class Staff
{
    [Required]
    [StringLength(50)]
    [Column("NETID")]
    [Display(Name = "NetID")]
    public string NetId { get; set; } = string.Empty;

    [Required]
    [StringLength(10)]
    [Column("APPLICATION")]
    public string Application { get; set; } = string.Empty;

    [Column("AUDIT_TIMESTAMP")]
    [Display(Name = "Created")]
    public DateTime? AuditTimestamp { get; set; }

    [StringLength(50)]
    [Column("HOSTNAME")]
    public string? Hostname { get; set; }

    [Column("TERMINATIONDATE")]
    [Display(Name = "Termination Date")]
    [DataType(DataType.Date)]
    public DateTime? TerminationDate { get; set; }

    [StringLength(50)]
    [Column("ROLE")]
    public string? Role { get; set; }

    [Column("DEPT_ID")]
    [Display(Name = "Department")]
    public string? DeptId { get; set; }

    [Column("ISADMIN")]
    public int? IsAdmin { get; set; }

    // Navigation property
    [NotMapped]
    public virtual Department? Department { get; set; }

    // Computed properties
    [NotMapped]
    public bool IsTerminated => TerminationDate.HasValue && TerminationDate.Value.Date <= DateTime.Today;

    [NotMapped]
    public string Status => IsTerminated ? "Terminated" : "Active";

    [NotMapped]
    public bool IsAdministrator => IsAdmin.HasValue && IsAdmin.Value == 1;
}
