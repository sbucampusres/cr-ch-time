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

END WS_CR_CH;