-- ============================================================
-- WS_CR_CH_PACKAGE_BODY.sql
-- Package body for Conference Housing procedures.
-- Run against CRPROD as CRADMIN after the spec.
-- ============================================================

CREATE OR REPLACE PACKAGE BODY WS_CR_CH AS

    -- --------------------------------------------------------
    -- CRCH_GET_SHIFT_CATEGORIES
    -- Returns all active shift categories for an application.
    -- --------------------------------------------------------
    PROCEDURE CRCH_GET_SHIFT_CATEGORIES(
        p_application   IN  VARCHAR2,
        r_cursor        OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN r_cursor FOR
            SELECT  ID, NAME, DESCRIPTION, APPLICATION, IS_ACTIVE,
                    CREATED_AT, CREATED_BY, MODIFIED_AT, MODIFIED_BY
            FROM    WS_CR_CS_SHIFT_CATEGORIES
            WHERE   APPLICATION = p_application
            AND     IS_ACTIVE   = 1
            ORDER BY NAME;
    END CRCH_GET_SHIFT_CATEGORIES;

    -- --------------------------------------------------------
    -- CRCH_ADD_UPDATE_SHIFT_CATEGORY
    -- Inserts (p_id NULL/0) or updates an existing category.
    -- --------------------------------------------------------
    PROCEDURE CRCH_ADD_UPDATE_SHIFT_CATEGORY(
        p_id            IN  NUMBER,
        p_name          IN  VARCHAR2,
        p_description   IN  VARCHAR2,
        p_application   IN  VARCHAR2,
        p_is_active     IN  NUMBER,
        p_user          IN  VARCHAR2,
        r_error         OUT VARCHAR2
    ) IS
    BEGIN
        r_error := NULL;

        IF p_id IS NULL OR p_id = 0 THEN
            INSERT INTO WS_CR_CS_SHIFT_CATEGORIES
                (NAME, DESCRIPTION, APPLICATION, IS_ACTIVE, CREATED_AT, CREATED_BY)
            VALUES
                (p_name, p_description, p_application, NVL(p_is_active, 1),
                 SYSTIMESTAMP, p_user);
        ELSE
            UPDATE WS_CR_CS_SHIFT_CATEGORIES
            SET    NAME        = p_name,
                   DESCRIPTION = p_description,
                   IS_ACTIVE   = NVL(p_is_active, 1),
                   MODIFIED_AT = SYSTIMESTAMP,
                   MODIFIED_BY = p_user
            WHERE  ID          = p_id
            AND    APPLICATION = p_application;

            IF SQL%ROWCOUNT = 0 THEN
                r_error := 'Shift category not found.';
                RETURN;
            END IF;
        END IF;

        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            r_error := 'Error saving shift category: ' || SQLERRM;
    END CRCH_ADD_UPDATE_SHIFT_CATEGORY;

    -- --------------------------------------------------------
    -- CRCH_DELETE_SHIFT_CATEGORY
    -- Soft-deletes a category by setting IS_ACTIVE = 0.
    -- --------------------------------------------------------
    PROCEDURE CRCH_DELETE_SHIFT_CATEGORY(
        p_id            IN  NUMBER,
        p_user          IN  VARCHAR2,
        r_error         OUT VARCHAR2
    ) IS
    BEGIN
        r_error := NULL;

        UPDATE WS_CR_CS_SHIFT_CATEGORIES
        SET    IS_ACTIVE   = 0,
               MODIFIED_AT = SYSTIMESTAMP,
               MODIFIED_BY = p_user
        WHERE  ID = p_id;

        IF SQL%ROWCOUNT = 0 THEN
            r_error := 'Shift category not found.';
            RETURN;
        END IF;

        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            r_error := 'Error deleting shift category: ' || SQLERRM;
    END CRCH_DELETE_SHIFT_CATEGORY;

    -- --------------------------------------------------------
    -- CRCH_GET_ALL_DEPARTMENTS_ADMIN
    -- Returns all departments for an application, including
    -- inactive ones, for the admin management page.
    -- --------------------------------------------------------
    PROCEDURE CRCH_GET_ALL_DEPARTMENTS_ADMIN(
        p_application   IN  VARCHAR2,
        r_cursor        OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN r_cursor FOR
            SELECT  DEPT_ID, NAME, APPLICATION, NVL(INACTIVE, 0) AS INACTIVE
            FROM    WS_FC_DEPARTMENTS
            WHERE   APPLICATION = p_application
            ORDER BY NAME;
    END CRCH_GET_ALL_DEPARTMENTS_ADMIN;

    -- --------------------------------------------------------
    -- CRCH_ADD_UPDATE_DEPARTMENT
    -- Inserts (p_dept_id NULL/0) or updates a department row.
    -- --------------------------------------------------------
    PROCEDURE CRCH_ADD_UPDATE_DEPARTMENT(
        p_dept_id       IN  NUMBER,
        p_name          IN  VARCHAR2,
        p_application   IN  VARCHAR2,
        p_inactive      IN  NUMBER,
        p_user          IN  VARCHAR2,
        r_error         OUT VARCHAR2
    ) IS
        v_new_id    NUMBER;
        v_count     NUMBER;
    BEGIN
        r_error := NULL;

        IF p_dept_id IS NULL OR p_dept_id = 0 THEN
            -- Check for duplicate name within the application
            SELECT COUNT(*) INTO v_count
            FROM   WS_FC_DEPARTMENTS
            WHERE  NAME        = p_name
            AND    APPLICATION = p_application;

            IF v_count > 0 THEN
                r_error := 'A department with that name already exists.';
                RETURN;
            END IF;

            SELECT NVL(MAX(DEPT_ID), 0) + 1 INTO v_new_id
            FROM   WS_FC_DEPARTMENTS;

            INSERT INTO WS_FC_DEPARTMENTS (DEPT_ID, NAME, APPLICATION, INACTIVE)
            VALUES (v_new_id, p_name, p_application, NVL(p_inactive, 0));
        ELSE
            UPDATE WS_FC_DEPARTMENTS
            SET    NAME        = p_name,
                   APPLICATION = p_application,
                   INACTIVE    = NVL(p_inactive, 0)
            WHERE  DEPT_ID = p_dept_id;

            IF SQL%ROWCOUNT = 0 THEN
                r_error := 'Department not found.';
                RETURN;
            END IF;
        END IF;

        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            r_error := 'Error saving department: ' || SQLERRM;
    END CRCH_ADD_UPDATE_DEPARTMENT;

    -- --------------------------------------------------------
    -- CRCH_DEACTIVATE_DEPARTMENT
    -- Soft-deactivates a department by setting INACTIVE = 1.
    -- --------------------------------------------------------
    PROCEDURE CRCH_DEACTIVATE_DEPARTMENT(
        p_dept_id       IN  NUMBER,
        p_user          IN  VARCHAR2,
        r_error         OUT VARCHAR2
    ) IS
    BEGIN
        r_error := NULL;

        UPDATE WS_FC_DEPARTMENTS
        SET    INACTIVE = 1
        WHERE  DEPT_ID  = p_dept_id;

        IF SQL%ROWCOUNT = 0 THEN
            r_error := 'Department not found.';
            RETURN;
        END IF;

        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            r_error := 'Error deactivating department: ' || SQLERRM;
    END CRCH_DEACTIVATE_DEPARTMENT;

    -- --------------------------------------------------------
    -- CRCH_GET_STAFF
    -- Returns all staff for an application including ROLE and
    -- DEPT_ID. Replaces CRFCCS_GET_STAFF which only returned
    -- NETID and TERMINATIONDATE.
    -- --------------------------------------------------------
    PROCEDURE CRCH_GET_STAFF(
        p_application   IN  VARCHAR2,
        r_cursor        OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN r_cursor FOR
            SELECT NETID, TERMINATIONDATE, ROLE, DEPT_ID
            FROM   WS_FCSTAFF
            WHERE  APPLICATION = p_application
            ORDER BY TERMINATIONDATE;
    END CRCH_GET_STAFF;

    -- --------------------------------------------------------
    -- CRCH_ADD_UPDATE_STAFF
    -- Inserts or fully updates a staff row including ROLE and
    -- DEPT_ID. Replaces CRFCCS_ADD_UPDATE_STAFF whose UPDATE
    -- statement omitted those two columns.
    -- --------------------------------------------------------
    PROCEDURE CRCH_ADD_UPDATE_STAFF(
        p_netid             IN  VARCHAR2,
        p_hostname          IN  VARCHAR2,
        p_terminationdate   IN  DATE,
        p_application       IN  VARCHAR2,
        p_role              IN  VARCHAR2,
        p_department        IN  VARCHAR2
    ) IS
        v_count NUMBER;
    BEGIN
        SELECT COUNT(1) INTO v_count
        FROM   WS_FCSTAFF
        WHERE  NETID        = LOWER(p_netid)
        AND    APPLICATION  = p_application;

        IF v_count > 0 THEN
            UPDATE WS_FCSTAFF
            SET    AUDIT_TIMESTAMP  = CURRENT_TIMESTAMP,
                   HOSTNAME         = p_hostname,
                   TERMINATIONDATE  = p_terminationdate,
                   ROLE             = p_role,
                   DEPT_ID          = p_department
            WHERE  NETID        = LOWER(p_netid)
            AND    APPLICATION  = p_application;
        ELSE
            INSERT INTO WS_FCSTAFF
                (NETID, AUDIT_TIMESTAMP, HOSTNAME, TERMINATIONDATE, APPLICATION, ROLE, DEPT_ID)
            VALUES
                (LOWER(p_netid), CURRENT_TIMESTAMP, p_hostname, p_terminationdate, p_application, p_role, p_department);
        END IF;

        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            RAISE;
    END CRCH_ADD_UPDATE_STAFF;

    -- --------------------------------------------------------
    -- CRCH_GET_USER_ROLES
    -- Returns the role for a staff member. Unlike the original
    -- CRFCCS_GET_USER_ROLES, this handles NULL TERMINATIONDATE
    -- correctly so active staff with no end date are included.
    -- --------------------------------------------------------
    PROCEDURE CRCH_GET_USER_ROLES(
        p_netid         IN  VARCHAR2,
        p_application   IN  VARCHAR2,
        r_cursor        OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN r_cursor FOR
            SELECT ROLE
            FROM   WS_FCSTAFF
            WHERE  APPLICATION  = p_application
            AND    NETID        = LOWER(p_netid)
            AND    (TERMINATIONDATE IS NULL OR TERMINATIONDATE >= CURRENT_TIMESTAMP);
    END CRCH_GET_USER_ROLES;

    -- --------------------------------------------------------
    -- CRCH_STAFF_CHECKIN
    -- Updated check-in that stores the shift category.
    -- Validates: not already checked in, not terminated.
    -- --------------------------------------------------------
    PROCEDURE CRCH_STAFF_CHECKIN(
        p_netid             IN  VARCHAR2,
        p_hostname          IN  VARCHAR2,
        p_ip                IN  VARCHAR2,
        p_application       IN  VARCHAR2,
        p_department_id     IN  NUMBER,
        p_shift_category_id IN  NUMBER,
        r_error             OUT VARCHAR2
    ) IS
        v_active_count  NUMBER;
        v_term_date     DATE;
    BEGIN
        r_error := NULL;

        -- Check if staff member has an open shift within the last 24 hours.
        -- Stale open rows older than 24 hours are ignored.
        SELECT COUNT(*)
        INTO   v_active_count
        FROM   WS_FCSTAFFWORKLOG
        WHERE  NETID              = p_netid
        AND    APPLICATION        = p_application
        AND    CHECKOUT_TIMESTAMP IS NULL
        AND    CHECKIN_TIMESTAMP  > SYSTIMESTAMP - INTERVAL '24' HOUR;

        IF v_active_count > 0 THEN
            r_error := 'You are already checked in. Please check out first.';
            RETURN;
        END IF;

        -- Validate staff record and termination date
        BEGIN
            SELECT TERMINATIONDATE
            INTO   v_term_date
            FROM   WS_FCSTAFF
            WHERE  NETID        = p_netid
            AND    APPLICATION  = p_application;

            IF v_term_date IS NOT NULL AND v_term_date < TRUNC(SYSDATE) THEN
                r_error := 'Your staff record has been terminated. Please contact an administrator.';
                RETURN;
            END IF;
        EXCEPTION
            WHEN NO_DATA_FOUND THEN
                r_error := 'Staff record not found. Please contact an administrator.';
                RETURN;
        END;

        INSERT INTO WS_FCSTAFFWORKLOG
            (NETID, CHECKIN_TIMESTAMP, CHECKIN_HOSTNAME, CHECKIN_IP,
             APPLICATION, DEPARTMENT_ID, SHIFT_CATEGORY_ID)
        VALUES
            (p_netid, SYSTIMESTAMP, p_hostname, p_ip,
             p_application, p_department_id, p_shift_category_id);

        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            r_error := 'Error during check-in: ' || SQLERRM;
    END CRCH_STAFF_CHECKIN;

    -- --------------------------------------------------------
    -- CRCH_GET_TIMECARD
    -- Returns timesheet entries joined with category name.
    -- --------------------------------------------------------
    PROCEDURE CRCH_GET_TIMECARD(
        p_begindate     IN  DATE,
        p_enddate       IN  DATE,
        p_netid         IN  VARCHAR2,
        p_department    IN  VARCHAR2,
        p_application   IN  VARCHAR2,
        r_cursor        OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN r_cursor FOR
            SELECT  ROWNUM AS ID,
                    ROWIDTOCHAR(w.ROWID) AS ROW_IDENTIFIER,
                    w.NETID,
                    w.CHECKIN_TIMESTAMP,
                    w.CHECKIN_HOSTNAME,
                    w.CHECKIN_IP,
                    w.CHECKOUT_TIMESTAMP,
                    w.CHECKOUT_HOSTNAME,
                    w.CHECKOUT_IP,
                    w.APPLICATION,
                    w.DEPARTMENT_ID,
                    d.NAME AS DEPARTMENT_NAME,
                    w.SHIFT_CATEGORY_ID,
                    sc.NAME AS SHIFT_CATEGORY_NAME
            FROM    WS_FCSTAFFWORKLOG w
            LEFT JOIN WS_CR_CS_SHIFT_CATEGORIES sc ON sc.ID = w.SHIFT_CATEGORY_ID
            LEFT JOIN WS_FC_DEPARTMENTS d ON d.DEPT_ID = w.DEPARTMENT_ID AND d.APPLICATION = w.APPLICATION
            WHERE   w.CHECKIN_TIMESTAMP >= p_begindate
            AND     w.CHECKIN_TIMESTAMP <  p_enddate
            AND     (p_netid       IS NULL OR w.NETID         = p_netid)
            AND     (p_department  IS NULL OR w.DEPARTMENT_ID = TO_NUMBER(p_department))
            AND     (p_application IS NULL OR w.APPLICATION   = p_application)
            ORDER BY w.CHECKIN_TIMESTAMP DESC;
    END CRCH_GET_TIMECARD;

    -- --------------------------------------------------------
    -- CRCH_GET_ALLOTMENTS
    -- Returns all allotment rows for an application + year.
    -- --------------------------------------------------------
    PROCEDURE CRCH_GET_ALLOTMENTS(
        p_application   IN  VARCHAR2,
        p_year          IN  NUMBER,
        r_cursor        OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN r_cursor FOR
            SELECT  ID, DEPT_ID, CATEGORY_ID, YEAR, HOURS, APPLICATION,
                    CREATED_AT, CREATED_BY, MODIFIED_AT, MODIFIED_BY
            FROM    WS_CR_CS_ALLOTMENTS
            WHERE   APPLICATION = p_application
            AND     YEAR        = p_year
            ORDER BY DEPT_ID, CATEGORY_ID;
    END CRCH_GET_ALLOTMENTS;

    -- --------------------------------------------------------
    -- CRCH_UPSERT_ALLOTMENT
    -- Inserts or updates a single dept+category+year cell.
    -- Passing NULL for p_hours stores NULL (no allotment).
    -- --------------------------------------------------------
    PROCEDURE CRCH_UPSERT_ALLOTMENT(
        p_application   IN  VARCHAR2,
        p_year          IN  NUMBER,
        p_dept_id       IN  NUMBER,
        p_category_id   IN  NUMBER,
        p_hours         IN  NUMBER,
        p_user          IN  VARCHAR2,
        r_error         OUT VARCHAR2
    ) IS
    BEGIN
        r_error := NULL;

        MERGE INTO WS_CR_CS_ALLOTMENTS a
        USING DUAL
        ON (    a.DEPT_ID     = p_dept_id
            AND a.CATEGORY_ID = p_category_id
            AND a.YEAR        = p_year
            AND a.APPLICATION = p_application)
        WHEN MATCHED THEN
            UPDATE SET
                HOURS       = p_hours,
                MODIFIED_AT = SYSTIMESTAMP,
                MODIFIED_BY = p_user
        WHEN NOT MATCHED THEN
            INSERT (DEPT_ID, CATEGORY_ID, YEAR, HOURS, APPLICATION, CREATED_AT, CREATED_BY)
            VALUES (p_dept_id, p_category_id, p_year, p_hours, p_application, SYSTIMESTAMP, p_user);

        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            r_error := 'Error saving allotment: ' || SQLERRM;
    END CRCH_UPSERT_ALLOTMENT;

    -- --------------------------------------------------------
    -- CRCH_GET_HOURS_USED
    -- Sums completed shift hours from WS_FCSTAFFWORKLOG,
    -- grouped by department + shift category for a given year.
    -- Only includes rows where CHECKOUT_TIMESTAMP IS NOT NULL.
    -- --------------------------------------------------------
    PROCEDURE CRCH_GET_HOURS_USED(
        p_application   IN  VARCHAR2,
        p_year          IN  NUMBER,
        r_cursor        OUT SYS_REFCURSOR
    ) IS
    BEGIN
        OPEN r_cursor FOR
            SELECT
                w.DEPARTMENT_ID,
                w.SHIFT_CATEGORY_ID,
                SUM(
                    (CAST(w.CHECKOUT_TIMESTAMP AS DATE) - CAST(w.CHECKIN_TIMESTAMP AS DATE)) * 24
                ) AS HOURS_USED
            FROM  WS_FCSTAFFWORKLOG w
            WHERE w.APPLICATION        = p_application
            AND   EXTRACT(YEAR FROM w.CHECKIN_TIMESTAMP) = p_year
            AND   w.CHECKOUT_TIMESTAMP IS NOT NULL
            AND   w.DEPARTMENT_ID      IS NOT NULL
            AND   w.SHIFT_CATEGORY_ID  IS NOT NULL
            GROUP BY w.DEPARTMENT_ID, w.SHIFT_CATEGORY_ID
            ORDER BY w.DEPARTMENT_ID, w.SHIFT_CATEGORY_ID;
    END CRCH_GET_HOURS_USED;

    -- --------------------------------------------------------
    -- CRCH_UPDATE_TIMESHEET_ENTRY
    -- Updates an existing WS_FCSTAFFWORKLOG row identified by
    -- NETID + original check-in timestamp (cast to DATE to
    -- strip sub-second precision stored by SYSTIMESTAMP).
    -- Pass NULL for p_checkout_timestamp to clear the checkout
    -- (sets entry back to active/checked-in state).
    -- --------------------------------------------------------
    PROCEDURE CRCH_UPDATE_TIMESHEET_ENTRY(
        p_row_id                IN  VARCHAR2,
        p_checkin_timestamp     IN  DATE,
        p_checkout_timestamp    IN  DATE,
        p_department_id         IN  NUMBER,
        p_shift_category_id     IN  NUMBER,
        p_user                  IN  VARCHAR2,
        r_error                 OUT VARCHAR2
    ) IS
    BEGIN
        r_error := NULL;

        UPDATE WS_FCSTAFFWORKLOG
        SET    CHECKIN_TIMESTAMP   = p_checkin_timestamp,
               CHECKOUT_TIMESTAMP  = p_checkout_timestamp,
               DEPARTMENT_ID       = p_department_id,
               SHIFT_CATEGORY_ID   = p_shift_category_id
        WHERE  ROWID = CHARTOROWID(p_row_id);

        IF SQL%ROWCOUNT = 0 THEN
            r_error := 'Timesheet entry not found.';
            RETURN;
        END IF;

        COMMIT;
    EXCEPTION
        WHEN OTHERS THEN
            ROLLBACK;
            r_error := 'Error updating timesheet entry: ' || SQLERRM;
    END CRCH_UPDATE_TIMESHEET_ENTRY;

END WS_CR_CH;
