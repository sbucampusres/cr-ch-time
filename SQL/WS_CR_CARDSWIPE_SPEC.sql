-- ============================================================================
-- WS_CR_CARDSWIPE Package Specification
-- Campus Residences Card Swipe System
--
-- This is the NEW package that replaces WS_FC_CARDSWIPE for the modernized
-- ASP.NET Core 8 application.  WS_FC_CARDSWIPE is left untouched so the
-- legacy ASP.NET MVC 5 site continues to work.
--
-- Changes from WS_FC_CARDSWIPE:
--   CRFCCS_GET_SWIPES         - Removed p_companyid parameter
--   CRFCCS_GET_TIMECARD       - Renamed p_dept_id → p_department, moved before p_application
--   CRFCCS_INSERT_COMPANY     - Renamed p_company → p_name
--   CRFCCS_ASSOCIATE_NAME     - Changed p_sbuid type VARCHAR2 → INTEGER
--   CRFCCS_GET_ALL_ROLES      - New procedure (was missing from old package)
--   CRFCCS_GET_BUILDINGS      - Body now returns APPLICATION and INACTIVE columns
--   CRFCCS_GET_ALL_DEPARTMENTS- Body now returns APPLICATION and INACTIVE columns
--   CRFCCS_GET_COMPANIES      - Body now returns APPLICATION column
--   CRFCCS_GET_SWIPES         - Body returns raw columns (ID, NETID_IN/OUT, etc.)
--   CRFCCS_GET_TIMECARD       - Body returns raw columns (ID, CHECKIN/CHECKOUT details)
-- ============================================================================

