# CRCardSwipe

Access control and timesheet management system for Campus Residences at Stony Brook University. Processes magnetic stripe card swipes, manages staff check-ins/check-outs, tracks contractor access, and generates timesheets and visit reports for facilities operations.

## Technology Stack

- **Framework**: ASP.NET Core 8.0 (Razor Pages)
- **Database**: Oracle 19c+ via Entity Framework Core 9
- **Authentication**: Shibboleth SSO — reads `REMOTE_USER` server variable set by IIS; no ASP.NET Identity
- **Authorization**: Cookie-based sessions with three roles: Administrator, Operator, Viewer
- **Logging**: Serilog (console, file, Oracle table)
- **UI**: Bootstrap 5, jQuery 3

## Key Design Choices

### Business Logic Lives in Oracle Packages

All database operations go through the `WS_FC_CARDSWIPE` Oracle stored procedure package (procedures prefixed `CRFCCS_`). There is no inline SQL in the application. The C# layer calls procedures and maps results — it does not own the business logic for things like shift validation, midnight crossover handling, or duplicate swipe detection.

This was a deliberate choice from the original 2015 MVC 5 application and has been preserved in this rewrite. Benefits: logic is centralized and testable in Oracle, DBA team can patch behavior without a code deployment, and the procedures are reusable by other consumers.

Many procedures return values via `OUT` parameters rather than cursors (e.g. `r_error`, `r_firstname`, `r_room`). The C# code reads these after execution rather than mapping a result set.

### Multi-Application Architecture

Seven distinct operational contexts share the same codebase, each with its own role and page access:

| Code | Name | Purpose |
|------|------|---------|
| FC | Fitness Center | Card swipes + staff check-in/out |
| CH | Conference Housing | Staff shifts with department tracking |
| RE | Summer Renovations | Contractor swipe tracking |
| HA | Residence Hall Association | Visit logging |
| RT | Residential Tutoring Center | Staff check-in/out |
| FD | RSP Front Desk | Card swipes with per-session building selection |
| ET | Event Tracking | Visit logging |

The active application context is stored in session and controls which features and navigation items are visible.

### Two Separate Table Sets

The app writes to two Oracle table namespaces:

- **`WS_CR_CS_*`** — application-managed tables (users, audit log, logs) created by this project's SQL scripts
- **`WS_FC*`** — legacy domain tables (visits, staff, timesheets, swipes, buildings, companies) owned by the original package and shared with other systems

C# entities are explicitly mapped to uppercase column names in `ApplicationDbContext.OnModelCreating()` to match Oracle conventions.

### Legacy Modernization

This is a rewrite of a 2015 ASP.NET MVC 5 application. What changed:

- CAS SSO → Shibboleth SSO
- MVC Controllers + Razor views → Razor Pages
- Bootstrap 3 → Bootstrap 5
- GridMVC grid component → native HTML tables
- Custom role tables → template `AppUser` entity with role field

What stayed the same:

- Oracle table structures (`WS_FCVISITS`, `WS_FCSTAFF`, etc.)
- `WS_FC_CARDSWIPE` stored procedure package with `CRFCCS_` prefix
- Card parsing logic (Track 1/Track 2 magnetic stripe formats)
- All business rules (shift validation, midnight crossover, duplicate swipe window)

One bug was fixed during the rewrite: the original C# called `CRFCCS_GET_ALL_ROLES` which did not exist in the package. A `CRFCCS_GET_ALL_ROLES` procedure was added to `WS_FC_CARDSWIPE`.

## Project Structure

```
CRCardSwipe/
├── CRCardSwipe/
│   ├── Pages/
│   │   ├── CardSwipe/        # Swipe input and visit history
│   │   ├── Staff/            # Check-in/out and timesheets
│   │   ├── Contractor/       # Contractor swipe tracking
│   │   ├── Admin/            # Staff, company, and user management
│   │   ├── Association/      # Link names/companies to SBUIDs
│   │   └── Account/          # Application user management
│   ├── Models/
│   │   ├── Entities/         # Domain entities (Visit, Staff, SwipeEntry, etc.)
│   │   └── ViewModels/       # Page-specific view data
│   ├── Data/
│   │   ├── ApplicationDbContext.cs
│   │   └── StoredProcedures/ # Oracle procedure call wrappers
│   ├── Services/             # Business logic and procedure orchestration
│   └── Middleware/
│       └── ShibbolethAuthorizationMiddleware.cs
└── sql/
    ├── WS_CR_CS_PACKAGE_SPEC.sql   # Application package spec
    ├── WS_CR_CS_PACKAGE_BODY.sql   # Application package body
    └── TEMPLATE/                   # Base table creation scripts
```

## Running Locally

```bash
dotnet build
dotnet run --project CRCardSwipe
```

Configuration (database credentials, connection strings) goes in `CRCardSwipe/appsettings.json`, which is gitignored. Use `appsettings.TEMPLATE.json` as a starting point.

## Deployment

```bash
publish-for-iis.bat
```

Requires IIS with .NET 8 Hosting Bundle and the Shibboleth SP module configured.

---

Campus Residences IT — Stony Brook University | cris@stonybrook.edu
