using CRCHTime.Models;
using CRCHTime.Models.Entities;
using CRCHTime.Models.ViewModels;

namespace CRCHTime.Services;

/// <summary>
/// Interface for Oracle stored procedure calls to WS_FC_CARDSWIPE package
/// </summary>
public interface IStoredProcService
{
    // Staff Management
    Task<IEnumerable<Staff>> GetAllStaffAsync(string application);
    Task<bool> AddUpdateStaffAsync(Staff staff);
    Task<OperationResult> StaffCheckinAsync(string netId, string hostname, string ip, string application, int? departmentId, int? shiftCategoryId = null);
    Task<OperationResult> StaffCheckoutAsync(string netId, string hostname, string ip, string application);

    // Shift Categories
    Task<IEnumerable<ShiftCategory>> GetShiftCategoriesAsync(string application);
    Task<OperationResult> AddUpdateShiftCategoryAsync(ShiftCategory category, string auditUser);
    Task<OperationResult> DeleteShiftCategoryAsync(int id, string auditUser);

    // Visit Tracking
    Task<bool> LogVisitAsync(string sbuid, string firstName, string lastName, string hostname, string ip, string location, string application, string? note, string netIdAudit);
    Task<IEnumerable<Visit>> GetVisitsAsync(DateTime startDate, DateTime endDate, string? sbuid, string? application, string? location);
    Task<IEnumerable<Visit>> GetRecentVisitsAsync(string application, DateTime date, int maxRows = 10);

    // Contractor Swipes
    Task<OperationResult> SwipeInAsync(string sbuid, string firstName, string lastName, string hostname, string ip, string application, string netIdAudit);
    Task<OperationResult> SwipeOutAsync(string sbuid, string hostname, string ip, string application, string netIdAudit);
    Task<IEnumerable<SwipeEntry>> GetSwipesAsync(DateTime startDate, DateTime endDate, string? sbuid, string? application);

    // Lookups
    Task<IEnumerable<Building>> GetBuildingsAsync(string? application);
    Task<IEnumerable<Department>> GetDepartmentsAsync(string? application);
    Task<IEnumerable<Company>> GetCompaniesAsync(string? application);
    Task<Department?> GetDepartmentForStaffAsync(string netId, string application);

    // Department Management (admin)
    Task<IEnumerable<Department>> GetAllDepartmentsAdminAsync(string? application);
    Task<OperationResult> AddUpdateDepartmentAsync(Department dept, string auditUser);
    Task<OperationResult> DeactivateDepartmentAsync(int deptId, string auditUser);

    // Associations
    Task<bool> AssociateNameAsync(string sbuid, string firstName, string lastName, string application);
    Task<IdAssociation?> GetAssociatedNameAsync(string sbuid, string application);
    Task<bool> AssociateCompanyAsync(string sbuid, int companyId, string application);
    Task<bool> InsertCompanyAsync(string companyName, string application);

    // Roles
    Task<IEnumerable<string>> GetAllRolesAsync(string application);
    Task<IEnumerable<string>> GetUserRolesAsync(string netId, string application);

    // Reports
    Task<IEnumerable<TimesheetEntry>> GetTimecardAsync(DateTime startDate, DateTime endDate, string? netId, int? departmentId, string? application);
    Task<OperationResult> UpdateTimesheetEntryAsync(string rowId, DateTime checkinTimestamp, DateTime? checkoutTimestamp, int? departmentId, int? shiftCategoryId, string auditUser);

    // Allotments
    Task<IEnumerable<Allotment>> GetAllotmentsAsync(string application, int year);
    Task<OperationResult> UpsertAllotmentAsync(string application, int year, int deptId, int categoryId, decimal? hours, string modifiedBy);
    Task<IEnumerable<(int DeptId, int CategoryId, double HoursUsed)>> GetHoursUsedAsync(string application, int year);

    // Student Info (from external procedures)
    Task<string?> GetRoomAsync(string sbuid);
    Task<int?> GetAgeAsync(string sbuid);
    Task<(string? FirstName, string? LastName)> GetStudentNameAsync(string sbuid);

    // User / Auth Lookups
    Task<(string? Name, string? Email)> GetUserInfoAsync(string netId);
    Task<(bool Found, bool IsTerminated, string? Role)> AuthLookupAsync(string netId, string application);

    // HID Card Lookups
    Task<string?> GetEmplIdFromHidAsync(string hidNum);

    // Dashboard KPIs
    Task<IEnumerable<VisitsByHostEntry>> GetVisitsByHostAsync(int year, int month, string application);
    Task<IEnumerable<DailyVisitEntry>> GetDailyVisitsAsync(DateTime startDate, DateTime endDate, string application);
}
