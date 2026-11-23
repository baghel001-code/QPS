/*
    QPS — seed data for TEST and local development databases. Run AFTER Database/qps.sql on an EMPTY database:

        sqlcmd -S <server> -d <test db> -b -i Database/test/00-test-prerequisites.sql
        sqlcmd -S <server> -d <test db> -b -i Database/qps.sql
        sqlcmd -S <server> -d <test db> -b -i Database/test/01-seed-test-data.sql

    Everything here is invented test data (no real employees, vendors, POs or e-mail addresses).
    Do NOT run it on the production database.

    Fixed IDs (the tests rely on them):
      Employees (USER_MASTER)  ID 1 E1001 admin (Administrator)   ID 2 E1002 QA      ID 3 E1003 QAM
                               ID 4 E1004 buyer (Buyer)           ID 5 E1005 resigned / inactive
      Vendors (VENDORS_MASTER) ID 1 V0001 Acme Textiles (active)  ID 2 V0002 Blue Loom (must change password)
                               ID 3 V0003 Old Mills (inactive)
      Vendor test password     Qps@Test2026   (PBKDF2 hash below, same format as Application.Services.PasswordHasher)
      POs                      POID 1 4500000001 (V0001)          POID 2 4500000002 (V0002)
      Inspection requests      INSP_ID 1 on PO 1 (status 7 completed)   INSP_ID 2 on PO 2 (status 1 new)
      Inspections              INSPECT_ID 1 for request 1 (complete, Pass)
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @VendorPasswordHash VARCHAR(500) = 'AAECAwQFBgcICQoLDA0OD0X71osGbLnLc0MNjKFEQ17Lf1YMhI2mpYCVRZxSO/fM';  -- Qps@Test2026
DECLARE @Now DATETIME = '2026-11-01T09:00:00';

/* ---------------- Lookup masters ---------------- */
INSERT INTO MASTER_REGION (REGION_ID, REGION, QAM_EMP_CODE, DESCRIPTION, ACTIVE) VALUES
 (1, 'North', 'E1003', 'North region', 1),
 (2, 'South', 'E1003', 'South region', 1),
 (3, 'East',  NULL,    'East region',  1),
 (4, 'West',  NULL,    'West region (inactive)', 0);

INSERT INTO MASTER_YESNO (YESNO_ID, YESNO, ACTIVE) VALUES (1, 'Yes', 1), (2, 'No', 1), (3, 'NA', 1);
INSERT INTO MASTER_PASSFAIL (PASSFAIL_ID, PASSFAIL, ACTIVE) VALUES (1, 'Pass', 1), (2, 'Fail', 1), (3, 'Hold', 1), (4, 'Retired', 0);
INSERT INTO MASTER_UNIT (UNIT_ID, UNIT, ACTIVE) VALUES (1, 'Cartons', 1), (2, 'Bags', 1), (3, 'Pieces', 1);
INSERT INTO MASTER_INSPECTION_TYPE (INSPECTION_TYPE_ID, INSPECTION_TYPE, DESCRIPTION, ACTIVE) VALUES
 (1, 'Inline', 'Inline inspection', 1), (2, 'Final', 'Final inspection', 1), (3, 'Re-Inspection', 'Re-inspection', 1);
-- The codes the stored procedures use (SP_SAVE_PO, SP_SAVE_INSPECTION, SP_UPDATE_POHEADER, SP_DOI_ASSIGNED_BY_QA):
-- 7 = completed, 8 = reset by admin (counted as open), 9 = cancelled, 10 = abortive; 5 / 6 = date / inspector assigned.
INSERT INTO INSPECTION_REQUEST_STATUS (INSP_STATUS_ID, INSP_STATUS, DESCRIPTION) VALUES
 (1, 'New', 'Raised by the vendor'), (2, 'Updated', 'Changed by the vendor'),
 (5, 'Date Assigned', 'QA assigned the date of inspection'), (6, 'Inspector Assigned', 'Inspector assigned'),
 (7, 'Completed', 'Inspection completed'), (8, 'Reset by Admin', 'Completed report re-opened by Admin'),
 (9, 'Cancelled', 'Cancelled'), (10, 'Abortive', 'Inspection could not be carried out');
