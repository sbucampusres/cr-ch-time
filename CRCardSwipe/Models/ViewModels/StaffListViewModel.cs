using System.ComponentModel.DataAnnotations;
using CRCardSwipe.Models.Entities;

namespace CRCardSwipe.Models.ViewModels;

/// <summary>
/// View model for staff management list
/// </summary>
public class StaffListViewModel
{
    public IEnumerable<StaffDisplayModel> Staff { get; set; } = new List<StaffDisplayModel>();

    public IEnumerable<Department> Departments { get; set; } = new List<Department>();

    [Display(Name = "Show Terminated")]
    public bool ShowTerminated { get; set; }

    [Display(Name = "Department")]
    public int? DepartmentFilter { get; set; }
}

/// <summary>
/// View model for displaying a staff member with additional info
/// </summary>
public class StaffDisplayModel
{
    public Staff Staff { get; set; } = null!;
    public string? DisplayName { get; set; }
    public string? Email { get; set; }
    public string? DepartmentName { get; set; }
    public bool IsCurrentlyCheckedIn { get; set; }
    public DateTime? LastCheckIn { get; set; }
}

/// <summary>
/// View model for staff check-in/check-out
/// </summary>
public class StaffCheckViewModel
{
    public string? NetId { get; set; }
    public string? DisplayName { get; set; }
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckInTime { get; set; }
    public string? CurrentLocation { get; set; }

    [Display(Name = "Department")]
    public int? DepartmentId { get; set; }

    public IEnumerable<Department> Departments { get; set; } = new List<Department>();
    public IEnumerable<Building> Buildings { get; set; } = new List<Building>();

    public string? StatusMessage { get; set; }
    public bool IsSuccess { get; set; }
}
