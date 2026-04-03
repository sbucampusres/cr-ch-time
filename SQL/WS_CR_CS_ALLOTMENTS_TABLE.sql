-- ============================================================
-- WS_CR_CS_ALLOTMENTS_TABLE.sql
-- Annual hourly allotments per department + shift category.
-- Run against CRPROD as CRADMIN before deploying the updated
-- WS_CR_CH package.
-- ============================================================

CREATE TABLE WS_CR_CS_ALLOTMENTS (
    ID          NUMBER          GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    DEPT_ID     NUMBER          NOT NULL,
    CATEGORY_ID NUMBER          NOT NULL,
    YEAR        NUMBER(4)       NOT NULL,
    HOURS       NUMBER(8, 2),
    APPLICATION VARCHAR2(10)    NOT NULL,
    CREATED_AT  TIMESTAMP       DEFAULT SYSTIMESTAMP NOT NULL,
    CREATED_BY  VARCHAR2(50),
    MODIFIED_AT TIMESTAMP,
    MODIFIED_BY VARCHAR2(50),
    CONSTRAINT WS_CR_CS_ALLOT_UQ
        UNIQUE (DEPT_ID, CATEGORY_ID, YEAR, APPLICATION)
);
