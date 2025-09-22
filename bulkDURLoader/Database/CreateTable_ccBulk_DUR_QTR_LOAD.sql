-- SQL Script to create ccBulk_DUR_QTR_LOAD table in Oracle
-- This script creates the table structure for DUR (Drug Utilization Review) quarterly loads

-- Drop table if it exists (optional - comment out if you want to preserve existing data)
-- DROP TABLE ccBulk_DUR_QTR_LOAD;

CREATE TABLE ccBulk_DUR_QTR_LOAD (
    ID NUMBER(19) NOT NULL,
    MEMBER_ID VARCHAR2(50) NOT NULL,
    PRESCRIPTION_NUMBER VARCHAR2(50),
    NDC VARCHAR2(11),
    SERVICE_DATE DATE,
    PROVIDER_ID VARCHAR2(50),
    PHARMACY_ID VARCHAR2(50),
    DRUG_NAME VARCHAR2(255),
    DRUG_STRENGTH VARCHAR2(50),
    QUANTITY NUMBER(10,2),
    DAYS_SUPPLY NUMBER(10),
    PAID_AMOUNT NUMBER(10,2),
    DUR_ALERT_CODE VARCHAR2(10),
    DUR_ALERT_DESCRIPTION VARCHAR2(500),
    QUARTER VARCHAR2(2) NOT NULL,
    YEAR NUMBER(4) NOT NULL,
    BATCH_ID VARCHAR2(50),
    CREATED_DATE DATE DEFAULT SYSDATE NOT NULL,
    UPDATED_DATE DATE,
    STATUS VARCHAR2(20) DEFAULT 'PENDING' NOT NULL,
    ERROR_MESSAGE VARCHAR2(1000),
    ADDITIONAL_DATA CLOB,
    
    -- Primary key constraint
    CONSTRAINT PK_ccBulk_DUR_QTR_LOAD PRIMARY KEY (ID),
    
    -- Check constraints
    CONSTRAINT CHK_QUARTER CHECK (QUARTER IN ('Q1', 'Q2', 'Q3', 'Q4')),
    CONSTRAINT CHK_YEAR CHECK (YEAR >= 2020 AND YEAR <= 2099),
    CONSTRAINT CHK_STATUS CHECK (STATUS IN ('PENDING', 'PROCESSED', 'ERROR', 'VALIDATED'))
);

-- Create indexes for better query performance
CREATE INDEX IDX_ccBulk_DUR_MEMBER ON ccBulk_DUR_QTR_LOAD (MEMBER_ID);
CREATE INDEX IDX_ccBulk_DUR_QUARTER_YEAR ON ccBulk_DUR_QTR_LOAD (QUARTER, YEAR);
CREATE INDEX IDX_ccBulk_DUR_BATCH ON ccBulk_DUR_QTR_LOAD (BATCH_ID);
CREATE INDEX IDX_ccBulk_DUR_SERVICE_DATE ON ccBulk_DUR_QTR_LOAD (SERVICE_DATE);
CREATE INDEX IDX_ccBulk_DUR_STATUS ON ccBulk_DUR_QTR_LOAD (STATUS);
CREATE INDEX IDX_ccBulk_DUR_CREATED_DATE ON ccBulk_DUR_QTR_LOAD (CREATED_DATE);

-- Create a sequence for generating IDs (optional - you can also use application-generated IDs)
CREATE SEQUENCE SEQ_ccBulk_DUR_QTR_LOAD
    START WITH 1
    INCREMENT BY 1
    NOMAXVALUE
    NOCYCLE
    CACHE 1000;

-- Grant permissions (adjust as needed for your environment)
-- GRANT SELECT, INSERT, UPDATE, DELETE ON ccBulk_DUR_QTR_LOAD TO your_application_user;
-- GRANT SELECT ON SEQ_ccBulk_DUR_QTR_LOAD TO your_application_user;

-- Comments for documentation
COMMENT ON TABLE ccBulk_DUR_QTR_LOAD IS 'Table for storing DUR (Drug Utilization Review) quarterly load data';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.ID IS 'Unique identifier for each record';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.MEMBER_ID IS 'Member identifier for the DUR record';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.PRESCRIPTION_NUMBER IS 'Prescription number or identifier';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.NDC IS 'National Drug Code (11 characters)';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.SERVICE_DATE IS 'Date when the service was provided';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.PROVIDER_ID IS 'Prescribing provider identifier';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.PHARMACY_ID IS 'Pharmacy identifier where prescription was filled';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.DRUG_NAME IS 'Name of the prescribed drug';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.DRUG_STRENGTH IS 'Strength/dosage of the drug';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.QUANTITY IS 'Quantity of drug dispensed';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.DAYS_SUPPLY IS 'Number of days the prescription should last';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.PAID_AMOUNT IS 'Amount paid for the prescription';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.DUR_ALERT_CODE IS 'DUR alert code if any alerts were triggered';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.DUR_ALERT_DESCRIPTION IS 'Description of the DUR alert';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.QUARTER IS 'Quarter of the year (Q1, Q2, Q3, Q4)';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.YEAR IS 'Year of the quarterly load';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.BATCH_ID IS 'Batch identifier for grouping related records';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.CREATED_DATE IS 'Date and time when the record was created';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.UPDATED_DATE IS 'Date and time when the record was last updated';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.STATUS IS 'Processing status of the record';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.ERROR_MESSAGE IS 'Error message if processing failed';
COMMENT ON COLUMN ccBulk_DUR_QTR_LOAD.ADDITIONAL_DATA IS 'Additional data in JSON or XML format for flexibility';

-- Optional: Create a view for easy querying of current quarter data
CREATE OR REPLACE VIEW V_CURRENT_QUARTER_DUR AS
SELECT *
FROM ccBulk_DUR_QTR_LOAD
WHERE QUARTER = 'Q' || CEIL(EXTRACT(MONTH FROM SYSDATE) / 3)
  AND YEAR = EXTRACT(YEAR FROM SYSDATE);

COMMENT ON VIEW V_CURRENT_QUARTER_DUR IS 'View showing DUR records for the current quarter';

-- Example queries for testing:
/*
-- Count records by quarter and year
SELECT QUARTER, YEAR, COUNT(*) as RECORD_COUNT
FROM ccBulk_DUR_QTR_LOAD
GROUP BY QUARTER, YEAR
ORDER BY YEAR DESC, QUARTER DESC;

-- Count records by status
SELECT STATUS, COUNT(*) as RECORD_COUNT
FROM ccBulk_DUR_QTR_LOAD
GROUP BY STATUS;

-- Count records by batch
SELECT BATCH_ID, COUNT(*) as RECORD_COUNT, MIN(CREATED_DATE) as FIRST_RECORD, MAX(CREATED_DATE) as LAST_RECORD
FROM ccBulk_DUR_QTR_LOAD
GROUP BY BATCH_ID
ORDER BY FIRST_RECORD DESC;
*/