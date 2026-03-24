# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CRCardSwipe is an ASP.NET Core 8.0 web application for Campus Residences access control and timesheet management at Stony Brook University. It processes card swipe data, manages staff check-ins/check-outs, tracks visits, and provides administrative functions for facilities and housing operations.

**Built from cr-app-template**: This project was cloned from the Campus Residences ASP.NET Core application template and customized with CardSwipe-specific business logic. It inherits the template's:
- Shibboleth SSO authentication
- Oracle database with Entity Framework Core
- Role-based authorization (Administrator, Operator, Viewer)
- Audit logging system
- Configuration-driven naming (TemplateSettings)
- User management patterns

**Legacy Context:** This is a modernization of a legacy ASP.NET MVC 5 application originally built in 2015. The original used CAS authentication and the `WS_FC_CARDSWIPE` Oracle package. We are rebuilding with modern .NET 8 patterns using the new `WS_CR_CS` stored procedure package while preserving all business logic.

## Common Commands

### Build and Run
- `dotnet build` - Build the application
- `dotnet run --project CRCardSwipe` - Run the application locally
- `dotnet build CRCardSwipe/CRCardSwipe.sln` - Build using solution file
- `dotnet watch run --project CRCardSwipe` - Run with hot reload for development

### Publish to IIS
The `.csproj` has an `AfterTargets="Publish"` target that automatically copies output to the RDP share after every publish. Always use:
```bash
dotnet publish CRCardSwipe/CRCardSwipe.csproj -c Release -f net8.0 -r win-x64 --no-self-contained
```
**Do not specify `--output`** — the post-publish target copies to `/Users/wa/Documents/RDP Share/publish/cr-cardswipe` automatically. Specifying `--output` directly to the RDP share path causes an incomplete copy due to the space in the path.

### Database Operations
- Database operations handled through Oracle stored procedures via Entity Framework Core
- Connection string configured in `appsettings.json` (not in repo - use appsettings.TEMPLATE.json as guide)
- Stored procedures in the `WS_CR_CS` package (Campus Residences Card Swipe)
- DbContext: `ApplicationDbContext` in `Data/ApplicationDbContext.cs`
- Any new SQL scripts should be saved to `./sql/` folder for manual execution

### Testing
- `dotnet test` - Run unit tests
- Integration tests should mock Oracle stored procedure calls

## Architecture

### Database Schema

**Primary Database:** Oracle (CRPROD instance) via `WS_FC_CARDSWIPE` stored procedure package

**Table Naming Convention:**
- Uses `TemplateSettings.TablePrefix` from appsettings.json (configured as `WS_CR_CS_`)
- All table/column names mapped to uppercase in `ApplicationDbContext.OnModelCreating()`
- Example: `AppUser` entity → `WS_CR_CS_USERS` table

**Core Entities (from template):**
- `AppUser` → `{TablePrefix}USERS` table - User authentication with NetID and role-based access
- `AuditLog` → `{TablePrefix}AUDITLOG` table - Change tracking and audit trail

**CardSwipe Domain Entities:**
- `Visit` → `WS_FCVISITS` table - Card swipe visit logging
- `Staff` → `WS_FCSTAFF` table - Staff member records
- `TimesheetEntry` → `WS_FCSTAFFWORKLOG` table - Staff check-in/out records
- `SwipeEntry` → `WS_FCSWIPES` table - Contractor swipe records
- `Building` → `WS_FC_BUILDING` table - Building/facility data
- `Department` → `WS_FC_DEPARTMENTS` table - Departmental organization
- `Company` → `WS_FC_COMPANY` table - Contractor company records

**WS_FC_CARDSWIPE Package Procedures:**