INSERT INTO QPS_STATUS (STATUS, DESCRIPTION) VALUES ('Pending', 'Waiting for approval'), ('Approved', 'Approved'), ('Rejected', 'Rejected');
INSERT INTO MASTER_APPROVE_REJECT (ID, STATUS, DESCRIPTION) VALUES (1, 'Approved', 'Approved'), (2, 'Rejected', 'Rejected');
INSERT INTO MASTER_AQL (BATCH_SIZE_MIN, BATCH_SIZE_MAX, SAMPLE_SIZE, AQL_2_5_MAJOR, AQL_2_5_MINOR) VALUES
 (2, 8, 2, 0, 1), (9, 15, 3, 0, 1), (16, 25, 5, 0, 1), (26, 50, 8, 0, 1), (51, 90, 13, 1, 2),
 (91, 150, 20, 1, 3), (151, 280, 32, 2, 3), (281, 500, 50, 3, 5), (501, 1200, 80, 5, 7), (1201, 3200, 125, 7, 10);
INSERT INTO MASTER_ABORTIVE_REASON (REASON, IS_ACTIVE) VALUES ('Goods not ready', 1), ('Factory closed', 1), ('Old reason', 0);
INSERT INTO MASTER_COLOR_CODE (COLOR_CODE_ID, COLOR_CODE, DESCRIPTION, ACTIVE, ORDER_NO) VALUES
 (1, 'Red', 'Critical', 1, 1), (2, 'Amber', 'Attention', 1, 2), (3, 'Green', 'OK', 1, 3);
INSERT INTO MASTER_CLASSIFICATION (CLASSIFICATION_ID, CLASSIFICATION, DESCRIPTION, ACTIVE) VALUES (1, 'Critical', NULL, 1), (2, 'Major', NULL, 1), (3, 'Minor', NULL, 1);
INSERT INTO MASTER_CONFORM (CONFORM_ID, CONFORM, DESCRIPTION, ACTIVE) VALUES (1, 'Conform', NULL, 1), (2, 'Not Conform', NULL, 1);
INSERT INTO MASTER_FAIL_REASON (FAIL_REASON_ID, FAIL_REASON, DESCRIPTION, ACTIVE) VALUES (1, 'Measurement', NULL, 1), (2, 'Workmanship', NULL, 1);
INSERT INTO MASTER_RESULT (RESULT_TYPE_ID, RESULT, DESCRIPTION, ACTIVE) VALUES (1, 'Pass', NULL, 1), (2, 'Fail', NULL, 1);
INSERT INTO MASTER_BUSINESS_HEAD (ID, HEAD, DESCRIPTION, ACTIVE) VALUES (1, 'Apparel', 'Apparel business', 1), (2, 'Non-Apparel', 'Non-apparel business', 1);

/* ---------------- Roles and menus ---------------- */
SET IDENTITY_INSERT ROLE_MASTER ON;
INSERT INTO ROLE_MASTER (ID, ROLE_NAME, MENU, CMS, MEDIA, USER_MANAGEMENT, ACTIVE_STATUS, INSERTED_BY) VALUES
 (1,    'Administrator',           1, 1, 1, 1, 1, 1),
 (2,    'Buyer',                   0, 0, 0, 0, 1, 1),
 (3,    'QC',                      0, 0, 0, 0, 1, 1),
 (4,    'View',                    0, 0, 0, 0, 1, 1),
 (5,    'Admin',                   1, 0, 0, 1, 1, 1),
 (1006, 'QA',                      0, 0, 0, 0, 1, 1),
 (1007, 'QAM',                     0, 0, 0, 0, 1, 1),
 (1008, 'QA Admin',                0, 0, 0, 1, 1, 1),
 (1009, 'CM',                      0, 0, 0, 0, 1, 1),
 (1010, 'Category Head',           0, 0, 0, 0, 1, 1),
 (1011, 'Associate Category Head', 0, 0, 0, 0, 1, 1),
 (1012, 'Merchandiser',            0, 0, 0, 0, 1, 1),
 (1013, 'BFT',                     0, 0, 0, 0, 1, 1),
 (1014, 'ASN',                     0, 0, 0, 0, 1, 1);
SET IDENTITY_INSERT ROLE_MASTER OFF;

