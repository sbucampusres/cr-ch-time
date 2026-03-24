-- ============================================================================
-- WS_CR_CARDSWIPE Package Body
-- Campus Residences Card Swipe System
--
-- See WS_CR_CARDSWIPE_SPEC.sql for change notes vs WS_FC_CARDSWIPE.
-- ============================================================================

CREATE OR REPLACE PACKAGE BODY WS_CR_CARDSWIPE AS

  -- -------------------------------------------------------------------------
  -- Staff Management
  -- -------------------------------------------------------------------------

  PROCEDURE CRFCCS_GET_STAFF
  (
    p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  )
  IS
  BEGIN
    OPEN cv_results FOR
      SELECT NETID, TERMINATIONDATE
      FROM WS_FCSTAFF
      WHERE APPLICATION = p_application
      ORDER BY TERMINATIONDATE;
  END CRFCCS_GET_STAFF;


  PROCEDURE CRFCCS_ADD_UPDATE_STAFF
  (
    p_netid           IN VARCHAR2
  , p_hostname        IN VARCHAR2
  , p_terminationdate IN DATE
  , p_application     IN VARCHAR2
  , p_role            IN VARCHAR2
  , p_department      IN VARCHAR2
  )
  AS
  BEGIN
    SELECT COUNT(1) INTO foundNetId
    FROM WS_FCSTAFF
    WHERE NETID = LOWER(p_netid) AND APPLICATION = p_application;

    IF foundNetId != 0 THEN
      UPDATE WS_FCSTAFF
      SET AUDIT_TIMESTAMP  = CURRENT_TIMESTAMP,
          HOSTNAME         = p_hostname,
          TERMINATIONDATE  = p_terminationdate
      WHERE NETID = LOWER(p_netid)
        AND (p_application IS NULL OR APPLICATION = p_application);
    ELSE
      INSERT INTO WS_FCSTAFF (NETID, AUDIT_TIMESTAMP, HOSTNAME, TERMINATIONDATE, APPLICATION, ROLE, DEPT_ID)
      VALUES (LOWER(p_netid), CURRENT_TIMESTAMP, p_hostname, p_terminationdate, p_application, p_role, p_department);
    END IF;
  END CRFCCS_ADD_UPDATE_STAFF;


  PROCEDURE CRFCCS_STAFF_CHECKIN
  (
    p_netid       IN  VARCHAR2
  , p_hostname    IN  VARCHAR2
  , p_application IN  VARCHAR2
  , p_ip          IN  VARCHAR2
  , r_error       OUT VARCHAR2
  )
  IS
    CURSOR c1 IS
      SELECT CHECKIN_TIMESTAMP, CHECKOUT_TIMESTAMP
      FROM WS_FCSTAFFWORKLOG
      WHERE NETID = LOWER(p_netid)
        AND APPLICATION = p_application
      ORDER BY CHECKIN_TIMESTAMP DESC;
  BEGIN
    OPEN c1;
    FETCH c1 INTO foundCheckin, foundCheckout;
    r_error := '';
    IF (foundCheckin IS NOT NULL) AND (foundCheckout IS NULL) THEN
      r_error := 'You did not check out of your previous shift.  New shift started, please contact management.';
    END IF;
    INSERT INTO WS_FCSTAFFWORKLOG (NETID, CHECKIN_TIMESTAMP, CHECKIN_HOSTNAME, APPLICATION, CHECKIN_IP, DEPARTMENT_ID)
    VALUES (
      LOWER(p_netid),
      CURRENT_TIMESTAMP,
      p_hostname,
      p_application,
      p_ip,
      (SELECT DEPT_ID FROM WS_FCSTAFF WHERE APPLICATION = p_application AND NETID = LOWER(p_netid))
    );
  END CRFCCS_STAFF_CHECKIN;


  PROCEDURE CRFCCS_STAFF_CHECKOUT
  (
    p_netid       IN  VARCHAR2
  , p_hostname    IN  VARCHAR2
  , p_application IN  VARCHAR2
  , p_ip          IN  VARCHAR2
  , r_error       OUT VARCHAR2
  )
  IS
  BEGIN
    r_error := '';

    SELECT CHECKIN_TIMESTAMP, CHECKOUT_TIMESTAMP, DEPARTMENT_ID
    INTO   foundCheckin, foundCheckout, foundDepartment
    FROM (
      SELECT CHECKIN_TIMESTAMP, CHECKOUT_TIMESTAMP, DEPARTMENT_ID
      FROM WS_FCSTAFFWORKLOG
      WHERE NETID = LOWER(p_netid)
        AND APPLICATION = p_application
      ORDER BY CHECKIN_TIMESTAMP DESC
    )
    WHERE ROWNUM = 1;

    hoursDiff := TO_NUMBER(REGEXP_SUBSTR(TO_CHAR((CURRENT_TIMESTAMP - foundCheckin) * 24), '[^ ]+', 1, 1));

    IF (foundCheckin IS NULL) OR (foundCheckout IS NOT NULL) THEN
      r_error := 'No check in found.  Please check in before checking out.';
    ELSIF (hoursDiff > 12) AND (p_application != 'CH') THEN
      r_error := 'Prior checkin is more than 12 hours old.  Please create a new checkin and contact management.';
    ELSIF (TRUNC(foundCheckin) != TRUNC(CURRENT_TIMESTAMP)) THEN
      -- Close yesterday's shift at 23:59
      UPDATE WS_FCSTAFFWORKLOG
      SET CHECKOUT_TIMESTAMP = TO_DATE(CONCAT(TO_CHAR(TRUNC(foundCheckin)), ' 23:59:00'), 'DD-MON-YY HH24:MI:SS'),
          CHECKOUT_HOSTNAME  = p_hostname,
          CHECKOUT_IP        = p_ip
      WHERE NETID = LOWER(p_netid)
        AND APPLICATION = p_application
        AND CHECKIN_TIMESTAMP = foundCheckin;
      -- Begin a new shift at midnight today
      foundCheckin := TO_DATE(CONCAT(TO_CHAR(TRUNC(CURRENT_TIMESTAMP)), ' 00:00:00'), 'DD-MON-YY HH24:MI:SS');
      INSERT INTO WS_FCSTAFFWORKLOG (NETID, CHECKIN_TIMESTAMP, CHECKIN_HOSTNAME, CHECKOUT_TIMESTAMP, CHECKOUT_HOSTNAME, APPLICATION, DEPARTMENT_ID)
      VALUES (LOWER(p_netid), foundCheckin, p_hostname, CURRENT_TIMESTAMP, p_hostname, p_application, foundDepartment);
    ELSE
      UPDATE WS_FCSTAFFWORKLOG
      SET CHECKOUT_TIMESTAMP = CURRENT_TIMESTAMP,
          CHECKOUT_HOSTNAME  = p_hostname,
          CHECKOUT_IP        = p_ip
      WHERE NETID = LOWER(p_netid)
        AND APPLICATION = p_application
        AND CHECKIN_TIMESTAMP = foundCheckin;
    END IF;
  END CRFCCS_STAFF_CHECKOUT;


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
  )
  IS
  BEGIN
    SELECT COUNT(1) INTO foundId
    FROM WS_FCVISITS
    WHERE SBUID = p_sbuid
      AND APPLICATION = p_application
      AND SWIPETIME > (SYSDATE - INTERVAL '1' MINUTE);

    IF foundId = 0 THEN
      INSERT INTO WS_FCVISITS (SBUID, HOSTNAME, FIRSTNAME, LASTNAME, LOCATION, IP, SWIPETIME, APPLICATION, NOTE, NETID_AUDIT)
      VALUES (p_sbuid, p_hostname, p_firstname, p_lastname, p_location, p_ip, CURRENT_TIMESTAMP, p_application, p_note, p_netid);
    END IF;
  END CRFCCS_VISIT;


  -- Fast recent-visits query for the dashboard widget.
  -- Reads only WS_FCVISITS (no booking/personalinfo joins) and caps rows.
  -- Uses WS_FCVISITS_APP_TIME_IDX (APPLICATION, SWIPETIME DESC) for top-N stop.
  PROCEDURE CRFCCS_GET_VISITS_BY_HOST
  (
    p_year        IN     NUMBER
  , p_month       IN     NUMBER
  , p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  )
  IS
  BEGIN
    OPEN cv_results FOR
      SELECT NVL(HOSTNAME, '(unknown)') AS HOSTNAME,
             COUNT(*)                   AS VISIT_COUNT
      FROM   WS_FCVISITS
      WHERE  APPLICATION = p_application
        AND  TRUNC(SWIPETIME, 'MM') = TRUNC(TO_DATE(
               LPAD(TO_CHAR(p_month), 2, '0') || '/' || TO_CHAR(p_year),
               'MM/YYYY'), 'MM')
      GROUP BY HOSTNAME
      ORDER BY HOSTNAME;
  END CRFCCS_GET_VISITS_BY_HOST;


  PROCEDURE CRFCCS_GET_DAILY_VISITS
  (
    p_startdate   IN     DATE
  , p_enddate     IN     DATE
  , p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  )
  IS
  BEGIN
    OPEN cv_results FOR
      SELECT TRUNC(SWIPETIME) AS VISIT_DATE,
             COUNT(*)         AS VISIT_COUNT
      FROM   WS_FCVISITS
      WHERE  APPLICATION = p_application
        AND  SWIPETIME >= p_startdate
        AND  SWIPETIME <  p_enddate + 1
      GROUP BY TRUNC(SWIPETIME)
      ORDER BY 1;
  END CRFCCS_GET_DAILY_VISITS;


  PROCEDURE CRFCCS_GET_RECENT_VISITS
  (
    p_application IN     VARCHAR2
  , p_date        IN     DATE
  , p_max_rows    IN     NUMBER DEFAULT 10
  , cv_results    IN OUT SYS_REFCURSOR
  )
  IS
  BEGIN
    OPEN cv_results FOR
      SELECT SBUID, FIRSTNAME, LASTNAME, SWIPETIME, LOCATION, NOTE, NETID_AUDIT
      FROM (
        SELECT SBUID, FIRSTNAME, LASTNAME, SWIPETIME, LOCATION, NOTE, NETID_AUDIT
        FROM WS_FCVISITS
        WHERE APPLICATION = p_application
          AND SWIPETIME >= TRUNC(p_date)
          AND SWIPETIME <  TRUNC(p_date) + 1
        ORDER BY SWIPETIME DESC
      )
      WHERE ROWNUM <= p_max_rows;
  END CRFCCS_GET_RECENT_VISITS;


  PROCEDURE CRFCCS_GET_VISITS
  (
    p_begindate   IN     DATE
  , p_enddate     IN     DATE
  , p_location    IN     VARCHAR2
  , p_application IN     VARCHAR2
  , p_netid       IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  )
  IS
  BEGIN
    OPEN cv_results FOR
      SELECT a.SBUID,
             c.FIRST_NAME,
             c.LAST_NAME,
             a.SWIPETIME,
             CASE
               WHEN b.NAME IS NULL THEN
                 SUBSTR(sr.ROOMSPACE_DESCRIPTION, 1, INSTR(sr.ROOMSPACE_DESCRIPTION, '-', -3, 1) - 1)
               ELSE b.NAME
             END AS Build,
             a.NOTE,
             d.NAME         AS NETID_DISPLAY,
             sr.ROOMSPACE_DESCRIPTION AS CK_BED_SPACE,
             f.EMAIL_ADDR
      FROM WS_FCVISITS a
      LEFT OUTER JOIN JS_V_PERSONALINFO c
           ON a.SBUID = c.EMPLID
      LEFT OUTER JOIN JS_V_NETIDALL d
           ON LOWER(a.NETID_AUDIT) = LOWER(d.SU_EXT_USERID)
      LEFT OUTER JOIN WS_FC_BUILDING b
           ON a.LOCATION = b.BUILDING_ID
      LEFT OUTER JOIN (
           SELECT STUDENTID, ROOMSPACE_DESCRIPTION
           FROM CRADMIN.STARREZ_BOOKING_VIEW
           WHERE BOOKINGSTATUS = 'In Room'
           UNION ALL
           SELECT STUDENTID, ROOMSPACE_DESCRIPTION
           FROM CRADMIN.STARREZ_OTHEROCCUPANTS_VIEW
           WHERE BOOKINGSTATUS = 'In Room'
      ) sr ON a.SBUID = sr.STUDENTID
      LEFT OUTER JOIN JS_V_ALLEMAILADDR f
           ON a.SBUID = f.EMPLID
      WHERE (p_begindate   IS NULL OR a.SWIPETIME >= p_begindate)
        AND (p_enddate     IS NULL OR a.SWIPETIME <= p_enddate)
        AND (p_application IS NULL OR a.APPLICATION = p_application)
        AND (p_location    IS NULL OR a.LOCATION = p_location)
        AND (p_netid       IS NULL OR a.NETID_AUDIT = p_netid)
      ORDER BY a.SWIPETIME DESC;
  END CRFCCS_GET_VISITS;


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
  )
  IS
    CURSOR c1 IS
      SELECT SWIPE_TIME_IN, SWIPE_TIME_OUT
      FROM WS_FCSWIPES
      WHERE SBUID = p_sbuid
        AND APPLICATION = p_application
      ORDER BY APPLICATION, SBUID, SWIPE_TIME_IN DESC;
  BEGIN
    OPEN c1;
    FETCH c1 INTO foundCheckin, foundCheckout;
    r_error := '';
    IF (foundCheckin IS NOT NULL) AND (foundCheckout IS NULL) THEN
      r_error := 'Warning.  You did not check out of your previous shift started at ' || foundCheckin || '.  ';
      IF p_firstname IS NOT NULL THEN
        r_error := r_error || 'New shift started for ' || p_firstname || ' ' || p_lastname || ', please contact management.';
      ELSE
        r_error := r_error || 'New shift started, please contact management.';
      END IF;
    ELSE
      IF p_firstname IS NOT NULL THEN
        r_error := 'New shift started for ' || p_firstname || ' ' || p_lastname || '.';
      ELSE
        r_error := 'New shift started.';
      END IF;
    END IF;
    INSERT INTO WS_FCSWIPES (SBUID, SWIPE_TIME_IN, NETID_IN, HOSTNAME_IN, APPLICATION, FIRSTNAME, LASTNAME, IP_IN)
    VALUES (p_sbuid, CURRENT_TIMESTAMP, LOWER(p_netid), p_hostname, p_application, p_firstname, p_lastname, p_ip);
  END CRFCCS_SWIPE_IN;


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
  )
  IS
  BEGIN
    r_error := 'Shift ended successfully.';

    SELECT SWIPE_TIME_IN, SWIPE_TIME_OUT
    INTO   foundCheckin, foundCheckout
    FROM (
      SELECT SWIPE_TIME_IN, SWIPE_TIME_OUT
      FROM WS_FCSWIPES
      WHERE SBUID = p_sbuid
        AND APPLICATION = p_application
      ORDER BY APPLICATION, SBUID, SWIPE_TIME_IN DESC
    )
    WHERE ROWNUM = 1;

    hoursDiff := TO_NUMBER(REGEXP_SUBSTR(TO_CHAR((CURRENT_TIMESTAMP - foundCheckin) * 24), '[^ ]+', 1, 1));

    IF (foundCheckin IS NULL) OR (foundCheckout IS NOT NULL) THEN
      r_error := 'No swipe in found.  Please swipe in before checking out.';
    ELSIF (hoursDiff > 12) THEN
      r_error := 'Prior checkin is more than 12 hours old.  Please create a new checkin and contact management.';
    ELSIF (TRUNC(foundCheckin) != TRUNC(CURRENT_TIMESTAMP)) THEN
      -- Close yesterday's shift at 23:59
      UPDATE WS_FCSWIPES
      SET SWIPE_TIME_OUT = TO_DATE(CONCAT(TO_CHAR(TRUNC(foundCheckin)), ' 23:59:00'), 'DD-MON-YY HH24:MI:SS'),
          HOSTNAME_OUT   = p_hostname
      WHERE SBUID = p_sbuid
        AND APPLICATION = p_application
        AND SWIPE_TIME_IN = foundCheckin;
      -- Begin a new shift at midnight today
      foundCheckin := TO_DATE(CONCAT(TO_CHAR(TRUNC(CURRENT_TIMESTAMP)), ' 00:00:00'), 'DD-MON-YY HH24:MI:SS');
      INSERT INTO WS_FCSWIPES (SBUID, SWIPE_TIME_IN, NETID_IN, HOSTNAME_IN, APPLICATION, FIRSTNAME, LASTNAME, IP_IN)
      VALUES (p_sbuid, foundCheckin, LOWER(p_netid), p_hostname, p_application, p_firstname, p_lastname, p_ip);
    ELSE
      UPDATE WS_FCSWIPES
      SET SWIPE_TIME_OUT = CURRENT_TIMESTAMP,
          HOSTNAME_OUT   = p_hostname,
          IP_OUT         = p_ip,
          NETID_OUT      = p_netid
      WHERE SBUID = p_sbuid
        AND APPLICATION = p_application
        AND SWIPE_TIME_IN = foundCheckin;
    END IF;
  END CRFCCS_SWIPE_OUT;


  -- NOTE: p_companyid parameter removed. Returns raw columns to match C# mapper.
  PROCEDURE CRFCCS_GET_SWIPES
  (
    p_begindate   IN     DATE
  , p_enddate     IN     DATE
  , p_sbuid       IN     INTEGER
  , p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  )
  IS
  BEGIN
    OPEN cv_results FOR
      SELECT ROWNUM AS ID,
             a.SBUID,
             a.SWIPE_TIME_IN,
             a.SWIPE_TIME_OUT,
             a.NETID_IN,
             a.NETID_OUT,
             a.HOSTNAME_IN,
             a.HOSTNAME_OUT,
             a.APPLICATION,
             CASE WHEN a.FIRSTNAME IS NULL THEN d.FIRSTNAME ELSE a.FIRSTNAME END AS FIRSTNAME,
             CASE WHEN a.LASTNAME  IS NULL THEN d.LASTNAME  ELSE a.LASTNAME  END AS LASTNAME,
             a.IP_IN,
             a.IP_OUT
      FROM WS_FCSWIPES a
      LEFT OUTER JOIN WS_FCIDASSOCNAME d
           ON a.SBUID = d.SBUID AND a.APPLICATION = d.APPLICATION
      WHERE (p_begindate   IS NULL OR a.SWIPE_TIME_IN >= p_begindate)
        AND (p_enddate     IS NULL OR a.SWIPE_TIME_IN <= p_enddate)
        AND (p_sbuid       IS NULL OR a.SBUID = p_sbuid)
        AND (p_application IS NULL OR a.APPLICATION = p_application)
      ORDER BY a.SWIPE_TIME_IN DESC;
  END CRFCCS_GET_SWIPES;


  -- -------------------------------------------------------------------------
  -- Lookups
  -- -------------------------------------------------------------------------

  -- Now returns APPLICATION and INACTIVE columns to match C# mapper
  PROCEDURE CRFCCS_GET_BUILDINGS
  (
    p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  )
  IS
  BEGIN
    OPEN cv_results FOR
      SELECT BUILDING_ID, NAME, APPLICATION, INACTIVE
      FROM WS_FC_BUILDING
      WHERE APPLICATION = p_application
      ORDER BY NAME ASC;
  END CRFCCS_GET_BUILDINGS;


  -- Now returns APPLICATION and INACTIVE columns to match C# mapper
  PROCEDURE CRFCCS_GET_ALL_DEPARTMENTS
  (
    p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  )
  IS
  BEGIN
    OPEN cv_results FOR
      SELECT DEPT_ID, NAME, APPLICATION, INACTIVE
      FROM WS_FC_DEPARTMENTS
      WHERE APPLICATION = p_application
        AND ((INACTIVE <> 1) OR (INACTIVE IS NULL));
  END CRFCCS_GET_ALL_DEPARTMENTS;


  -- Now returns APPLICATION column to match C# mapper
  PROCEDURE CRFCCS_GET_COMPANIES
  (
    p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  )
  IS
  BEGIN
    OPEN cv_results FOR
      SELECT COMPANY_ID, NAME, APPLICATION
      FROM WS_FC_COMPANY
      WHERE APPLICATION = p_application;
  END CRFCCS_GET_COMPANIES;


  PROCEDURE CRFCCS_GET_DEPARTMENT
  (
    p_application IN     VARCHAR2
  , p_netid       IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  )
  IS
  BEGIN
    OPEN cv_results FOR
      SELECT DEPT_ID
      FROM WS_FCSTAFF
      WHERE NETID = LOWER(p_netid)
        AND APPLICATION = p_application;
  END CRFCCS_GET_DEPARTMENT;


  -- -------------------------------------------------------------------------
  -- Name / Company Associations
  -- -------------------------------------------------------------------------

  -- NOTE: p_sbuid changed to INTEGER (was VARCHAR2 in old package)
  PROCEDURE CRFCCS_ASSOCIATE_NAME
  (
    p_sbuid       IN INTEGER
  , p_firstname   IN VARCHAR2
  , p_lastname    IN VARCHAR2
  , p_application IN VARCHAR2
  )
  AS
  BEGIN
    SELECT COUNT(1) INTO foundSbuId
    FROM WS_FCIDASSOCNAME
    WHERE SBUID = p_sbuid AND APPLICATION = p_application;

    IF foundSbuId != 0 THEN
      UPDATE WS_FCIDASSOCNAME
      SET FIRSTNAME = p_firstname,
          LASTNAME  = p_lastname
      WHERE SBUID = p_sbuid AND APPLICATION = p_application;
    ELSE
      INSERT INTO WS_FCIDASSOCNAME (SBUID, APPLICATION, FIRSTNAME, LASTNAME)
      VALUES (p_sbuid, p_application, p_firstname, p_lastname);
    END IF;
  END CRFCCS_ASSOCIATE_NAME;


  PROCEDURE CRFCCS_GET_ASSOC_NAME
  (
    p_sbuid       IN  INTEGER
  , p_application IN  VARCHAR2
  , r_firstname   OUT VARCHAR2
  , r_lastname    OUT VARCHAR2
  )
  IS
    CURSOR c1 IS
      SELECT FIRSTNAME, LASTNAME
      FROM WS_FCIDASSOCNAME
      WHERE SBUID = p_sbuid AND APPLICATION = p_application;
  BEGIN
    OPEN c1;
    FETCH c1 INTO r_firstname, r_lastname;
  END CRFCCS_GET_ASSOC_NAME;


  PROCEDURE CRFCCS_ASSOCIATE_COMPANY
  (
    p_sbuid       IN INTEGER
  , p_company_id  IN VARCHAR2
  , p_application IN VARCHAR2
  )
  AS
  BEGIN
    DELETE FROM WS_FC_ID_ASSOC_COMPANY
    WHERE APPLICATION = p_application AND SBUID = p_sbuid;

    INSERT INTO WS_FC_ID_ASSOC_COMPANY (APPLICATION, SBUID, COMPANYID)
    VALUES (p_application, p_sbuid, p_company_id);
  END CRFCCS_ASSOCIATE_COMPANY;


  PROCEDURE CRFCCS_GET_ASSOC_COMPANY
  (
    p_sbuid        IN  INTEGER
  , p_application  IN  VARCHAR2
  , r_company_id   OUT VARCHAR2
  , r_company_name OUT VARCHAR2
  )
  IS
    CURSOR c1 IS
      SELECT co.NAME, co.COMPANY_ID
      FROM WS_FC_ID_ASSOC_COMPANY ac
      JOIN WS_FC_COMPANY co
           ON  ac.APPLICATION = co.APPLICATION
           AND ac.COMPANYID   = co.COMPANY_ID
      WHERE ac.SBUID = p_sbuid AND ac.APPLICATION = p_application;
  BEGIN
    OPEN c1;
    FETCH c1 INTO r_company_name, r_company_id;
  END CRFCCS_GET_ASSOC_COMPANY;


  -- NOTE: parameter renamed from p_company to p_name
  PROCEDURE CRFCCS_INSERT_COMPANY
  (
    p_name        IN VARCHAR2
  , p_application IN VARCHAR2
  )
  IS
  BEGIN
    SELECT MAX(COMPANY_ID) + 1 INTO foundId
    FROM WS_FC_COMPANY
    WHERE APPLICATION = p_application;

    INSERT INTO WS_FC_COMPANY (APPLICATION, NAME, COMPANY_ID)
    VALUES (p_application, p_name, foundId);
  END CRFCCS_INSERT_COMPANY;


  -- -------------------------------------------------------------------------
  -- Reports
  -- -------------------------------------------------------------------------

  -- NOTE: p_dept_id renamed to p_department, moved before p_application.
  -- Returns raw columns (ID, CHECKIN/CHECKOUT hostname/IP, DEPARTMENT_ID)
  -- to match C# TimesheetEntry mapper.
  PROCEDURE CRFCCS_GET_TIMECARD
  (
    p_begindate   IN     DATE
  , p_enddate     IN     DATE
  , p_netid       IN     VARCHAR2
  , p_department  IN     VARCHAR2
  , p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  )
  IS
  BEGIN
    OPEN cv_results FOR
      SELECT ROWNUM AS ID,
             a.NETID,
             a.CHECKIN_TIMESTAMP,
             a.CHECKIN_HOSTNAME,
             a.CHECKIN_IP,
             a.CHECKOUT_TIMESTAMP,
             a.CHECKOUT_HOSTNAME,
             a.CHECKOUT_IP,
             a.APPLICATION,
             a.DEPARTMENT_ID
      FROM WS_FCSTAFFWORKLOG a
      WHERE (p_begindate   IS NULL OR a.CHECKIN_TIMESTAMP >= p_begindate)
        AND (p_enddate     IS NULL OR a.CHECKIN_TIMESTAMP <= p_enddate)
        AND (p_netid       IS NULL OR a.NETID = LOWER(p_netid))
        AND (p_application IS NULL OR a.APPLICATION = p_application)
        AND (p_department  IS NULL OR a.DEPARTMENT_ID = p_department)
      ORDER BY a.CHECKIN_TIMESTAMP DESC;
  END CRFCCS_GET_TIMECARD;


  -- -------------------------------------------------------------------------
  -- Roles
  -- -------------------------------------------------------------------------

  PROCEDURE CRFCCS_CREATE_ROLE
  (
    p_application IN VARCHAR2
  , p_role        IN VARCHAR2
  )
  IS
  BEGIN
    INSERT INTO WS_FCROLES (APPLICATION, ROLE)
    VALUES (p_application, p_role);
  END CRFCCS_CREATE_ROLE;


  -- New procedure: returns all roles without requiring a specific role filter
  PROCEDURE CRFCCS_GET_ALL_ROLES
  (
    p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  )
  IS
  BEGIN
    OPEN cv_results FOR
      SELECT ROLE
      FROM WS_FCROLES
      WHERE APPLICATION = p_application
      ORDER BY ROLE;
  END CRFCCS_GET_ALL_ROLES;


  PROCEDURE CRFCCS_GET_ROLES
  (
    p_application IN     VARCHAR2
  , p_role        IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  )
  IS
  BEGIN
    OPEN cv_results FOR
      SELECT ROLE
      FROM WS_FCROLES
      WHERE APPLICATION = p_application
        AND ((p_role IS NULL) OR (ROLE = p_role));
  END CRFCCS_GET_ROLES;


  PROCEDURE CRFCCS_GET_USERS_IN_ROLE
  (
    p_application IN     VARCHAR2
  , p_role        IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  )
  IS
  BEGIN
    OPEN cv_results FOR
      SELECT DISTINCT NETID
      FROM WS_FCSTAFF
      WHERE APPLICATION = p_application
        AND ROLE = p_role
        AND TERMINATIONDATE >= CURRENT_TIMESTAMP;
  END CRFCCS_GET_USERS_IN_ROLE;


  PROCEDURE CRFCCS_GET_USER_ROLES
  (
    p_netid       IN     VARCHAR2
  , p_application IN     VARCHAR2
  , cv_results    IN OUT SYS_REFCURSOR
  )
  IS
  BEGIN
    OPEN cv_results FOR
      SELECT ROLE
      FROM WS_FCSTAFF
      WHERE APPLICATION = p_application
        AND NETID = LOWER(p_netid)
        AND TERMINATIONDATE >= CURRENT_TIMESTAMP;
  END CRFCCS_GET_USER_ROLES;


  PROCEDURE CRFCCS_IS_USER_IN_ROLE
  (
    p_netid       IN  VARCHAR2
  , p_application IN  VARCHAR2
  , p_role        IN  VARCHAR2
  , r_hasrole     OUT INTEGER
  )
  IS
  BEGIN
    SELECT COUNT(1) INTO r_hasrole
    FROM WS_FCSTAFF
    WHERE NETID = LOWER(p_netid)
      AND APPLICATION = p_application
      AND ROLE = p_role
      AND TERMINATIONDATE >= CURRENT_TIMESTAMP;
  END CRFCCS_IS_USER_IN_ROLE;


  -- -------------------------------------------------------------------------
  -- User / Auth Lookups
  -- -------------------------------------------------------------------------

  PROCEDURE CRFCCS_GET_USER_INFO
  (
    p_netid IN  VARCHAR2
  , r_name  OUT VARCHAR2
  , r_email OUT VARCHAR2
  )
  IS
  BEGIN
    SELECT a.NAME, b.EMAIL_ADDR
    INTO   r_name, r_email
    FROM   JS_V_NETIDALL a
    LEFT OUTER JOIN JS_V_ALLEMAILADDR b ON a.EMPLID = b.EMPLID
    WHERE  UPPER(a.SU_EXT_USERID) = UPPER(p_netid)
    AND    ROWNUM = 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      r_name  := NULL;
      r_email := NULL;
  END CRFCCS_GET_USER_INFO;


  PROCEDURE CRFCCS_AUTH_LOOKUP
  (
    p_netid             IN  VARCHAR2
  , p_application       IN  VARCHAR2
  , r_role              OUT VARCHAR2
  , r_terminationdate   OUT DATE
  , r_found             OUT INTEGER
  )
  IS
  BEGIN
    SELECT ROLE, TERMINATIONDATE
    INTO   r_role, r_terminationdate
    FROM   WS_FCSTAFF
    WHERE  NETID = LOWER(p_netid)
      AND  APPLICATION = p_application
      AND  ROWNUM = 1;
    r_found := 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      r_role            := NULL;
      r_terminationdate := NULL;
      r_found           := 0;
  END CRFCCS_AUTH_LOOKUP;


  -- -------------------------------------------------------------------------
  -- External Student Info
  -- -------------------------------------------------------------------------

  PROCEDURE CRFCCS_GET_ROOM
  (
    p_sbuid IN  INTEGER
  , r_room  OUT VARCHAR2
  )
  IS
  BEGIN
    SELECT access_level_1
    INTO r_room
    FROM (
      SELECT access_level_1 FROM ws_lenel      WHERE cardholder_id = p_sbuid
      UNION ALL
      SELECT access_level_1 FROM ws_lenel_test WHERE cardholder_id = p_sbuid
    )
    WHERE ROWNUM = 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN r_room := '';
  END CRFCCS_GET_ROOM;


  PROCEDURE CRFCCS_GET_AGE
  (
    p_sbuid IN  INTEGER
  , r_age   OUT INTEGER
  )
  IS
  BEGIN
    r_age := 0;
    -- SELECT TRUNC(MONTHS_BETWEEN(SYSDATE, DOB) / 12) INTO r_age
    -- FROM STARREZ_STUDENT WHERE STUDENTID = p_sbuid;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN r_age := 0;
  END CRFCCS_GET_AGE;


  PROCEDURE CRFCCS_GET_NAME
  (
    p_sbuid IN  INTEGER
  , r_fname OUT VARCHAR2
  , r_lname OUT VARCHAR2
  )
  IS
  BEGIN
    SELECT FIRST_NAME, LAST_NAME
    INTO   r_fname, r_lname
    FROM   JS_V_PERSONALINFO
    WHERE  EMPLID = p_sbuid
      AND  ROWNUM = 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN r_fname := NULL; r_lname := NULL;
  END CRFCCS_GET_NAME;

END WS_CR_CARDSWIPE;