| Procedure | Purpose | Notes |
|-----------|---------|-------|
| `CRFCCS_ADD_UPDATE_STAFF` | Add/update staff member | |
| `CRFCCS_GET_STAFF` | Get all staff for application | |
| `CRFCCS_STAFF_CHECKIN` | Staff check-in | Returns error via `r_error` OUT param |
| `CRFCCS_STAFF_CHECKOUT` | Staff check-out | Returns error via `r_error` OUT param |
| `CRFCCS_VISIT` | Log facility visit | |
| `CRFCCS_GET_VISITS` | Query visit history | |
| `CRFCCS_SWIPE_IN` | Contractor swipe in | Returns error via `r_error` OUT param |
| `CRFCCS_SWIPE_OUT` | Contractor swipe out | Returns error via `r_error` OUT param |
| `CRFCCS_GET_SWIPES` | Query contractor swipes | |
| `CRFCCS_ASSOCIATE_NAME` | Link name to SBUID | |
| `CRFCCS_GET_ASSOC_NAME` | Get name for SBUID | Returns via `r_firstname`, `r_lastname` OUT params |
| `CRFCCS_ASSOCIATE_COMPANY` | Link company to SBUID | |
| `CRFCCS_INSERT_COMPANY` | Add new company | |
| `CRFCCS_GET_COMPANIES` | Get all companies | |
| `CRFCCS_GET_BUILDINGS` | Get all buildings | |
| `CRFCCS_GET_ALL_DEPARTMENTS` | Get all departments | |
| `CRFCCS_GET_DEPARTMENT` | Get department for NetID | Returns just DEPT_ID |
| `CRFCCS_CREATE_ROLE` | Create new role | |
| `CRFCCS_GET_ALL_ROLES` | Get all roles | **NEW** - fixes documented bug |
| `CRFCCS_GET_ROLES` | Get specific role | Legacy procedure |
| `CRFCCS_GET_USERS_IN_ROLE` | Get users in role | |
| `CRFCCS_GET_USER_ROLES` | Get roles for user | |
| `CRFCCS_IS_USER_IN_ROLE` | Check if user has role | |
| `CRFCCS_GET_TIMECARD` | Generate timesheet | |
| `CRFCCS_GET_ROOM` | Get student room | Returns via `r_room` OUT param (external) |
| `CRFCCS_GET_AGE` | Get student age | Returns via `r_age` OUT param (external) |
| `CRFCCS_GET_NAME` | Get student name | Returns via `r_fname`, `r_lname` OUT params (external) |

**Note:** Many procedures use OUT parameters for return values instead of cursors.

**Critical Bug Fixed:**
- Added `CRFCCS_GET_ALL_ROLES` procedure to original package
- Original only had `CRFCCS_GET_ROLES` which required a role parameter
- C# code was calling non-existent `GET_ALL_ROLES`

**Secondary Database:** MySQL (KACE) - Optional for IP-to-hostname lookups via `ORG3.MACHINE` table

### Application Structure

**Razor Pages**: Located in `Pages/` folder

```
/Pages/
├── /Index.cshtml                 # Dashboard home page (from template)
├── /Account/                     # User management (from template)
│   ├── Index.cshtml              # User list (Administrator only)
│   ├── Create.cshtml             # Add user
│   ├── Edit.cshtml               # Edit user
│   └── Delete.cshtml             # Delete user
├── /CardSwipe/
│   ├── Index.cshtml              # Main card swipe interface
│   ├── Manual.cshtml             # Manual entry form
│   └── ViewVisits.cshtml         # Visit history
├── /Staff/
│   ├── CheckIn.cshtml            # Staff check-in
│   ├── CheckOut.cshtml           # Staff check-out
│   └── ViewTimesheet.cshtml      # Personal timesheet
├── /Contractor/
│   ├── SwipeIn.cshtml            # Contractor check-in
│   ├── SwipeOut.cshtml           # Contractor check-out
│   └── ViewSwipes.cshtml         # Contractor history
├── /Admin/
│   ├── ManageStaff.cshtml        # Staff CRUD
│   ├── ViewTimesheets.cshtml     # All timesheets
│   ├── ViewVisits.cshtml         # All visits
│   └── ManageCompanies.cshtml    # Company management
└── /Association/
    ├── AssociateName.cshtml      # Link name to SBUID
    ├── AssociateCompany.cshtml   # Link company to SBUID
    └── SelectBuilding.cshtml     # Building selection (RSP)
```

