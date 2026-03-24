using System.ComponentModel.DataAnnotations;
using CRCardSwipe.Models.Entities;

namespace CRCardSwipe.Models.ViewModels;

/// <summary>
/// View model for contractor swipe reports
/// </summary>
public class SwipeReportViewModel
{
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(-7);

    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today;

    [Display(Name = "Company")]
    public int? CompanyId { get; set; }

    [Display(Name = "SBU ID")]
    [StringLength(9)]
    public string? SBUID { get; set; }

    public IEnumerable<Company> Companies { get; set; } = new List<Company>();

    public IEnumerable<SwipeEntry> Swipes { get; set; } = new List<SwipeEntry>();

    // Summary statistics
    public int TotalSwipes => Swipes.Count();

    public int CurrentlyOnSite => Swipes.Count(s => s.IsSwipedIn);

    public double TotalHours => Swipes
        .Where(s => s.HoursWorked.HasValue)
        .Sum(s => s.HoursWorked!.Value);

    public string TotalHoursDisplay => $"{Math.Floor(TotalHours)}h {(int)((TotalHours % 1) * 60)}m";
}
