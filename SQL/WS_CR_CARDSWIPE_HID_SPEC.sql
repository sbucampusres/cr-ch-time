-- ============================================================================
-- WS_CR_CARDSWIPE_HID Package Specification
-- Campus Residences Card Swipe — HID Card Lookup
--
-- Handles lookups from HID card wedge reader output to student EMPLID.
-- ============================================================================

CREATE OR REPLACE PACKAGE WS_CR_CARDSWIPE_HID AS

  -- Given a HIDNUM (6-digit HID card number), returns the student EMPLID
  -- from the PS_IDCARD table via the csprod.world database link.
  -- Returns NULL in r_emplid if not found.
  PROCEDURE CRFCCS_GET_EMPLID_FROM_HID
  (
    p_hidnum IN  VARCHAR2
  , r_emplid OUT VARCHAR2
  );

END WS_CR_CARDSWIPE_HID;
