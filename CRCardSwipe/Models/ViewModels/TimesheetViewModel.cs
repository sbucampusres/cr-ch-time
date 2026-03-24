using System.ComponentModel.DataAnnotations;
using CRCardSwipe.Models.Entities;

namespace CRCardSwipe.Models.ViewModels;

/// <summary>
/// View model for timesheet display and filtering
/// </summary>
public class TimesheetViewModel
{
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-7);

    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today;

    [Display(Name = "NetID")]
    public string? NetId { get; set; }

    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    public IEnumerable<Department> Departments { get; set; } = new List<Department>();

    public IEnumerable<TimesheetEntry> Entries { get; set; } = new List<TimesheetEntry>();

    // Summary statistics
    public int TotalEntries => Entries.Count();

    public double TotalHours => Entries
        .Where(e => e.HoursWorked.HasValue)
        .Sum(e => e.HoursWorked!.Value);

    public string TotalHoursDisplay => $"{Math.Floor(TotalHours)}h {(int)((TotalHours % 1) * 60)}m";
}

/// <summary>
/// View model for a single timesheet entry with display name
/// </summary>
public class TimesheetEntryViewModel
{
    public TimesheetEntry Entry { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? DepartmentName { get; set; }
}