SET IDENTITY_INSERT MENU_MASTER ON;
INSERT INTO MENU_MASTER (ID, LABEL, URL, [ORDER], PARENT_ID, WEBSITE_VISIBILITY, ACTIVE_STATUS, PAGE_TITLE, INSERTED_BY) VALUES
 (1, 'Dashboard',   '/',             1, NULL, 1, 1, 'Dashboard',   1),
 (2, 'Inspections', '/inspections',  2, NULL, 1, 1, 'Inspections', 1),
 (3, 'Masters',     NULL,            3, NULL, 1, 1, 'Masters',     1),
 (4, 'Users',       '/users',        4, 3,    1, 1, 'Users',       1),
 (5, 'Old report',  '/old-report',   5, NULL, 1, 0, 'Old report',  1);   -- inactive: never returned
SET IDENTITY_INSERT MENU_MASTER OFF;

/* ---------------- Employees ---------------- */
SET IDENTITY_INSERT USER_MASTER ON;
INSERT INTO USER_MASTER (ID, EMP_CODE, EMP_NAME, EMP_LOCATION, EMP_LOC_CODE, EMP_DEPT_NAME, EMP_DESIG, EMP_CONTACT_NO, EMP_MAIL_ID,
                         IS_RESIGN, EMP_STATUS, ROLE_ID, REGION_ID, REGION_IDS, INSERTED_BY) VALUES
 (1, 'E1001', 'Asha Admin',   'Head Office', 'HO', 'IT',      'Manager',   '9000000001', 'asha.admin@example.com',   0, 1, 1,    1, '1,2', 1),
 (2, 'E1002', 'Ravi QA',      'Delhi DC',    'DC1', 'Quality', 'Inspector', '9000000002', 'ravi.qa@example.com',      0, 1, 1006, 1, '1',   1),
 (3, 'E1003', 'Meera QAM',    'Delhi DC',    'DC1', 'Quality', 'Manager',   '9000000003', 'meera.qam@example.com',    0, 1, 1007, 1, '1,2', 1),
 (4, 'E1004', 'Karan Buyer',  'Head Office', 'HO', 'Buying',  'Buyer',     '9000000004', 'karan.buyer@example.com',  0, 1, 2,    2, '2',   1),
 (5, 'E1005', 'Old Employee', 'Head Office', 'HO', 'Buying',  'Buyer',     '9000000005', 'old.employee@example.com', 1, 0, 2,    2, '2',   1);
SET IDENTITY_INSERT USER_MASTER OFF;

INSERT INTO USER_ROLE_DETAILS (USER_ID, MENU_ID, PERMISSION) VALUES
 (1, 1, 'FULL'), (1, 2, 'FULL'), (1, 3, 'FULL'), (1, 4, 'FULL'), (1, 5, 'FULL'),
 (2, 1, 'VIEW'), (2, 2, 'EDIT');

INSERT INTO auth.UserSecurity (UserId, AccountType, PasswordHash, SecurityStamp, FailedLoginCount, MustChangePassword, UpdatedUtc) VALUES
 ('E1001', 'Employee', '', 'STAMP-E1001', 0, 0, '2026-11-01T09:00:00+00:00');   -- employees: password checked by HRMS, no hash

INSERT INTO MASTER_HIERARCHY (EMP_CODE, EMP_NAME, EMAIL_ID, MOBILE_NO, ROLE_ID, REPORTING_EMP_ID, IS_ACTIVE) VALUES
 ('H001', 'Nita Category Head', 'nita.ch@example.com',  '9100000001', 1010, NULL,   1),
 ('H002', 'Arun Assoc CH',      'arun.ach@example.com', '9100000002', 1011, 'H001', 1),
 ('H003', 'Karan Buyer',        'karan.buyer@example.com', '9100000003', 2,  'H002', 1),
 ('H005', 'Vikram Category Head', 'vikram.ch@example.com', '9100000005', 1010, NULL, 1);   -- second chain, for tests that add members

/* ---------------- Vendors ---------------- */
SET IDENTITY_INSERT VENDORS_MASTER ON;
INSERT INTO VENDORS_MASTER (VENDOR_ID, VENDOR_CODE, VENDOR_NAME, GST_NO, PASSWORD, DEFAULT_PASSWORD_CHANGED, IS_ACTIVE, CREATED_DATE, CREATED_BY, IS_EMAIL_SENT) VALUES
 (1, 'V0001', 'Acme Textiles', '27AAAAA0000A1Z5', @VendorPasswordHash, 1, 1, @Now, 1, 1),
 (2, 'V0002', 'Blue Loom',     '07BBBBB1111B1Z6', @VendorPasswordHash, 0, 1, @Now, 1, 1),
 (3, 'V0003', 'Old Mills',     '29CCCCC2222C1Z7', @VendorPasswordHash, 1, 0, @Now, 1, 0);
