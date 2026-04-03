using Microsoft.EntityFrameworkCore;
using Oracle.ManagedDataAccess.Client;
using Oracle.ManagedDataAccess.Types;
using CRCHTime.Data;
using CRCHTime.Models;
using CRCHTime.Models.Entities;
using CRCHTime.Models.ViewModels;
using System.Data;

namespace CRCHTime.Services;

/// <summary>
/// Implementation of stored procedure calls to WS_CR_CARDSWIPE Oracle package.
/// All database operations go through the package - no direct SQL queries.
/// Procedures use CRFCCS_ prefix (Campus Residences Fitness Center Card Swipe).
/// Many procedures return errors via OUT parameters instead of throwing exceptions.
/// </summary>
public class StoredProcService : IStoredProcService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<StoredProcService> _logger;

    public StoredProcService(
        ApplicationDbContext context,
        ILogger<StoredProcService> logger)
    {
        _context = context;
        _logger = logger;
    }

    private string GetConnectionString()
    {
        return _context.Database.GetConnectionString() ?? string.Empty;
    }

    #region Staff Management

    public async Task<IEnumerable<Staff>> GetAllStaffAsync(string application)
    {
        var staffList = new List<Staff>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CH.CRCH_GET_STAFF";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application ?? (object)DBNull.Value;
            command.Parameters.Add("r_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                staffList.Add(MapStaffFromReader(reader));
            }
            return staffList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all staff for application {Application}", application);
            return Enumerable.Empty<Staff>();
        }
    }

    public async Task<bool> AddUpdateStaffAsync(Staff staff)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CH.CRCH_ADD_UPDATE_STAFF";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_netid", OracleDbType.Varchar2).Value = staff.NetId;
            command.Parameters.Add("p_hostname", OracleDbType.Varchar2).Value = staff.Hostname ?? (object)DBNull.Value;
            command.Parameters.Add("p_terminationdate", OracleDbType.Date).Value = staff.TerminationDate ?? (object)DBNull.Value;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = staff.Application;
            command.Parameters.Add("p_role", OracleDbType.Varchar2).Value = staff.Role ?? (object)DBNull.Value;
            command.Parameters.Add("p_department", OracleDbType.Varchar2).Value = staff.DeptId ?? (object)DBNull.Value;

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding/updating staff {NetId}", staff.NetId);
            return false;
        }
    }

    public async Task<OperationResult> StaffCheckinAsync(string netId, string hostname, string ip, string application, int? departmentId, int? shiftCategoryId = null)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CH.CRCH_STAFF_CHECKIN";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_netid", OracleDbType.Varchar2).Value = netId;
            command.Parameters.Add("p_hostname", OracleDbType.Varchar2).Value = hostname ?? (object)DBNull.Value;
            command.Parameters.Add("p_ip", OracleDbType.Varchar2).Value = ip ?? (object)DBNull.Value;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;
            command.Parameters.Add("p_department_id", OracleDbType.Int32).Value = departmentId ?? (object)DBNull.Value;
            command.Parameters.Add("p_shift_category_id", OracleDbType.Int32).Value = shiftCategoryId ?? (object)DBNull.Value;

            var errorParam = new OracleParameter("r_error", OracleDbType.Varchar2, 500);
            errorParam.Direction = ParameterDirection.Output;
            command.Parameters.Add(errorParam);

            await command.ExecuteNonQueryAsync();

            var errorValue = errorParam.Value;
            var errorMessage = (errorValue != null && errorValue != DBNull.Value)
                ? errorValue.ToString()
                : string.Empty;

            if (errorMessage == "null") errorMessage = string.Empty;

            return string.IsNullOrEmpty(errorMessage)
                ? OperationResult.Succeeded()
                : OperationResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking in staff {NetId}", netId);
            return OperationResult.Failed("An error occurred during check-in. Please try again.");
        }
    }

    public async Task<OperationResult> StaffCheckoutAsync(string netId, string hostname, string ip, string application)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_STAFF_CHECKOUT";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_netid", OracleDbType.Varchar2).Value = netId;
            command.Parameters.Add("p_hostname", OracleDbType.Varchar2).Value = hostname ?? (object)DBNull.Value;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;
            command.Parameters.Add("p_ip", OracleDbType.Varchar2).Value = ip ?? (object)DBNull.Value;

            var errorParam = new OracleParameter("r_error", OracleDbType.Varchar2, 500);
            errorParam.Direction = ParameterDirection.Output;
            command.Parameters.Add(errorParam);

            await command.ExecuteNonQueryAsync();

            // Oracle OUT parameters may return OracleString.Null which .ToString() returns "null"
            var errorValue = errorParam.Value;
            var errorMessage = (errorValue != null && errorValue != DBNull.Value)
                ? errorValue.ToString()
                : string.Empty;

            // Check for Oracle's literal "null" string
            if (errorMessage == "null") errorMessage = string.Empty;

            return string.IsNullOrEmpty(errorMessage)
                ? OperationResult.Succeeded()
                : OperationResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking out staff {NetId}", netId);
            return OperationResult.Failed("An error occurred during check-out. Please try again.");
        }
    }

    #endregion

    #region Visit Tracking

    public async Task<bool> LogVisitAsync(string sbuid, string firstName, string lastName, string hostname, string ip, string location, string application, string? note, string netIdAudit)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_VISIT";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_sbuid", OracleDbType.Int32).Value = int.Parse(sbuid);
            command.Parameters.Add("p_location", OracleDbType.Varchar2).Value = location ?? (object)DBNull.Value;
            command.Parameters.Add("p_hostname", OracleDbType.Varchar2).Value = hostname ?? (object)DBNull.Value;
            command.Parameters.Add("p_firstname", OracleDbType.Varchar2).Value = firstName ?? (object)DBNull.Value;
            command.Parameters.Add("p_lastname", OracleDbType.Varchar2).Value = lastName ?? (object)DBNull.Value;
            command.Parameters.Add("p_ip", OracleDbType.Varchar2).Value = ip ?? (object)DBNull.Value;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;
            command.Parameters.Add("p_note", OracleDbType.Varchar2).Value = note ?? (object)DBNull.Value;
            command.Parameters.Add("p_netid", OracleDbType.Varchar2).Value = netIdAudit ?? (object)DBNull.Value;

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging visit for SBUID {SBUID}", sbuid);
            return false;
        }
    }

    public async Task<IEnumerable<Visit>> GetVisitsAsync(DateTime startDate, DateTime endDate, string? sbuid, string? application, string? location)
    {
        var visits = new List<Visit>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_GET_VISITS";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_begindate", OracleDbType.Date).Value = startDate;
            command.Parameters.Add("p_enddate", OracleDbType.Date).Value = endDate.AddDays(1);
            command.Parameters.Add("p_location", OracleDbType.Varchar2).Value = location ?? (object)DBNull.Value;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application ?? (object)DBNull.Value;
            command.Parameters.Add("p_netid", OracleDbType.Varchar2).Value = sbuid ?? (object)DBNull.Value;
            command.Parameters.Add("cv_results", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                visits.Add(MapVisitFromReader(reader));
            }
            return visits;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting visits");
            return Enumerable.Empty<Visit>();
        }
    }

    public async Task<IEnumerable<Visit>> GetRecentVisitsAsync(string application, DateTime date, int maxRows = 10)
    {
        var visits = new List<Visit>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_GET_RECENT_VISITS";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;
            command.Parameters.Add("p_date", OracleDbType.Date).Value = date.Date;
            command.Parameters.Add("p_max_rows", OracleDbType.Int32).Value = maxRows;
            command.Parameters.Add("cv_results", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                visits.Add(MapRecentVisitFromReader(reader));
            }
            return visits;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent visits for application {Application}", application);
            return Enumerable.Empty<Visit>();
        }
    }

    #endregion

    #region Contractor Swipes

    public async Task<OperationResult> SwipeInAsync(string sbuid, string firstName, string lastName, string hostname, string ip, string application, string netIdAudit)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_SWIPE_IN";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_netid", OracleDbType.Varchar2).Value = netIdAudit ?? (object)DBNull.Value;
            command.Parameters.Add("p_sbuid", OracleDbType.Int32).Value = int.Parse(sbuid);
            command.Parameters.Add("p_hostname", OracleDbType.Varchar2).Value = hostname ?? (object)DBNull.Value;
            command.Parameters.Add("p_ip", OracleDbType.Varchar2).Value = ip ?? (object)DBNull.Value;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;
            command.Parameters.Add("p_firstname", OracleDbType.Varchar2).Value = firstName ?? (object)DBNull.Value;
            command.Parameters.Add("p_lastname", OracleDbType.Varchar2).Value = lastName ?? (object)DBNull.Value;

            var errorParam = new OracleParameter("r_error", OracleDbType.Varchar2, 500);
            errorParam.Direction = ParameterDirection.Output;
            command.Parameters.Add(errorParam);

            await command.ExecuteNonQueryAsync();

            var errorMessage = errorParam.Value?.ToString() ?? string.Empty;

            return string.IsNullOrEmpty(errorMessage)
                ? OperationResult.Succeeded()
                : OperationResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error swiping in SBUID {SBUID}", sbuid);
            return OperationResult.Failed("An error occurred during swipe in. Please try again.");
        }
    }

    public async Task<OperationResult> SwipeOutAsync(string sbuid, string hostname, string ip, string application, string netIdAudit)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_SWIPE_OUT";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_netid", OracleDbType.Varchar2).Value = netIdAudit ?? (object)DBNull.Value;
            command.Parameters.Add("p_sbuid", OracleDbType.Int32).Value = int.Parse(sbuid);
            command.Parameters.Add("p_hostname", OracleDbType.Varchar2).Value = hostname ?? (object)DBNull.Value;
            command.Parameters.Add("p_ip", OracleDbType.Varchar2).Value = ip ?? (object)DBNull.Value;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;
            command.Parameters.Add("p_firstname", OracleDbType.Varchar2).Value = DBNull.Value;
            command.Parameters.Add("p_lastname", OracleDbType.Varchar2).Value = DBNull.Value;

            var errorParam = new OracleParameter("r_error", OracleDbType.Varchar2, 500);
            errorParam.Direction = ParameterDirection.Output;
            command.Parameters.Add(errorParam);

            await command.ExecuteNonQueryAsync();

            var errorMessage = errorParam.Value?.ToString() ?? string.Empty;

            return string.IsNullOrEmpty(errorMessage)
                ? OperationResult.Succeeded()
                : OperationResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error swiping out SBUID {SBUID}", sbuid);
            return OperationResult.Failed("An error occurred during swipe out. Please try again.");
        }
    }

    public async Task<IEnumerable<SwipeEntry>> GetSwipesAsync(DateTime startDate, DateTime endDate, string? sbuid, string? application)
    {
        var swipes = new List<SwipeEntry>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_GET_SWIPES";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_begindate", OracleDbType.Date).Value = startDate;
            command.Parameters.Add("p_enddate", OracleDbType.Date).Value = endDate.AddDays(1);
            command.Parameters.Add("p_sbuid", OracleDbType.Int32).Value = string.IsNullOrEmpty(sbuid) ? (object)DBNull.Value : int.Parse(sbuid);
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application ?? (object)DBNull.Value;
            command.Parameters.Add("cv_results", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                swipes.Add(MapSwipeEntryFromReader(reader));
            }
            return swipes;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting swipes");
            return Enumerable.Empty<SwipeEntry>();
        }
    }

    #endregion

    #region Lookups

    public async Task<IEnumerable<Building>> GetBuildingsAsync(string? application)
    {
        var buildings = new List<Building>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_GET_BUILDINGS";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application ?? (object)DBNull.Value;
            command.Parameters.Add("cv_results", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                buildings.Add(MapBuildingFromReader(reader));
            }
            return buildings;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting buildings");
            return Enumerable.Empty<Building>();
        }
    }

    public async Task<IEnumerable<Department>> GetDepartmentsAsync(string? application)
    {
        var departments = new List<Department>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_GET_ALL_DEPARTMENTS";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application ?? (object)DBNull.Value;
            command.Parameters.Add("cv_results", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                departments.Add(MapDepartmentFromReader(reader));
            }
            return departments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting departments");
            return Enumerable.Empty<Department>();
        }
    }

    public async Task<IEnumerable<Company>> GetCompaniesAsync(string? application)
    {
        var companies = new List<Company>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_GET_COMPANIES";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application ?? (object)DBNull.Value;
            command.Parameters.Add("cv_results", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                companies.Add(MapCompanyFromReader(reader));
            }
            return companies;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting companies");
            return Enumerable.Empty<Company>();
        }
    }

    public async Task<Department?> GetDepartmentForStaffAsync(string netId, string application)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_GET_DEPARTMENT";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;
            command.Parameters.Add("p_netid", OracleDbType.Varchar2).Value = netId;
            command.Parameters.Add("cv_results", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var raw = reader.GetValue(0);
                if (raw == DBNull.Value || !int.TryParse(raw.ToString(), out var deptId))
                    return null;
                // Now get full department details from all departments
                var allDepts = await GetDepartmentsAsync(application);
                return allDepts.FirstOrDefault(d => d.DeptId == deptId);
            }
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting department for staff {NetId}", netId);
            return null;
        }
    }

    #endregion

    #region Department Management

    public async Task<IEnumerable<Department>> GetAllDepartmentsAdminAsync(string? application)
    {
        var departments = new List<Department>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CH.CRCH_GET_ALL_DEPARTMENTS_ADMIN";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application ?? (object)DBNull.Value;
            command.Parameters.Add("r_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                departments.Add(MapDepartmentFromReader(reader));
            }
            return departments;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all departments (admin) for application {Application}", application);
            return Enumerable.Empty<Department>();
        }
    }

    public async Task<OperationResult> AddUpdateDepartmentAsync(Department dept, string auditUser)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CH.CRCH_ADD_UPDATE_DEPARTMENT";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_dept_id", OracleDbType.Int32).Value = dept.DeptId == 0 ? (object)DBNull.Value : dept.DeptId;
            command.Parameters.Add("p_name", OracleDbType.Varchar2).Value = dept.Name;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = dept.Application ?? (object)DBNull.Value;
            command.Parameters.Add("p_inactive", OracleDbType.Int32).Value = dept.Inactive ? 1 : 0;
            command.Parameters.Add("p_user", OracleDbType.Varchar2).Value = auditUser;

            var errorParam = new OracleParameter("r_error", OracleDbType.Varchar2, 500);
            errorParam.Direction = ParameterDirection.Output;
            command.Parameters.Add(errorParam);

            await command.ExecuteNonQueryAsync();

            var errorValue = errorParam.Value;
            var errorMessage = (errorValue != null && errorValue != DBNull.Value)
                ? errorValue.ToString() : string.Empty;
            if (errorMessage == "null") errorMessage = string.Empty;

            return string.IsNullOrEmpty(errorMessage)
                ? OperationResult.Succeeded()
                : OperationResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving department {Name}", dept.Name);
            return OperationResult.Failed("An error occurred while saving the department.");
        }
    }

    public async Task<OperationResult> DeactivateDepartmentAsync(int deptId, string auditUser)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CH.CRCH_DEACTIVATE_DEPARTMENT";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_dept_id", OracleDbType.Int32).Value = deptId;
            command.Parameters.Add("p_user", OracleDbType.Varchar2).Value = auditUser;

            var errorParam = new OracleParameter("r_error", OracleDbType.Varchar2, 500);
            errorParam.Direction = ParameterDirection.Output;
            command.Parameters.Add(errorParam);

            await command.ExecuteNonQueryAsync();

            var errorValue = errorParam.Value;
            var errorMessage = (errorValue != null && errorValue != DBNull.Value)
                ? errorValue.ToString() : string.Empty;
            if (errorMessage == "null") errorMessage = string.Empty;

            return string.IsNullOrEmpty(errorMessage)
                ? OperationResult.Succeeded()
                : OperationResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deactivating department {DeptId}", deptId);
            return OperationResult.Failed("An error occurred while deactivating the department.");
        }
    }

    #endregion

    #region Associations

    public async Task<bool> AssociateNameAsync(string sbuid, string firstName, string lastName, string application)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_ASSOCIATE_NAME";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_sbuid", OracleDbType.Int32).Value = int.Parse(sbuid);
            command.Parameters.Add("p_firstname", OracleDbType.Varchar2).Value = firstName;
            command.Parameters.Add("p_lastname", OracleDbType.Varchar2).Value = lastName;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error associating name for SBUID {SBUID}", sbuid);
            return false;
        }
    }

    public async Task<IdAssociation?> GetAssociatedNameAsync(string sbuid, string application)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_GET_ASSOC_NAME";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_sbuid", OracleDbType.Int32).Value = int.Parse(sbuid);
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;

            var fnameParam = new OracleParameter("r_firstname", OracleDbType.Varchar2, 100);
            fnameParam.Direction = ParameterDirection.Output;
            command.Parameters.Add(fnameParam);

            var lnameParam = new OracleParameter("r_lastname", OracleDbType.Varchar2, 100);
            lnameParam.Direction = ParameterDirection.Output;
            command.Parameters.Add(lnameParam);

            await command.ExecuteNonQueryAsync();

            // Oracle OUT parameters return OracleString.Null when null,
            // and .ToString() returns the literal string "null"
            var fnameValue = fnameParam.Value;
            var lnameValue = lnameParam.Value;

            var firstName = (fnameValue != null && fnameValue != DBNull.Value)
                ? fnameValue.ToString()
                : null;
            var lastName = (lnameValue != null && lnameValue != DBNull.Value)
                ? lnameValue.ToString()
                : null;

            // Check for Oracle's literal "null" string
            if (firstName == "null") firstName = null;
            if (lastName == "null") lastName = null;

            if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName))
                return null;

            return new IdAssociation
            {
                SBUID = sbuid,
                Application = application,
                FirstName = firstName,
                LastName = lastName
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting associated name for SBUID {SBUID}", sbuid);
            return null;
        }
    }

    public async Task<bool> AssociateCompanyAsync(string sbuid, int companyId, string application)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_ASSOCIATE_COMPANY";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_sbuid", OracleDbType.Int32).Value = int.Parse(sbuid);
            command.Parameters.Add("p_company_id", OracleDbType.Int32).Value = companyId;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error associating company for SBUID {SBUID}", sbuid);
            return false;
        }
    }

    public async Task<bool> InsertCompanyAsync(string companyName, string application)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_INSERT_COMPANY";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_name", OracleDbType.Varchar2).Value = companyName;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;

            await command.ExecuteNonQueryAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error inserting company {CompanyName}", companyName);
            return false;
        }
    }

    #endregion

    #region Shift Categories

    public async Task<IEnumerable<ShiftCategory>> GetShiftCategoriesAsync(string application)
    {
        var categories = new List<ShiftCategory>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CH.CRCH_GET_SHIFT_CATEGORIES";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;
            command.Parameters.Add("r_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categories.Add(new ShiftCategory
                {
                    Id          = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("ID"))),
                    Name        = reader.GetString(reader.GetOrdinal("NAME")),
                    Description = reader.IsDBNull(reader.GetOrdinal("DESCRIPTION")) ? null : reader.GetString(reader.GetOrdinal("DESCRIPTION")),
                    Application = reader.GetString(reader.GetOrdinal("APPLICATION")),
                    IsActive    = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("IS_ACTIVE"))) == 1
                });
            }
            return categories;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting shift categories for application {Application}", application);
            return Enumerable.Empty<ShiftCategory>();
        }
    }

    public async Task<OperationResult> AddUpdateShiftCategoryAsync(ShiftCategory category, string auditUser)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CH.CRCH_ADD_UPDATE_SHIFT_CATEGORY";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_id", OracleDbType.Int32).Value = category.Id == 0 ? (object)DBNull.Value : category.Id;
            command.Parameters.Add("p_name", OracleDbType.Varchar2).Value = category.Name;
            command.Parameters.Add("p_description", OracleDbType.Varchar2).Value = category.Description ?? (object)DBNull.Value;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = category.Application;
            command.Parameters.Add("p_is_active", OracleDbType.Int32).Value = category.IsActive ? 1 : 0;
            command.Parameters.Add("p_user", OracleDbType.Varchar2).Value = auditUser;

            var errorParam = new OracleParameter("r_error", OracleDbType.Varchar2, 500);
            errorParam.Direction = ParameterDirection.Output;
            command.Parameters.Add(errorParam);

            await command.ExecuteNonQueryAsync();

            var errorValue = errorParam.Value;
            var errorMessage = (errorValue != null && errorValue != DBNull.Value)
                ? errorValue.ToString() : string.Empty;
            if (errorMessage == "null") errorMessage = string.Empty;

            return string.IsNullOrEmpty(errorMessage)
                ? OperationResult.Succeeded()
                : OperationResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving shift category {Name}", category.Name);
            return OperationResult.Failed("An error occurred while saving the shift category.");
        }
    }

    public async Task<OperationResult> DeleteShiftCategoryAsync(int id, string auditUser)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CH.CRCH_DELETE_SHIFT_CATEGORY";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_id", OracleDbType.Int32).Value = id;
            command.Parameters.Add("p_user", OracleDbType.Varchar2).Value = auditUser;

            var errorParam = new OracleParameter("r_error", OracleDbType.Varchar2, 500);
            errorParam.Direction = ParameterDirection.Output;
            command.Parameters.Add(errorParam);

            await command.ExecuteNonQueryAsync();

            var errorValue = errorParam.Value;
            var errorMessage = (errorValue != null && errorValue != DBNull.Value)
                ? errorValue.ToString() : string.Empty;
            if (errorMessage == "null") errorMessage = string.Empty;

            return string.IsNullOrEmpty(errorMessage)
                ? OperationResult.Succeeded()
                : OperationResult.Failed(errorMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting shift category {Id}", id);
            return OperationResult.Failed("An error occurred while deleting the shift category.");
        }
    }

    #endregion

    #region Reports

    public async Task<IEnumerable<TimesheetEntry>> GetTimecardAsync(DateTime startDate, DateTime endDate, string? netId, int? departmentId, string? application)
    {
        var entries = new List<TimesheetEntry>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CH.CRCH_GET_TIMECARD";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_begindate", OracleDbType.Date).Value = startDate;
            command.Parameters.Add("p_enddate", OracleDbType.Date).Value = endDate.AddDays(1);
            command.Parameters.Add("p_netid", OracleDbType.Varchar2).Value = netId ?? (object)DBNull.Value;
            command.Parameters.Add("p_department", OracleDbType.Varchar2).Value = departmentId?.ToString() ?? (object)DBNull.Value;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application ?? (object)DBNull.Value;
            command.Parameters.Add("r_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                entries.Add(MapTimesheetEntryFromReader(reader));
            }
            return entries;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting timecard");
            return Enumerable.Empty<TimesheetEntry>();
        }
    }

    #endregion

    #region Student Info

    public async Task<string?> GetRoomAsync(string sbuid)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_GET_ROOM";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_sbuid", OracleDbType.Int32).Value = int.Parse(sbuid);
            var outParam = command.Parameters.Add("r_room", OracleDbType.Varchar2, 100);
            outParam.Direction = ParameterDirection.Output;

            await command.ExecuteNonQueryAsync();

            return outParam.Value is OracleString os && !os.IsNull ? os.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting room for SBUID {SBUID}", sbuid);
            return null;
        }
    }

    public async Task<int?> GetAgeAsync(string sbuid)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_GET_AGE";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_sbuid", OracleDbType.Int32).Value = int.Parse(sbuid);
            var outParam = command.Parameters.Add("r_age", OracleDbType.Int32);
            outParam.Direction = ParameterDirection.Output;

            await command.ExecuteNonQueryAsync();

            var result = outParam.Value;
            if (result == DBNull.Value || result == null)
                return null;
            return Convert.ToInt32(result);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting age for SBUID {SBUID}", sbuid);
            return null;
        }
    }

    public async Task<(string? FirstName, string? LastName)> GetStudentNameAsync(string sbuid)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_GET_NAME";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_sbuid", OracleDbType.Int32).Value = int.Parse(sbuid);
            var firstNameParam = command.Parameters.Add("r_fname", OracleDbType.Varchar2, 100);
            firstNameParam.Direction = ParameterDirection.Output;
            var lastNameParam = command.Parameters.Add("r_lname", OracleDbType.Varchar2, 100);
            lastNameParam.Direction = ParameterDirection.Output;

            await command.ExecuteNonQueryAsync();

            var firstName = firstNameParam.Value is OracleString fn && !fn.IsNull ? fn.Value : null;
            var lastName  = lastNameParam.Value  is OracleString ln && !ln.IsNull ? ln.Value : null;
            return (firstName, lastName);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error getting student name for SBUID {SBUID}", sbuid);
            return (null, null);
        }
    }

    #endregion

    #region Roles

    public async Task<IEnumerable<string>> GetAllRolesAsync(string application)
    {
        var roles = new List<string>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_GET_ALL_ROLES";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application ?? (object)DBNull.Value;
            command.Parameters.Add("cv_results", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var role = reader.IsDBNull(0) ? null : reader.GetString(0);
                if (!string.IsNullOrEmpty(role))
                    roles.Add(role);
            }
            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting all roles for application {Application}", application);
            return Enumerable.Empty<string>();
        }
    }

    public async Task<IEnumerable<string>> GetUserRolesAsync(string netId, string application)
    {
        var roles = new List<string>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CH.CRCH_GET_USER_ROLES";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_netid", OracleDbType.Varchar2).Value = netId;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application ?? (object)DBNull.Value;
            command.Parameters.Add("r_cursor", OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var role = reader.IsDBNull(0) ? null : reader.GetString(0);
                if (!string.IsNullOrEmpty(role))
                    roles.Add(role);
            }
            return roles;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting roles for staff {NetId}", netId);
            return Enumerable.Empty<string>();
        }
    }

    #endregion

    #region User / Auth Lookups

    public async Task<(string? Name, string? Email)> GetUserInfoAsync(string netId)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_GET_USER_INFO";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_netid", OracleDbType.Varchar2).Value = netId;
            var nameParam  = new OracleParameter("r_name",  OracleDbType.Varchar2, 200) { Direction = ParameterDirection.Output };
            var emailParam = new OracleParameter("r_email", OracleDbType.Varchar2, 200) { Direction = ParameterDirection.Output };
            command.Parameters.Add(nameParam);
            command.Parameters.Add(emailParam);

            await command.ExecuteNonQueryAsync();

            var name  = nameParam.Value  == DBNull.Value ? null : nameParam.Value?.ToString();
            var email = emailParam.Value == DBNull.Value ? null : emailParam.Value?.ToString();
            if (name == "null")  name  = null;
            if (email == "null") email = null;
            return (name, email);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user info for NetID {NetId}", netId);
            return (null, null);
        }
    }

    public async Task<(bool Found, bool IsTerminated, string? Role)> AuthLookupAsync(string netId, string application)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_AUTH_LOOKUP";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_netid",       OracleDbType.Varchar2).Value = netId;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;
            var roleParam = new OracleParameter("r_role", OracleDbType.Varchar2, 50) { Direction = ParameterDirection.Output };
            var termParam = new OracleParameter("r_terminationdate", OracleDbType.Date) { Direction = ParameterDirection.Output };
            var foundParam = new OracleParameter("r_found", OracleDbType.Int32) { Direction = ParameterDirection.Output };
            command.Parameters.Add(roleParam);
            command.Parameters.Add(termParam);
            command.Parameters.Add(foundParam);

            await command.ExecuteNonQueryAsync();

            var foundVal = foundParam.Value is OracleDecimal fd ? (int)fd : 0;
            if (foundVal == 0)
                return (false, false, null);

            var role = roleParam.Value is OracleString rs && !rs.IsNull ? rs.Value : null;

            DateTime? termDate = termParam.Value is OracleDate od && !od.IsNull ? od.Value : null;
            var isTerminated = termDate.HasValue && termDate.Value.Date <= DateTime.Today;

            return (true, isTerminated, role);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auth lookup for NetID {NetId} application {Application}", netId, application);
            return (false, false, null);
        }
    }

    #endregion

    #region HID Card Lookups

    public async Task<string?> GetEmplIdFromHidAsync(string hidNum)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE_HID.CRFCCS_GET_EMPLID_FROM_HID";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_hidnum", OracleDbType.Varchar2).Value = hidNum;
            var emplIdParam = new OracleParameter("r_emplid", OracleDbType.Varchar2, 20) { Direction = ParameterDirection.Output };
            command.Parameters.Add(emplIdParam);

            await command.ExecuteNonQueryAsync();

            var result = emplIdParam.Value is OracleString os && !os.IsNull ? os.Value : null;
            _logger.LogInformation("HID lookup for HIDNUM {HidNum}: EMPLID={EmplId}", hidNum, result ?? "not found");
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error looking up EMPLID for HIDNUM {HidNum}", hidNum);
            return null;
        }
    }

    #endregion

    #region Dashboard KPIs

    public async Task<IEnumerable<VisitsByHostEntry>> GetVisitsByHostAsync(int year, int month, string application)
    {
        var results = new List<VisitsByHostEntry>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_GET_VISITS_BY_HOST";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_year",        OracleDbType.Int32).Value   = year;
            command.Parameters.Add("p_month",       OracleDbType.Int32).Value   = month;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;
            var cursor = new OracleParameter("cv_results", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };
            command.Parameters.Add(cursor);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new VisitsByHostEntry(
                    reader.GetString(reader.GetOrdinal("HOSTNAME")),
                    Convert.ToInt32(reader.GetValue(reader.GetOrdinal("VISIT_COUNT")))
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching visits by host for {Year}-{Month}", year, month);
        }
        return results;
    }

    public async Task<IEnumerable<DailyVisitEntry>> GetDailyVisitsAsync(DateTime startDate, DateTime endDate, string application)
    {
        var results = new List<DailyVisitEntry>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CARDSWIPE.CRFCCS_GET_DAILY_VISITS";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_startdate",   OracleDbType.Date).Value    = startDate;
            command.Parameters.Add("p_enddate",     OracleDbType.Date).Value    = endDate;
            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;
            var cursor = new OracleParameter("cv_results", OracleDbType.RefCursor) { Direction = ParameterDirection.Output };
            command.Parameters.Add(cursor);

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new DailyVisitEntry(
                    reader.GetDateTime(reader.GetOrdinal("VISIT_DATE")),
                    Convert.ToInt32(reader.GetValue(reader.GetOrdinal("VISIT_COUNT")))
                ));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching daily visits from {Start} to {End}", startDate, endDate);
        }
        return results;
    }

    #endregion

    #region Mapping Helpers

    private static Staff MapStaffFromReader(IDataReader reader)
    {
        return new Staff
        {
            NetId = reader.GetString(reader.GetOrdinal("NETID")),
            TerminationDate = reader.IsDBNull(reader.GetOrdinal("TERMINATIONDATE"))
                ? null : reader.GetDateTime(reader.GetOrdinal("TERMINATIONDATE")),
            Role = reader.IsDBNull(reader.GetOrdinal("ROLE"))
                ? null : reader.GetString(reader.GetOrdinal("ROLE")),
            DeptId = reader.IsDBNull(reader.GetOrdinal("DEPT_ID"))
                ? null : reader.GetValue(reader.GetOrdinal("DEPT_ID"))?.ToString()
        };
    }

    // Maps CRFCCS_GET_RECENT_VISITS result set — columns come directly from
    // WS_FCVISITS so names are FIRSTNAME/LASTNAME (not the aliased FIRST_NAME/LAST_NAME
    // that GET_VISITS uses via the JS_V_PERSONALINFO join).
    private static Visit MapRecentVisitFromReader(IDataReader reader)
    {
        return new Visit
        {
            SBUID        = reader.GetValue(reader.GetOrdinal("SBUID"))?.ToString() ?? string.Empty,
            FirstName    = reader.IsDBNull(reader.GetOrdinal("FIRSTNAME"))   ? null : reader.GetString(reader.GetOrdinal("FIRSTNAME")),
            LastName     = reader.IsDBNull(reader.GetOrdinal("LASTNAME"))    ? null : reader.GetString(reader.GetOrdinal("LASTNAME")),
            SwipeTime    = reader.GetDateTime(reader.GetOrdinal("SWIPETIME")),
            Location     = reader.IsDBNull(reader.GetOrdinal("LOCATION"))    ? null : reader.GetString(reader.GetOrdinal("LOCATION")),
            Note         = reader.IsDBNull(reader.GetOrdinal("NOTE"))        ? null : reader.GetString(reader.GetOrdinal("NOTE")),
            NetIdAudit   = reader.IsDBNull(reader.GetOrdinal("NETID_AUDIT")) ? null : reader.GetString(reader.GetOrdinal("NETID_AUDIT")),
        };
    }

    private static Visit MapVisitFromReader(IDataReader reader)
    {
        // Column names from CRFCCS_GET_VISITS stored procedure:
        // SBUID, FIRST_NAME, LAST_NAME, SWIPETIME, Build, NOTE, NETID_DISPLAY, CK_BED_SPACE, EMAIL_ADDR
        return new Visit
        {
            // SBUID is a NUMBER in Oracle, use GetValue to handle type conversion
            SBUID = reader.GetValue(reader.GetOrdinal("SBUID"))?.ToString() ?? string.Empty,
            FirstName = reader.IsDBNull(reader.GetOrdinal("FIRST_NAME"))
                ? null : reader.GetString(reader.GetOrdinal("FIRST_NAME")),
            LastName = reader.IsDBNull(reader.GetOrdinal("LAST_NAME"))
                ? null : reader.GetString(reader.GetOrdinal("LAST_NAME")),
            SwipeTime = reader.GetDateTime(reader.GetOrdinal("SWIPETIME")),
            // Column alias is "Build" (mixed case) in the stored procedure
            Location = reader.IsDBNull(reader.GetOrdinal("Build"))
                ? null : reader.GetString(reader.GetOrdinal("Build")),
            Note = reader.IsDBNull(reader.GetOrdinal("NOTE"))
                ? null : reader.GetString(reader.GetOrdinal("NOTE")),
            NetIdAudit = reader.IsDBNull(reader.GetOrdinal("NETID_DISPLAY"))
                ? null : reader.GetString(reader.GetOrdinal("NETID_DISPLAY"))
        };
    }

    private static SwipeEntry MapSwipeEntryFromReader(IDataReader reader)
    {
        return new SwipeEntry
        {
            Id = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("ID"))),
            SBUID = reader.GetString(reader.GetOrdinal("SBUID")),
            SwipeTimeIn = reader.GetDateTime(reader.GetOrdinal("SWIPE_TIME_IN")),
            SwipeTimeOut = reader.IsDBNull(reader.GetOrdinal("SWIPE_TIME_OUT"))
                ? null : reader.GetDateTime(reader.GetOrdinal("SWIPE_TIME_OUT")),
            NetIdIn = reader.IsDBNull(reader.GetOrdinal("NETID_IN"))
                ? null : reader.GetString(reader.GetOrdinal("NETID_IN")),
            NetIdOut = reader.IsDBNull(reader.GetOrdinal("NETID_OUT"))
                ? null : reader.GetString(reader.GetOrdinal("NETID_OUT")),
            HostnameIn = reader.IsDBNull(reader.GetOrdinal("HOSTNAME_IN"))
                ? null : reader.GetString(reader.GetOrdinal("HOSTNAME_IN")),
            HostnameOut = reader.IsDBNull(reader.GetOrdinal("HOSTNAME_OUT"))
                ? null : reader.GetString(reader.GetOrdinal("HOSTNAME_OUT")),
            Application = reader.IsDBNull(reader.GetOrdinal("APPLICATION"))
                ? null : reader.GetString(reader.GetOrdinal("APPLICATION")),
            FirstName = reader.IsDBNull(reader.GetOrdinal("FIRSTNAME"))
                ? null : reader.GetString(reader.GetOrdinal("FIRSTNAME")),
            LastName = reader.IsDBNull(reader.GetOrdinal("LASTNAME"))
                ? null : reader.GetString(reader.GetOrdinal("LASTNAME")),
            IPIn = reader.IsDBNull(reader.GetOrdinal("IP_IN"))
                ? null : reader.GetString(reader.GetOrdinal("IP_IN")),
            IPOut = reader.IsDBNull(reader.GetOrdinal("IP_OUT"))
                ? null : reader.GetString(reader.GetOrdinal("IP_OUT"))
        };
    }

    private static TimesheetEntry MapTimesheetEntryFromReader(IDataReader reader)
    {
        return new TimesheetEntry
        {
            Id = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("ID"))),
            NetId = reader.GetString(reader.GetOrdinal("NETID")),
            CheckinTimestamp = reader.GetDateTime(reader.GetOrdinal("CHECKIN_TIMESTAMP")),
            CheckinHostname = reader.IsDBNull(reader.GetOrdinal("CHECKIN_HOSTNAME"))
                ? null : reader.GetString(reader.GetOrdinal("CHECKIN_HOSTNAME")),
            CheckinIP = reader.IsDBNull(reader.GetOrdinal("CHECKIN_IP"))
                ? null : reader.GetString(reader.GetOrdinal("CHECKIN_IP")),
            CheckoutTimestamp = reader.IsDBNull(reader.GetOrdinal("CHECKOUT_TIMESTAMP"))
                ? null : reader.GetDateTime(reader.GetOrdinal("CHECKOUT_TIMESTAMP")),
            CheckoutHostname = reader.IsDBNull(reader.GetOrdinal("CHECKOUT_HOSTNAME"))
                ? null : reader.GetString(reader.GetOrdinal("CHECKOUT_HOSTNAME")),
            CheckoutIP = reader.IsDBNull(reader.GetOrdinal("CHECKOUT_IP"))
                ? null : reader.GetString(reader.GetOrdinal("CHECKOUT_IP")),
            Application = reader.IsDBNull(reader.GetOrdinal("APPLICATION"))
                ? null : reader.GetString(reader.GetOrdinal("APPLICATION")),
            DepartmentId = reader.IsDBNull(reader.GetOrdinal("DEPARTMENT_ID"))
                ? null : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("DEPARTMENT_ID"))),
            ShiftCategoryId = reader.IsDBNull(reader.GetOrdinal("SHIFT_CATEGORY_ID"))
                ? null : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("SHIFT_CATEGORY_ID"))),
            ShiftCategoryName = reader.IsDBNull(reader.GetOrdinal("SHIFT_CATEGORY_NAME"))
                ? null : reader.GetString(reader.GetOrdinal("SHIFT_CATEGORY_NAME"))
        };
    }

    private static Building MapBuildingFromReader(IDataReader reader)
    {
        return new Building
        {
            BuildingId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("BUILDING_ID"))),
            Name = reader.GetString(reader.GetOrdinal("NAME")),
            Application = reader.IsDBNull(reader.GetOrdinal("APPLICATION"))
                ? null : reader.GetString(reader.GetOrdinal("APPLICATION")),
            Inactive = reader.IsDBNull(reader.GetOrdinal("INACTIVE"))
                ? false : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("INACTIVE"))) == 1
        };
    }

    private static Department MapDepartmentFromReader(IDataReader reader)
    {
        return new Department
        {
            DeptId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("DEPT_ID"))),
            Name = reader.GetString(reader.GetOrdinal("NAME")),
            Application = reader.IsDBNull(reader.GetOrdinal("APPLICATION"))
                ? null : reader.GetString(reader.GetOrdinal("APPLICATION")),
            Inactive = reader.IsDBNull(reader.GetOrdinal("INACTIVE"))
                ? false : Convert.ToInt32(reader.GetValue(reader.GetOrdinal("INACTIVE"))) == 1
        };
    }

    private static Company MapCompanyFromReader(IDataReader reader)
    {
        return new Company
        {
            CompanyId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("COMPANY_ID"))),
            Name = reader.GetString(reader.GetOrdinal("NAME")),
            Application = reader.IsDBNull(reader.GetOrdinal("APPLICATION"))
                ? null : reader.GetString(reader.GetOrdinal("APPLICATION"))
        };
    }

    private static IdAssociation MapIdAssociationFromReader(IDataReader reader)
    {
        return new IdAssociation
        {
            SBUID = reader.GetString(reader.GetOrdinal("SBUID")),
            Application = reader.IsDBNull(reader.GetOrdinal("APPLICATION"))
                ? null : reader.GetString(reader.GetOrdinal("APPLICATION")),
            FirstName = reader.IsDBNull(reader.GetOrdinal("FIRSTNAME"))
                ? null : reader.GetString(reader.GetOrdinal("FIRSTNAME")),
            LastName = reader.IsDBNull(reader.GetOrdinal("LASTNAME"))
                ? null : reader.GetString(reader.GetOrdinal("LASTNAME"))
        };
    }

    private static Allotment MapAllotmentFromReader(IDataReader reader)
    {
        return new Allotment
        {
            Id          = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("ID"))),
            DeptId      = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("DEPT_ID"))),
            CategoryId  = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("CATEGORY_ID"))),
            Year        = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("YEAR"))),
            Hours       = reader.IsDBNull(reader.GetOrdinal("HOURS"))
                            ? null : (decimal?)Convert.ToDecimal(reader.GetValue(reader.GetOrdinal("HOURS"))),
            Application = reader.GetString(reader.GetOrdinal("APPLICATION")),
        };
    }

    #endregion

    #region Allotments

    public async Task<IEnumerable<Allotment>> GetAllotmentsAsync(string application, int year)
    {
        var results = new List<Allotment>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CH.CRCH_GET_ALLOTMENTS";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;
            command.Parameters.Add("p_year",        OracleDbType.Int32).Value    = year;
            command.Parameters.Add("r_cursor",      OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
                results.Add(MapAllotmentFromReader(reader));

            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting allotments for {Application} {Year}", application, year);
            return Enumerable.Empty<Allotment>();
        }
    }

    public async Task<OperationResult> UpsertAllotmentAsync(
        string application, int year, int deptId, int categoryId, decimal? hours, string modifiedBy)
    {
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CH.CRCH_UPSERT_ALLOTMENT";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;
            command.Parameters.Add("p_year",        OracleDbType.Int32).Value    = year;
            command.Parameters.Add("p_dept_id",     OracleDbType.Int32).Value    = deptId;
            command.Parameters.Add("p_category_id", OracleDbType.Int32).Value    = categoryId;
            command.Parameters.Add("p_hours",       OracleDbType.Decimal).Value  =
                hours.HasValue ? (object)hours.Value : DBNull.Value;
            command.Parameters.Add("p_user",        OracleDbType.Varchar2).Value = modifiedBy;

            var errorParam = command.Parameters.Add("r_error", OracleDbType.Varchar2, 500);
            errorParam.Direction = ParameterDirection.Output;

            await command.ExecuteNonQueryAsync();

            var error = errorParam.Value is OracleString os && !os.IsNull ? os.Value : null;
            return error is null
                ? OperationResult.Succeeded()
                : OperationResult.Failed(error);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error upserting allotment {DeptId}/{CategoryId}/{Year}", deptId, categoryId, year);
            return OperationResult.Failed("Database error saving allotment.");
        }
    }

    public async Task<IEnumerable<(int DeptId, int CategoryId, double HoursUsed)>> GetHoursUsedAsync(
        string application, int year)
    {
        var results = new List<(int, int, double)>();
        try
        {
            using var connection = new OracleConnection(GetConnectionString());
            await connection.OpenAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "WS_CR_CH.CRCH_GET_HOURS_USED";
            command.CommandType = CommandType.StoredProcedure;

            command.Parameters.Add("p_application", OracleDbType.Varchar2).Value = application;
            command.Parameters.Add("p_year",        OracleDbType.Int32).Value    = year;
            command.Parameters.Add("r_cursor",      OracleDbType.RefCursor).Direction = ParameterDirection.Output;

            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var deptId     = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("DEPARTMENT_ID")));
                var categoryId = Convert.ToInt32(reader.GetValue(reader.GetOrdinal("SHIFT_CATEGORY_ID")));
                var rawHours   = (Oracle.ManagedDataAccess.Types.OracleDecimal)reader.GetProviderSpecificValue(reader.GetOrdinal("HOURS_USED"));
                var hoursUsed  = rawHours.ToDouble();
                results.Add((deptId, categoryId, hoursUsed));
            }
            return results;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting hours used for {Application} {Year}", application, year);
            return Enumerable.Empty<(int, int, double)>();
        }
    }

    #endregion
}
