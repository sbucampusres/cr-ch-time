using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CRCHTime.Models.Entities;

/// <summary>
/// TimesheetEntry entity - Maps to WS_FCSTAFFWORKLOG table
/// Records staff check-in/check-out times for timesheet tracking
/// </summary>
[Table("WS_FCSTAFFWORKLOG")]
public class TimesheetEntry
{
    [Key]
    [Column("ID")]
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    [Column("NETID")]
    [Display(Name = "NetID")]
    public string NetId { get; set; } = string.Empty;

    [Column("CHECKIN_TIMESTAMP")]
    [Display(Name = "Check-In Time")]
    public DateTime CheckinTimestamp { get; set; }

    [StringLength(50)]
    [Column("CHECKIN_HOSTNAME")]
    [Display(Name = "Check-In Computer")]
    public string? CheckinHostname { get; set; }

    [StringLength(50)]
    [Column("CHECKIN_IP")]
    [Display(Name = "Check-In IP")]
    public string? CheckinIP { get; set; }

    [Column("CHECKOUT_TIMESTAMP")]
    [Display(Name = "Check-Out Time")]
    public DateTime? CheckoutTimestamp { get; set; }

    [StringLength(50)]
    [Column("CHECKOUT_HOSTNAME")]
    [Display(Name = "Check-Out Computer")]
    public string? CheckoutHostname { get; set; }

    [StringLength(50)]
    [Column("CHECKOUT_IP")]
    [Display(Name = "Check-Out IP")]
    public string? CheckoutIP { get; set; }

    [StringLength(10)]
    [Column("APPLICATION")]
    public string? Application { get; set; }

    [Column("DEPARTMENT_ID")]
    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    [Column("SHIFT_CATEGORY_ID")]
    [Display(Name = "Shift Category")]
    public int? ShiftCategoryId { get; set; }

    // Populated by CRCH_GET_TIMECARD — not mapped to table columns
    [NotMapped]
    public string? RowIdentifier { get; set; }

    [NotMapped]
    public string? DepartmentName { get; set; }

    [NotMapped]
    public string? ShiftCategoryName { get; set; }

    // Navigation property
    [ForeignKey("DepartmentId")]
    public virtual Department? Department { get; set; }

    // Computed properties
    [NotMapped]
    public bool IsCheckedIn => CheckoutTimestamp == null;

    [NotMapped]
    public TimeSpan? Duration => CheckoutTimestamp.HasValue
        ? CheckoutTimestamp.Value - CheckinTimestamp
        : DateTime.Now - CheckinTimestamp;

    [NotMapped]
    public double? HoursWorked => Duration?.TotalHours;

    [NotMapped]
    public string DurationDisplay
    {
        get
        {
            if (!Duration.HasValue) return "In Progress";
            var d = Duration.Value;
            return $"{(int)d.TotalHours}h {d.Minutes}m";
        }
    }
}
