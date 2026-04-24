-- ============================================================
-- WS_CR_CH_PACKAGE_SPEC.sql
-- Package specification for Conference Housing procedures.
-- Contains new procedures and updated versions of existing
-- WS_CR_CARDSWIPE procedures that need the shift category field.
-- Run against CRPROD as CRADMIN.
-- ============================================================

CREATE OR REPLACE PACKAGE WS_CR_CH AS

    -- --------------------------------------------------------
    -- Shift Category management
    -- --------------------------------------------------------

    PROCEDURE CRCH_GET_SHIFT_CATEGORIES(
        p_application   IN  VARCHAR2,
        r_cursor        OUT SYS_REFCURSOR
    );

    PROCEDURE CRCH_ADD_UPDATE_SHIFT_CATEGORY(
        p_id            IN  NUMBER,
        p_name          IN  VARCHAR2,
        p_description   IN  VARCHAR2,
        p_application   IN  VARCHAR2,
        p_is_active     IN  NUMBER,
        p_user          IN  VARCHAR2,
        r_error         OUT VARCHAR2
    );

    PROCEDURE CRCH_DELETE_SHIFT_CATEGORY(
        p_id            IN  NUMBER,
        p_user          IN  VARCHAR2,
        r_error         OUT VARCHAR2
    );

    -- --------------------------------------------------------
    -- Department management (admin)
    -- --------------------------------------------------------

    PROCEDURE CRCH_GET_ALL_DEPARTMENTS_ADMIN(
        p_application   IN  VARCHAR2,
        r_cursor        OUT SYS_REFCURSOR
    );

    PROCEDURE CRCH_ADD_UPDATE_DEPARTMENT(
        p_dept_id       IN  NUMBER,
        p_name          IN  VARCHAR2,
        p_application   IN  VARCHAR2,
        p_inactive      IN  NUMBER,
        p_user          IN  VARCHAR2,
        r_error         OUT VARCHAR2
    );

    PROCEDURE CRCH_DEACTIVATE_DEPARTMENT(
        p_dept_id       IN  NUMBER,
        p_user          IN  VARCHAR2,
        r_error         OUT VARCHAR2
    );

    -- --------------------------------------------------------
    -- Staff management
    -- Replaces WS_CR_CARDSWIPE.CRFCCS_GET_STAFF,
    --          WS_CR_CARDSWIPE.CRFCCS_ADD_UPDATE_STAFF,
    --          WS_CR_CARDSWIPE.CRFCCS_GET_USER_ROLES
    -- --------------------------------------------------------

    PROCEDURE CRCH_GET_STAFF(
        p_application   IN  VARCHAR2,
        r_cursor        OUT SYS_REFCURSOR
    );

    PROCEDURE CRCH_ADD_UPDATE_STAFF(
        p_netid             IN  VARCHAR2,
        p_hostname          IN  VARCHAR2,
        p_terminationdate   IN  DATE,
        p_application       IN  VARCHAR2,
        p_role              IN  VARCHAR2,
        p_department        IN  VARCHAR2
    );

    PROCEDURE CRCH_GET_USER_ROLES(
        p_netid         IN  VARCHAR2,
        p_application   IN  VARCHAR2,
        r_cursor        OUT SYS_REFCURSOR
    );

    -- --------------------------------------------------------
    -- Updated Staff Check-In (adds p_shift_category_id)
    -- Replaces WS_CR_CARDSWIPE.CRFCCS_STAFF_CHECKIN
    -- --------------------------------------------------------

    PROCEDURE CRCH_STAFF_CHECKIN(
        p_netid             IN  VARCHAR2,
        p_hostname          IN  VARCHAR2,
        p_ip                IN  VARCHAR2,
        p_application       IN  VARCHAR2,
        p_department_id     IN  NUMBER,
        p_shift_category_id IN  NUMBER,
        r_error             OUT VARCHAR2
    );

    -- --------------------------------------------------------
    -- Updated timecard report (includes SHIFT_CATEGORY_NAME)
    -- Replaces WS_CR_CARDSWIPE.CRFCCS_GET_TIMECARD
    -- --------------------------------------------------------

    PROCEDURE CRCH_GET_TIMECARD(
        p_begindate     IN  DATE,
        p_enddate       IN  DATE,
        p_netid         IN  VARCHAR2,
        p_department    IN  VARCHAR2,
        p_application   IN  VARCHAR2,
        r_cursor        OUT SYS_REFCURSOR
    );

    -- --------------------------------------------------------
    -- Annual allotment management
    -- --------------------------------------------------------

    PROCEDURE CRCH_GET_ALLOTMENTS(
        p_application   IN  VARCHAR2,
        p_year          IN  NUMBER,
        r_cursor        OUT SYS_REFCURSOR
    );

    PROCEDURE CRCH_UPSERT_ALLOTMENT(
        p_application   IN  VARCHAR2,
        p_year          IN  NUMBER,
        p_dept_id       IN  NUMBER,
        p_category_id   IN  NUMBER,
        p_hours         IN  NUMBER,
        p_user          IN  VARCHAR2,
        r_error         OUT VARCHAR2
    );

    PROCEDURE CRCH_GET_HOURS_USED(
        p_application   IN  VARCHAR2,
        p_year          IN  NUMBER,
        r_cursor        OUT SYS_REFCURSOR
    );

    -- --------------------------------------------------------
    -- Timesheet entry editing (admin)
    -- --------------------------------------------------------

    PROCEDURE CRCH_UPDATE_TIMESHEET_ENTRY(
        p_row_id                IN  VARCHAR2,
        p_checkin_timestamp     IN  DATE,
        p_checkout_timestamp    IN  DATE,
        p_department_id         IN  NUMBER,
        p_shift_category_id     IN  NUMBER,
        p_user                  IN  VARCHAR2,
        r_error                 OUT VARCHAR2
    );

END WS_CR_CH;