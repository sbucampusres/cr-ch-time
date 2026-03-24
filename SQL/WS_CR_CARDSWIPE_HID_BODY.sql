-- ============================================================================
-- WS_CR_CARDSWIPE_HID Package Body
-- ============================================================================

CREATE OR REPLACE PACKAGE BODY WS_CR_CARDSWIPE_HID AS

  PROCEDURE CRFCCS_GET_EMPLID_FROM_HID
  (
    p_hidnum IN  VARCHAR2
  , r_emplid OUT VARCHAR2
  )
  IS
  BEGIN
    SELECT ID_NUM
    INTO   r_emplid
    FROM   PS_IDCARD@csprod.world
    WHERE  HIDNUM = p_hidnum
      AND  ROWNUM = 1;
  EXCEPTION
    WHEN NO_DATA_FOUND THEN
      r_emplid := NULL;
  END CRFCCS_GET_EMPLID_FROM_HID;

END WS_CR_CARDSWIPE_HID;
/
