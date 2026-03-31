using System.ComponentModel.DataAnnotations;
using CRCHTime.Models.Entities;

namespace CRCHTime.Models.ViewModels;

/// <summary>
/// View model for visit history and reporting
/// </summary>
public class VisitReportViewModel
{
    [Display(Name = "Start Date")]
    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [Display(Name = "End Date")]
    [DataType(DataType.Date)]
    public DateTime EndDate { get; set; } = DateTime.Today;

    [Display(Name = "SBU ID")]
    [StringLength(9)]
    public string? SBUID { get; set; }

    [Display(Name = "Building")]
    public int? BuildingId { get; set; }

    [Display(Name = "Application")]
    public string? Application { get; set; }

    public IEnumerable<Building> Buildings { get; set; } = new List<Building>();

    public IEnumerable<Visit> Visits { get; set; } = new List<Visit>();

    // Summary statistics
    public int TotalVisits => Visits.Count();

    public int UniqueVisitors => Visits
        .Select(v => v.SBUID)
        .Distinct()
        .Count();
}