**Models**: Located in `Models/` folder

```
/Models/
├── AppUser.cs                    # User entity (from template)
├── AuditLog.cs                   # Audit trail entity (from template)
├── TemplateSettings.cs           # Configuration settings (from template)
├── Entities/
│   ├── Visit.cs                  # Visit entity
│   ├── TimesheetEntry.cs         # Timesheet entity
│   ├── SwipeEntry.cs             # Contractor swipe entity
│   ├── Staff.cs                  # Staff member entity
│   ├── Building.cs               # Building entity
│   ├── Department.cs             # Department entity
│   └── Company.cs                # Company entity
├── ViewModels/
│   ├── CardSwipeViewModel.cs     # Card swipe page data
│   ├── TimesheetViewModel.cs     # Timesheet display data
│   └── VisitReportViewModel.cs   # Visit report data
└── Services/
    ├── ICardSwipeService.cs      # Card parsing/validation
    ├── IVisitService.cs          # Visit logging service
    ├── ITimesheetService.cs      # Timesheet generation
    └── IStoredProcService.cs     # Oracle stored proc wrapper
```

**Data Layer**: `Data/`

```
/Data/
├── ApplicationDbContext.cs       # EF Core context with explicit Oracle mappings
├── StoredProcedures/
│   ├── StaffProcedures.cs        # Staff management calls
│   ├── VisitProcedures.cs        # Visit tracking calls
│   ├── SwipeProcedures.cs        # Contractor swipe calls
│   └── AdminProcedures.cs        # Admin function calls
└── Migrations/                   # EF Core migrations (if any)
```

### Key Features

**From Template:**
- **Shibboleth SSO Authentication** via `ShibbolethAuthorizationMiddleware` reading `REMOTE_USER` server variable
- **Role-Based Authorization**: Administrator, Operator, Viewer roles
- **User Management**: AppUser CRUD with expiration dates, soft delete, NetID lookup
- **Audit Logging**: AuditLog entity tracking all changes with user/timestamp
- **Configuration-Driven**: TemplateSettings for app naming and table prefixes
- **Serilog Logging**: Multi-sink logging (console, file, Oracle database)

**CardSwipe-Specific:**

**1. Card Swipe Processing**
- Parse magnetic stripe card data (Track 1 and Track 2 formats)
- Extract SBUID (Stony Brook University ID), name, and validation data
- Verify student residency status via `js_wl_roomdata.js_sp_wl_roomdata` procedure (external)
- Log visits with timestamp, building, and optional notes
- Support barcode scanning (9-digit SBUID entry)

**2. Staff Check-In/Check-Out**
- Record shift start/end times with building location
- Generate timesheets filtered by date range, NetID, and department
- Support for terminated staff (ignore check-ins after termination date)
- Automatic midnight shift boundary handling
- Validation for 12+ hour shifts (except Conference Housing)

**3. Contractor Management**
- Track contractor swipes separately from staff
- Associate contractors with companies via SBUID
- Generate shift reports filtered by date range and company
- Name association for contractors without cards

**4. Visit Tracking**
- Query visits by date range, individual SBUID, or building
- Add manual visit notes for non-card-swipe entries
- View detailed visit history with timestamps and locations
- RSP Front Desk mode with building selection

**5. Administrative Functions**
- Manage staff: add/edit NetID, department, role, termination date
- Manage buildings and departments
- Associate names and companies with SBUIDs
- Export CSV reports for visits, timesheets, contractor swipes
- Integration with template's AppUser management system

**6. Multi-Application Support**