SET IDENTITY_INSERT VENDORS_MASTER OFF;

SET IDENTITY_INSERT VENDOR_ADDRESS ON;
INSERT INTO VENDOR_ADDRESS (ADDRESS_ID, VENDOR_ID, ADDRESS_LINE1, ADDRESS_LINE2, CITY, PINCODE, EMAIL_IDS, MOBILE_NUMBERS, REGION_CODE, REGION_DESCRIPTION,
                            IS_DEFAULT, CREATED_DATE, CREATED_BY, IS_INSPECTION_LOCATION, CONTACT_PERSON) VALUES
 (1, 1, N'Plot 1, Industrial Area', N'Phase 2', N'Mumbai',  N'400001', 'orders@acme.example.com',     '9200000001', '1', 'North', 1, @Now, 1, 1, 'Sunil'),
 (2, 1, N'Unit 7, Textile Park',    NULL,       N'Surat',   N'395001', 'surat@acme.example.com',      '9200000002', '1', 'North', 0, @Now, 1, 1, 'Priya'),
 (3, 2, N'12 Loom Street',          NULL,       N'Delhi',   N'110001', 'contact@blueloom.example.com', '9200000003', '2', 'South', 1, @Now, 1, 1, 'Imran'),
 (4, 3, N'1 Mill Road',             NULL,       N'Kolkata', N'700001', 'office@oldmills.example.com', '9200000004', '3', 'East',  1, @Now, 1, 0, 'Bose');
SET IDENTITY_INSERT VENDOR_ADDRESS OFF;

INSERT INTO auth.UserSecurity (UserId, AccountType, PasswordHash, SecurityStamp, FailedLoginCount, MustChangePassword, UpdatedUtc) VALUES
 ('V0001', 'Vendor', @VendorPasswordHash, 'STAMP-V0001', 0, 0, '2026-11-01T09:00:00+00:00'),
 ('V0002', 'Vendor', @VendorPasswordHash, 'STAMP-V0002', 0, 1, '2026-11-01T09:00:00+00:00'),
 ('V0003', 'Vendor', @VendorPasswordHash, 'STAMP-V0003', 0, 0, '2026-11-01T09:00:00+00:00');

/* ---------------- Purchase orders, inspections ---------------- */
SET IDENTITY_INSERT POHEADER ON;
INSERT INTO POHEADER (POID, PO_NUMBER, VENDOR_CODE, VENDOR_NAME, FABRIC, COLOR, TOTAL_QTY, CREATED_ON, CREATED_BY, APPARELS, CATEGORY,
                      BUYER_NAME, BUYER_ID, MC, MC_TEXT, MRP, BRAND_ID, FIT) VALUES
 (1, '4500000001', 'V0001', 'Acme Textiles', 'Cotton', 'Blue', 1200, @Now, 1, 'Apparel', 'Mens Shirts', 'Karan Buyer', 'E1004', 'MC101', 'Mens Casual Shirt', 799.00, 'BRAND-A', 'Slim'),
 (2, '4500000002', 'V0002', 'Blue Loom',     'Denim',  'Indigo', 800, @Now, 1, 'Apparel', 'Mens Jeans',  'Karan Buyer', 'E1004', 'MC202', 'Mens Denim',        1299.00, 'BRAND-B', 'Regular');
SET IDENTITY_INSERT POHEADER OFF;

INSERT INTO PODETAIL (POID, LINE_ITEM, ARTICLE, ARTICLE_DESC, PREPACK_QTY, PURCHASE_TOT_QTY, EACHES, CREATED_ON, CREATED_BY, MRP, ATTYP) VALUES
 (1, '10', 'ART-1001-M', 'Shirt blue M', 0, 600, 600, @Now, 1, 799.00, '00'),
 (1, '20', 'ART-1001-L', 'Shirt blue L', 0, 600, 600, @Now, 1, 799.00, '00'),
 (2, '10', 'ART-2001-32', 'Jeans indigo 32', 0, 800, 800, @Now, 1, 1299.00, '00');