CREATE OR REPLACE PACKAGE WS_CR_CARDSWIPE IS

  -- Package-level working variables (legacy Oracle pattern)
  foundCheckout  TIMESTAMP;
  foundCheckin   TIMESTAMP;
  foundNetId     INTEGER;
  foundSbuId     INTEGER;
  foundId        INTEGER;
  foundDepartment VARCHAR2(10);
  hoursDiff      INTEGER;

  -- -------------------------------------------------------------------------
  -- Staff Management
  -- -------------------------------------------------------------------------

  PROCEDURE CRFCCS_GET_STAFF
  (
    p_application IN VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  );

  PROCEDURE CRFCCS_ADD_UPDATE_STAFF
  (
    p_netid           IN VARCHAR2
  , p_hostname        IN VARCHAR2
  , p_terminationdate IN DATE
  , p_application     IN VARCHAR2
  , p_role            IN VARCHAR2
  , p_department      IN VARCHAR2
  );

  PROCEDURE CRFCCS_STAFF_CHECKIN
  (
    p_netid       IN  VARCHAR2
  , p_hostname    IN  VARCHAR2
  , p_application IN  VARCHAR2
  , p_ip          IN  VARCHAR2
  , r_error       OUT VARCHAR2
  );

  PROCEDURE CRFCCS_STAFF_CHECKOUT
  (
    p_netid       IN  VARCHAR2
  , p_hostname    IN  VARCHAR2
  , p_application IN  VARCHAR2
  , p_ip          IN  VARCHAR2
  , r_error       OUT VARCHAR2
  );

  -- -------------------------------------------------------------------------
  -- Visit Tracking
  -- -------------------------------------------------------------------------

  PROCEDURE CRFCCS_VISIT
  (
    p_sbuid       IN NUMBER
  , p_location    IN VARCHAR2
  , p_hostname    IN VARCHAR2
  , p_firstname   IN VARCHAR2
  , p_lastname    IN VARCHAR2
  , p_ip          IN VARCHAR2
  , p_application IN VARCHAR2
  , p_note        IN VARCHAR2
  , p_netid       IN VARCHAR2
  );

  PROCEDURE CRFCCS_GET_VISITS
  (
    p_begindate   IN     DATE
  , p_enddate     IN     DATE
  , p_location    IN     VARCHAR2
  , p_application IN     VARCHAR2
  , p_netid       IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  );

  -- Dashboard KPI: visits grouped by hostname for a given month.
  PROCEDURE CRFCCS_GET_VISITS_BY_HOST
  (
    p_year        IN     NUMBER
  , p_month       IN     NUMBER
  , p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  );

  -- Dashboard KPI: daily visit counts (TRUNC(SWIPETIME)) over a date range.
  PROCEDURE CRFCCS_GET_DAILY_VISITS
  (
    p_startdate   IN     DATE
  , p_enddate     IN     DATE
  , p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  );

  -- Fast recent-visits query for the dashboard widget.
  -- Reads only WS_FCVISITS (no booking/personalinfo joins) and caps rows.
  -- Uses WS_FCVISITS_APP_TIME_IDX (APPLICATION, SWIPETIME DESC) for top-N stop.
  PROCEDURE CRFCCS_GET_RECENT_VISITS
  (
    p_application IN     VARCHAR2
  , p_date        IN     DATE
  , p_max_rows    IN     NUMBER DEFAULT 10
  , cv_results    IN OUT SYS_REFCURSOR
  );

  -- -------------------------------------------------------------------------
  -- Contractor Swipes
  -- -------------------------------------------------------------------------

  PROCEDURE CRFCCS_SWIPE_IN
  (
    p_netid       IN  VARCHAR2
  , p_sbuid       IN  INTEGER
  , p_hostname    IN  VARCHAR2
  , p_ip          IN  VARCHAR2
  , p_application IN  VARCHAR2
  , p_firstname   IN  VARCHAR2
  , p_lastname    IN  VARCHAR2
  , r_error       OUT VARCHAR2
  );

  PROCEDURE CRFCCS_SWIPE_OUT
  (
    p_netid       IN  VARCHAR2
  , p_sbuid       IN  INTEGER
  , p_hostname    IN  VARCHAR2
  , p_ip          IN  VARCHAR2
  , p_application IN  VARCHAR2
  , p_firstname   IN  VARCHAR2
  , p_lastname    IN  VARCHAR2
  , r_error       OUT VARCHAR2
  );

  -- NOTE: p_companyid removed vs old WS_FC_CARDSWIPE version
  PROCEDURE CRFCCS_GET_SWIPES
  (
    p_begindate   IN     DATE
  , p_enddate     IN     DATE
  , p_sbuid       IN     INTEGER
  , p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  );

  -- -------------------------------------------------------------------------
  -- Lookups
  -- -------------------------------------------------------------------------

  PROCEDURE CRFCCS_GET_BUILDINGS
  (
    p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  );

  PROCEDURE CRFCCS_GET_ALL_DEPARTMENTS
  (
    p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  );

  PROCEDURE CRFCCS_GET_COMPANIES
  (
    p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  );

  PROCEDURE CRFCCS_GET_DEPARTMENT
  (
    p_application IN     VARCHAR2
  , p_netid       IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  );

  -- -------------------------------------------------------------------------
  -- Name / Company Associations
  -- -------------------------------------------------------------------------

  -- NOTE: p_sbuid changed to INTEGER vs VARCHAR2 in old package
  PROCEDURE CRFCCS_ASSOCIATE_NAME
  (
    p_sbuid       IN INTEGER
  , p_firstname   IN VARCHAR2
  , p_lastname    IN VARCHAR2
  , p_application IN VARCHAR2
  );

  PROCEDURE CRFCCS_GET_ASSOC_NAME
  (
    p_sbuid       IN  INTEGER
  , p_application IN  VARCHAR2
  , r_firstname   OUT VARCHAR2
  , r_lastname    OUT VARCHAR2
  );

  PROCEDURE CRFCCS_ASSOCIATE_COMPANY
  (
    p_sbuid       IN INTEGER
  , p_company_id  IN VARCHAR2
  , p_application IN VARCHAR2
  );

  PROCEDURE CRFCCS_GET_ASSOC_COMPANY
  (
    p_sbuid        IN  INTEGER
  , p_application  IN  VARCHAR2
  , r_company_id   OUT VARCHAR2
  , r_company_name OUT VARCHAR2
  );

  -- NOTE: p_company renamed to p_name vs old package
  PROCEDURE CRFCCS_INSERT_COMPANY
  (
    p_name        IN VARCHAR2
  , p_application IN VARCHAR2
  );

  -- -------------------------------------------------------------------------
  -- Reports
  -- -------------------------------------------------------------------------

  -- NOTE: p_dept_id renamed to p_department and moved before p_application vs old package
  PROCEDURE CRFCCS_GET_TIMECARD
  (
    p_begindate   IN     DATE
  , p_enddate     IN     DATE
  , p_netid       IN     VARCHAR2
  , p_department  IN     VARCHAR2
  , p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  );

  -- -------------------------------------------------------------------------
  -- Roles
  -- -------------------------------------------------------------------------

  PROCEDURE CRFCCS_CREATE_ROLE
  (
    p_application IN VARCHAR2
  , p_role        IN VARCHAR2
  );

  -- Returns all roles for an application (no role filter required)
  -- NEW: was missing from WS_FC_CARDSWIPE
  PROCEDURE CRFCCS_GET_ALL_ROLES
  (
    p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  );

  -- Returns roles, optionally filtered by a specific role value
  PROCEDURE CRFCCS_GET_ROLES
  (
    p_application IN     VARCHAR2
  , p_role        IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  );

  PROCEDURE CRFCCS_GET_USERS_IN_ROLE
  (
    p_application IN     VARCHAR2
  , p_role        IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  );

  PROCEDURE CRFCCS_GET_USER_ROLES
  (
    p_netid       IN     VARCHAR2
  , p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  );

  PROCEDURE CRFCCS_IS_USER_IN_ROLE
  (
    p_netid       IN  VARCHAR2
  , p_application IN  VARCHAR2
  , p_role        IN  VARCHAR2
  , r_hasrole     OUT INTEGER
  );

  -- -------------------------------------------------------------------------
  -- User / Auth Lookups
  -- -------------------------------------------------------------------------

  -- Looks up a user's display name and email from the HR views by NetID.
  -- Replaces the direct SqlQueryRaw call in UserLookupService.
  PROCEDURE CRFCCS_GET_USER_INFO
  (
    p_netid IN  VARCHAR2
  , r_name  OUT VARCHAR2
  , r_email OUT VARCHAR2
  );

  -- Authentication lookup: returns role + termination date for a staff member.
  -- r_found = 1 if the record exists, 0 if not found.
  -- Replaces the direct EF Core LINQ query in ShibbolethAuthorizationMiddleware.
  PROCEDURE CRFCCS_AUTH_LOOKUP
  (
    p_netid             IN  VARCHAR2
  , p_application       IN  VARCHAR2
  , r_role              OUT VARCHAR2
  , r_terminationdate   OUT DATE
  , r_found             OUT INTEGER
  );

  -- -------------------------------------------------------------------------
  -- External Student Info (read-only external data sources)
  -- -------------------------------------------------------------------------

  PROCEDURE CRFCCS_GET_ROOM
  (
    p_sbuid IN  INTEGER
  , r_room  OUT VARCHAR2
  );

  PROCEDURE CRFCCS_GET_AGE
  (
    p_sbuid IN  INTEGER
  , r_age   OUT INTEGER
  );

  PROCEDURE CRFCCS_GET_NAME
  (
    p_sbuid IN  INTEGER
  , r_fname OUT VARCHAR2
  , r_lname OUT VARCHAR2
  );

END WS_CR_CARDSWIPE;