Seven distinct "applications" (role contexts) mapped to template roles:
- **FC** - Fitness Center (card swipes, staff check-in/out) → Operator role
- **CH** - Conference Housing (staff check-in/out with departments) → Operator role
- **RE** - Summer Renovations (contractor swipes) → Operator role
- **HA** - Residence Hall Association (visit tracking) → Viewer role
- **RT** - Residential Tutoring Center (staff check-in/out) → Operator role
- **FD** - RSP Front Desk (card swipes with building + notes) → Operator role
- **ET** - Event Tracking (visit logging) → Viewer role
- **Admin** - All administrative functions → Administrator role

### Configuration

**Technology Stack (from template):**
- ASP.NET Core 8.0 (Razor Pages architecture)
- Oracle Entity Framework Core provider (`Oracle.EntityFrameworkCore` v9.23.90)
- Shibboleth SSO authentication (no ASP.NET Identity)
- Serilog for structured logging (console, file, Oracle)
- Cookie-based authentication

**Key Settings (appsettings.json):**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=(DESCRIPTION=(ADDRESS_LIST=(ADDRESS=(PROTOCOL=TCP)(HOST=129.49.2.54)(PORT=1539)))(CONNECT_DATA=(SID=CRPROD)));User Id=CRADMIN;Password=***;",
    "KaceConnection": "Server=***;Database=ORG3;User=***;Password=***;" // Optional MySQL
  },
  "TemplateSettings": {
    "ApplicationName": "CRCardSwipe",
    "ApplicationFullName": "Campus Residences Card Swipe System",
    "TablePrefix": "WS_CR_CS_",
    "OracleSchemaName": "CRADMIN"
  },
  "CardSwipe": {
    "SessionTimeout": 30,
    "DuplicateEntryWindowMinutes": 1,
    "MaxShiftHours": 12,
    "MaxShiftHoursConferenceHousing": 24,
    "CardDataMinLength": 100,
    "SBUIDLength": 9
  },
  "Serilog": {
    "MinimumLevel": "Information",
    "WriteTo": [
      { "Name": "Console" },
      { "Name": "File", "Args": { "path": "logs/log-.txt", "rollingInterval": "Day" } },
      { "Name": "Oracle", "Args": { "tableName": "WS_CR_CS_LOGS", "connectionString": "..." } }
    ]
  }
}
```

**IMPORTANT**: `appsettings.json` is gitignored and contains credentials. Use `appsettings.TEMPLATE.json` as a guide.

### Development Notes

**From Template Patterns:**
- All Oracle table/column names are explicitly mapped to uppercase in `ApplicationDbContext`
- Table names use `TemplateSettings.TablePrefix` for dynamic naming
- AppUser entity includes computed properties for access validation (`IsExpired`, `IsAccessValid`)
- Uses nullable reference types enabled
- Follow existing patterns when adding new entities:
  - `[Column("UPPERCASE_NAME")]` attributes on all properties
  - Configure table name in DbContext as `$"{_settings.TablePrefix}TABLENAME"`
  - Include audit fields: `CREATED_AT`, `CREATED_BY`, `MODIFIED_AT`, `MODIFIED_BY`
  - Use `NUMBER(1,0)` for booleans, convert with `.HasConversion<int>()`

**CardSwipe-Specific Patterns:**
- CardSwipe domain entities (Visit, Staff, TimesheetEntry, etc.) follow template mapping patterns
- Stored procedure calls use `FromSqlRaw()` with parameters
- RefCursor results mapped via `OracleDbType.RefCursor` to `IEnumerable<T>`
- All database operations wrapped in services following dependency injection pattern

**Date Handling:**
- UTC timestamps in database
- Display in Eastern Time to users
- Midnight shift crossover logic: Close shift at 23:59:59, start new at 00:00:00

**Session Management:**
- Store selected building (for FD application) in session: `HttpContext.Session.SetString("SelectedBuilding", ...)`
- Store application context in session: `HttpContext.Session.SetString("ApplicationName", ...)`

**Error Handling:**
- All database exceptions caught and logged via `ILogger<T>`
- Return user-friendly error messages (avoid exposing SQL details)
- Audit all failures using template's AuditLog entity
- Log to `WS_CR_CS_AUDITLOG` table

**Hostname Lookup:**
- Primary: Query MySQL KACE database for IP-to-hostname mapping
- Fallback: Use `Dns.GetHostEntry()` if KACE unavailable
- Used for audit trail (which computer performed the action)

**User Lookup Integration:**
- Uses template's `UserLookupService` for NetID/email directory integration
- Integrates with Shibboleth authentication for automatic user creation on first login

### Authentication & Authorization

**Authentication (from template):**
- Shibboleth SSO via `ShibbolethAuthorizationMiddleware`
- Reads `REMOTE_USER` server variable set by IIS
- Automatic user creation in `AppUser` table on first login
- Cookie-based session management
- NetID as primary identifier

**Authorization (from template):**
- Three core roles: Administrator, Operator, Viewer
- Role assignment via AppUser.Role property
- Page-level authorization using `[Authorize(Roles = "Administrator")]`
- User expiration date enforcement (IsExpired property)

**CardSwipe Role Mapping:**
- **Administrator**: Full access to all pages, user management, and administrative functions
- **Operator**: Access to card swipe, staff check-in/out, contractor swipes, visit logging
- **Viewer**: Read-only access to reports and visit history

**Application Context Roles:**
- Each of the 7 applications (FC, CH, RE, HA, RT, FD, ET) maps to Operator or Viewer
- Application selection stored in session
- Role validation checks application context + user role

### Legacy Migration Notes

**What Changed from MVC 5 Version:**
1. **Authentication:** CAS SSO → Shibboleth SSO (via template)
2. **User Management:** Custom tables → Template's AppUser entity
3. **Architecture:** MVC Controllers → Razor Pages
4. **UI Framework:** Bootstrap 3 + jQuery UI → Bootstrap 5
5. **Grid Component:** GridMVC → DataTables or native HTML tables
6. **Date Picker:** Bootstrap Datepicker → HTML5 date inputs
7. **Audit Logging:** Custom logging → Template's AuditLog entity

**What Stayed the Same:**
1. Oracle table structures (WS_FCVISITS, WS_FCSTAFF, etc.)
2. Business logic and validation rules
3. Card parsing algorithms (Track 1/Track 2 formats)
4. Multi-application architecture (FC/CH/RE/HA/RT/FD/ET)
5. Midnight shift crossover logic
6. 12-hour shift validation
7. WS_FC_CARDSWIPE stored procedure package (original package with one addition)
8. Procedure naming convention (CRFCCS_ prefix)

**Critical Bug Fixed:**
- Old: C# called `CRFCCS_GET_ALL_ROLES` but package only had `CRFCCS_GET_ROLES`
- Fixed: Added `CRFCCS_GET_ALL_ROLES` procedure to WS_FC_CARDSWIPE package

## Accessibility Guidelines

When generating code, default to WCAG 2.1 AA compliance using the following rules:

### General (from template)
- **Semantic HTML first** – headings in order (h1 → h2 → h3), landmark regions (`<header>`, `<nav>`, `<main>`, `<footer>`).
- **Keyboard everything** – tabbable, visible focus (`:focus` styles), Enter/Space activates controls.
- **Alt text always** – meaningful alt for informative images, `alt=""` for decorative.
- **Contrast check** – text contrast ≥ 4.5:1 small / 3:1 large; avoid conveying info by color alone.
- **Responsive & Reflow** – content usable at 320px width without horizontal scroll.
- **Forms** – `<label for="...">` pairs, inline error messages + `aria-describedby`, group with `<fieldset>`.
- **ARIA sparingly** – only to supplement semantics; never override native roles.
- **Media** – provide captions, transcripts; respect `prefers-reduced-motion`.
- **Live Regions** – announce dynamic updates with `aria-live="polite"`.
- **Validate** – run axe-core/Lighthouse; zero critical violations before merging.

### Card Swipe Interface Specific
- **Input field** for card swipe must have visible `<label>` with `for` attribute linking to input
- **Focus management** – after card swipe submission, focus should return to swipe input field for next card
- **Success/Error feedback** – use `role="status"` or `role="alert"` for swipe results, not just color
- **Building selection** – `<select>` dropdown must have associated `<label>`
- **Date inputs** – use HTML5 `<input type="date">` with clear labels and format hints

### Data Tables & Reports Specific
- **Tables** must have:
  - `<caption>` describing table purpose (can be visually hidden with `.visually-hidden`)
  - Proper `<thead>`, `<tbody>` structure
  - `<th scope="col">` for column headers
  - `<th scope="row">` for row headers if applicable
- **Sortable columns** – include `aria-sort="ascending|descending|none"` on `<th>` and visible indicator (↑/↓)
- **Pagination** – use `<nav aria-label="Pagination">` around pager controls
- **Export buttons** – ensure sufficient click target size (≥44×44 CSS pixels) and clear labels

### Check-In/Check-Out Interface Specific
- **Time display** – current time should update via JavaScript with `aria-live="polite"`
- **Location selection** – radio buttons or `<select>` must be in a `<fieldset>` with `<legend>`
- **Submit button** – use specific label like "Check In at [Location]" rather than generic "Submit"
- **Confirmation message** – use `role="status"` for successful check-in/out, `role="alert"` for errors

### Admin Forms Specific
- **Staff management form** – group related fields with `<fieldset>` + `<legend>`
- **Inline validation** – error messages must:
  - Be associated with field via `aria-describedby`
  - Use `aria-invalid="true"` on field with error
  - Not rely on color alone (include icon or text)
- **Delete/Terminate actions** – require confirmation dialog with proper focus management

### Navigation & Layout
- **Skip links** – provide "Skip to main content" link at top of page (visible on focus)
- **Main navigation** – use `<nav aria-label="Main">` with list structure
- **Active page indicator** – use `aria-current="page"` on current navigation item
- **Breadcrumbs** – use `<nav aria-label="Breadcrumb">` with `<ol>` list structure

## Code Style Guidelines

### C# Conventions (from template)
- Use C# 12 features (primary constructors, collection expressions, etc.)
- Async/await for all database operations
- Use `ILogger<T>` for structured logging via Serilog
- Dependency injection for all services
- Use records for DTOs and view models where appropriate

### Razor Pages Conventions (from template)
- Page models should be lean – delegate business logic to services
- Use `[BindProperty]` for form binding
- Implement `OnGet()` and `OnPostAsync()` methods
- Return `IActionResult` from handler methods
- Use partial views for reusable components
- Follow template's authorization patterns with `[Authorize(Roles = "...")]`

### Database Access Patterns
- **Never write direct SQL queries** – always use stored procedures from `WS_FC_CARDSWIPE` package
- Procedures use `CRFCCS_` prefix (Campus Residences Fitness Center Card Swipe)
- Many procedures return errors via OUT parameters instead of throwing exceptions
- Use `FromSqlRaw()` with parameters for cursor-returning procedures
- Use `ExecuteNonQueryAsync()` and read OUT parameters for error/value returns
- Map returned cursors to strongly-typed entities
- Always use dependency injection for DbContext lifetime management
- Follow template's explicit column mapping patterns
- Log all database operations via `ILogger<T>`

### Error Handling (from template)
- Use try-catch blocks around all database calls
- Log exceptions with context (user, action, parameters) via Serilog
- Return user-friendly error messages via TempData
- Never expose internal error details to users
- Use template's AuditLog entity for tracking errors
- Problem Details pattern for API errors (if any)

## Project Structure

```
CRCardSwipe/
├── CRCardSwipe.sln
├── SETUP.md                          # Template setup instructions
├── RENAME_CHECKLIST.md               # Template customization guide
├── appsettings.TEMPLATE.json         # Configuration template
├── CRCardSwipe/
│   ├── Program.cs                    # Application entry point
│   ├── appsettings.json              # Configuration (gitignored)
│   ├── Pages/
│   │   ├── _ViewImports.cshtml
│   │   ├── _ViewStart.cshtml
│   │   ├── Shared/
│   │   │   ├── _Layout.cshtml        # Template layout
│   │   │   └── _ValidationScriptsPartial.cshtml
│   │   ├── Index.cshtml              # Dashboard (from template)
│   │   ├── Account/                  # User management (from template)
│   │   ├── CardSwipe/                # Card swipe pages
│   │   ├── Staff/                    # Staff check-in/out pages
│   │   ├── Contractor/               # Contractor pages
│   │   ├── Admin/                    # Admin pages
│   │   └── Association/              # Association pages
│   ├── Models/
│   │   ├── AppUser.cs                # From template
│   │   ├── AuditLog.cs               # From template
│   │   ├── TemplateSettings.cs       # From template
│   │   ├── Entities/                 # CardSwipe domain entities
│   │   ├── ViewModels/               # CardSwipe view models
│   │   └── Services/                 # CardSwipe service interfaces
│   ├── Data/
│   │   ├── ApplicationDbContext.cs   # From template, extended
│   │   └── StoredProcedures/         # CardSwipe stored proc wrappers
│   ├── Middleware/
│   │   └── ShibbolethAuthorizationMiddleware.cs  # From template
│   ├── Services/
│   │   ├── UserLookupService.cs      # From template
│   │   ├── CardSwipeService.cs       # CardSwipe business logic
│   │   ├── VisitService.cs           # Visit logging
│   │   ├── TimesheetService.cs       # Timesheet generation
│   │   └── StoredProcService.cs      # Stored proc wrapper
│   ├── wwwroot/
│   │   ├── css/
│   │   │   └── site.css              # From template, extended
│   │   ├── js/
│   │   │   └── site.js               # From template, extended
│   │   └── lib/                      # Bootstrap 5, jQuery
│   └── Deploy/
│       └── deploy-iis.ps1            # From template
├── CRCardSwipe.Tests/                # Unit tests
├── sql/
│   ├── TEMPLATE/                     # Template SQL scripts
│   │   ├── 01_CREATE_USERS_TABLE.sql
│   │   └── 02_CREATE_AUDITLOG_TABLE.sql
│   ├── WS_CR_CS_PACKAGE_SPEC.sql     # CardSwipe package specification
│   ├── WS_CR_CS_PACKAGE_BODY.sql     # CardSwipe package body
│   └── MIGRATION_GUIDE.md            # Legacy → modern migration notes
└── README.md
```

## Development Workflow

### Starting from Template
1. Clone cr-app-template repository
2. Follow `SETUP.md` to configure basic settings
3. Use `RENAME_CHECKLIST.md` to rename from template to CRCardSwipe
4. Run template SQL scripts to create USERS and AUDITLOG tables
5. Run CardSwipe SQL scripts to create WS_CR_CS package and domain tables
6. Update `appsettings.json` with CardSwipe-specific configuration
7. Extend `ApplicationDbContext` with CardSwipe entities
8. Add CardSwipe-specific pages, services, and business logic

### Adding New Features
1. Follow template patterns for database entities (explicit column mapping)
2. Use template's authorization patterns for page access control
3. Integrate with AuditLog for change tracking
4. Follow accessibility guidelines for all UI components
5. Add stored procedures to WS_CR_CS package (never inline SQL)
6. Log all operations via `ILogger<T>` with Serilog

## Contact Information

This application is maintained by Campus Residences IT at Stony Brook University.

For questions or access issues:
- Email: cris@stonybrook.edu
- Phone: (631) 632-6750