SET IDENTITY_INSERT INSPECTION_REQUESTS ON;
INSERT INTO INSPECTION_REQUESTS (INSP_ID, POID, OFFERED_QTY, DATE_OF_INSPECTION, ADDRESS_ID, CREATED_ON, CREATED_BY, REGION_ID, QA_ASSIGNED_DOI,
                                 INSPECTOR_EMP_CODE, IS_DATE_ASSIGNED_BY_QA, IS_INSPECTOR_ASSIGNED, STATUS_CODE, INSPECTION_TYPE_ID, INSPECTION_NO, IS_QC_AT_DC) VALUES
 (1, 1, 1200, '2026-11-05', 1, @Now, 1, 1, '2026-11-05', 'E1002', 1, 1, 7, 2, 'INS-2026-0001', 0),   -- 7 = completed
 (2, 2,  800, '2026-11-20', 3, @Now, 2, 2, NULL,         NULL,    0, 0, 1, 1, 'INS-2026-0002', 0);
SET IDENTITY_INSERT INSPECTION_REQUESTS OFF;

SET IDENTITY_INSERT INSPECTIONS ON;
INSERT INTO INSPECTIONS (INSPECT_ID, INSPECTION_ID, INSPECTOR_CODE, OFFERED_QTY, GPT_NO, GPT_STATUS, NO_OF_BAGS_CARTONS_QTY, NO_OF_BAGS_CARTONS_UNIT,
                         PACKING_STATUS, INLINE_DONE, PACKING_LIST, FIBER_CONTENT_ON_PO, FIBER_CONTENT_ON_TEST_REPORT, FIBER_CONTENT_ON_WC,
                         GSM_ON_PO, GSM_ON_TEST_REPORT, MEASUREMENT_SAMPLE_SIZE, NO_OF_CHECK, NO_OF_CHECK_UNIT, AQL_ID, DEFECTS_STATUS,
                         INSPECTION_RESULT, NAME_OF_FACTORY_REPRESENTATIVE, IS_COMPLETE, CREATED_BY, CREATED_DATE, DATE_OF_COMPLETION, COLOR_CODE) VALUES
 (1, 1, 'E1002', 1200, 'GPT-0001', 1, 40, 1, 1, 1, 1, '100% Cotton', '100% Cotton', '100% Cotton', '140', '140', 1, 80, 3, 9, 1,
  1, 'Sunil', 1, 2, '2026-11-05T15:00:00', '2026-11-05T16:00:00', 3);
SET IDENTITY_INSERT INSPECTIONS OFF;

INSERT INTO INSPECTION_DEFECTS (INSPECTION_ID, DEFECTS, DIMENSION, MAJOR, MINOR, CREATED_BY, CREATED_DATE) VALUES
 (1, 'Loose thread', 'Sleeve', 0, 2, 2, '2026-11-05T15:30:00'),
 (1, 'Shade variation', 'Body', 1, 0, 2, '2026-11-05T15:35:00');

/* ---------------- E-mail list, API master ---------------- */
INSERT INTO EMAIL_LIST (EMAIL_ID, NAME, IS_ACTIVE, CREATED_BY_EMP_CODE, LAST_UPDATED_DATE) VALUES
 ('qa.team@example.com', 'QA Team', 1, 'E1001', @Now),
 ('old.list@example.com', 'Old List', 0, 'E1001', @Now);

SET IDENTITY_INSERT BASE_URL_MASTER ON;
INSERT INTO BASE_URL_MASTER (ID, NAME, API_BASE_URL) VALUES (1, 'QPSAPI', 'http://localhost:5279/api/v1/<apimodule>');
SET IDENTITY_INSERT BASE_URL_MASTER OFF;

INSERT INTO API_MASTER (API_NAME, TASK_NAME, BASE_URL_ID, API_MODULE_NAME, API_END_POINT, API_HTTP_METHOD, API_CONTENT_TYPE, API_TIMEOUT) VALUES
 ('QPS API', 'SELECT_ALL_ROLES',   1, 'SelectAllRoles',   NULL,          'POST', 'application/json', 60),
 ('QPS API', 'INSERT_UPDATE_ROLE', 1, 'InsertUpdateRole', NULL,          'POST', 'application/json', 60),
 ('QPS API', 'GET_LIST_BY_PARM',   1, 'GetList/<PARM>',   '?page=1',     'GET',  'application/json', 30);   -- <PARM> filled from @PARM

COMMIT TRANSACTION;
GO
