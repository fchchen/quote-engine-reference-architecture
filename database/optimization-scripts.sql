-- ============================================================================
-- Quote Engine SQL Optimization Scripts
-- Common interview questions and performance tuning examples
-- ============================================================================

USE QuoteEngine;
GO

-- ============================================================================
-- INTERVIEW QUESTION: Finding and Deleting Duplicate Records
-- This is a VERY common interview question!
-- ============================================================================

-- Method 1: Find duplicates using GROUP BY and HAVING
-- "Find all businesses that have the same BusinessName and TaxId"
PRINT 'Finding duplicates using GROUP BY...';

SELECT
    BusinessName,
    TaxId,
    COUNT(*) AS DuplicateCount
FROM Businesses
GROUP BY BusinessName, TaxId
HAVING COUNT(*) > 1;

-- Method 2: Find duplicates with all columns using ROW_NUMBER()
-- This is often the preferred approach as it shows which records are duplicates
PRINT 'Finding duplicates using ROW_NUMBER()...';

WITH DuplicateCTE AS (
    SELECT
        Id,
        BusinessName,
        TaxId,
        ROW_NUMBER() OVER (
            PARTITION BY BusinessName, TaxId
            ORDER BY CreatedDate DESC  -- Keep the most recent
        ) AS RowNum
    FROM Businesses
)
SELECT * FROM DuplicateCTE WHERE RowNum > 1;

-- Method 3: Delete duplicates keeping the most recent record
-- INTERVIEW TIP: Always show how to review before deleting!
PRINT 'Delete duplicates (keeping most recent)...';

-- First, preview what will be deleted
WITH DuplicateCTE AS (
    SELECT
        Id,
        BusinessName,
        TaxId,
        ROW_NUMBER() OVER (
            PARTITION BY BusinessName, TaxId
            ORDER BY CreatedDate DESC
        ) AS RowNum
    FROM Businesses
)
SELECT * FROM DuplicateCTE WHERE RowNum > 1;

-- Then delete (commented out for safety)
/*
WITH DuplicateCTE AS (
    SELECT
        Id,
        ROW_NUMBER() OVER (
            PARTITION BY BusinessName, TaxId
            ORDER BY CreatedDate DESC
        ) AS RowNum
    FROM Businesses
)
DELETE FROM DuplicateCTE WHERE RowNum > 1;
*/

GO

-- ============================================================================
-- INDEX ANALYSIS AND RECOMMENDATIONS
-- ============================================================================

-- Find missing indexes (SQL Server recommendation)
PRINT 'Checking for missing indexes...';

SELECT
    OBJECT_NAME(mid.object_id) AS TableName,
    migs.avg_total_user_cost * (migs.avg_user_impact / 100.0) *
        (migs.user_seeks + migs.user_scans) AS improvement_measure,
    'CREATE INDEX IX_' + OBJECT_NAME(mid.object_id) + '_' +
        REPLACE(REPLACE(REPLACE(
            ISNULL(mid.equality_columns,'') +
            CASE WHEN mid.inequality_columns IS NOT NULL
                 THEN ',' + mid.inequality_columns ELSE '' END,
        '[',''),']',''),' ','') +
        ' ON ' + mid.[statement] + ' (' + ISNULL(mid.equality_columns,'') +
        CASE WHEN mid.inequality_columns IS NOT NULL
             THEN ',' + mid.inequality_columns ELSE '' END + ')' +
        CASE WHEN mid.included_columns IS NOT NULL
             THEN ' INCLUDE (' + mid.included_columns + ')' ELSE '' END
    AS create_index_statement
FROM sys.dm_db_missing_index_groups mig
INNER JOIN sys.dm_db_missing_index_group_stats migs
    ON migs.group_handle = mig.index_group_handle
INNER JOIN sys.dm_db_missing_index_details mid
    ON mig.index_handle = mid.index_handle
WHERE database_id = DB_ID()
ORDER BY improvement_measure DESC;

GO

-- ============================================================================
-- QUERY PERFORMANCE ANALYSIS
-- ============================================================================

-- Get rate for a quote (common query pattern)
-- INTERVIEW TIP: Explain how the index supports this query
PRINT 'Rate lookup query with execution plan analysis...';

SET STATISTICS IO ON;
SET STATISTICS TIME ON;

SELECT
    BaseRate,
    MinPremium,
    StateTaxRate
FROM RateTables
WHERE StateCode = 'CA'
  AND ClassificationCode = '8810'
  AND ProductType = 1
  AND IsActive = 1
  AND EffectiveDate <= GETDATE()
  AND (ExpirationDate IS NULL OR ExpirationDate >= GETDATE());

SET STATISTICS IO OFF;
SET STATISTICS TIME OFF;

-- INTERVIEW QUESTION: Why is this index effective?
-- Answer: The composite index IX_RateTables_Lookup covers all WHERE clause columns
-- in order of selectivity, and includes a filter for IsActive = 1

GO

-- ============================================================================
-- CLUSTERED VS NON-CLUSTERED INDEXES
-- Common interview topic
-- ============================================================================

/*
INTERVIEW EXPLANATION:

CLUSTERED INDEX:
- Determines physical order of data in the table
- One per table (usually the primary key)
- Leaf nodes contain actual data rows
- Best for range queries and columns frequently used in ORDER BY

NON-CLUSTERED INDEX:
- Separate structure from data
- Up to 999 per table
- Leaf nodes contain index key + row locator (to clustered index or RID)
- Best for point lookups and columns in WHERE clauses

Example in this schema:
- Businesses.Id has CLUSTERED index (PK)
- IX_Businesses_TaxId is NON-CLUSTERED for quick TaxId lookups
- IX_Businesses_StateCode_BusinessType is NON-CLUSTERED with INCLUDE for filter+select
*/

-- Show index information
PRINT 'Index information for Businesses table...';

SELECT
    i.name AS IndexName,
    i.type_desc AS IndexType,
    c.name AS ColumnName,
    ic.is_included_column AS IsIncluded,
    i.is_unique AS IsUnique,
    i.filter_definition AS FilterDefinition
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
INNER JOIN sys.columns c ON ic.object_id = c.object_id AND ic.column_id = c.column_id
WHERE i.object_id = OBJECT_ID('Businesses')
ORDER BY i.name, ic.key_ordinal;

GO

-- ============================================================================
-- QUERY TUNING EXAMPLES
-- ============================================================================

-- BEFORE: Query without proper indexing (would do table scan)
-- SELECT * FROM Quotes WHERE BusinessId = 123 AND Status = 2

-- AFTER: Query using covering index
-- The IX_Quotes_BusinessId_ProductType_Status index makes this efficient
PRINT 'Efficient quote lookup...';

SELECT
    QuoteNumber,
    ProductType,
    AnnualPremium,
    QuoteDate
FROM Quotes
WHERE BusinessId = 1
  AND Status = 2
ORDER BY QuoteDate DESC;

GO

-- ============================================================================
-- COMMON TABLE EXPRESSIONS (CTE) FOR COMPLEX QUERIES
-- ============================================================================

-- Example: Get businesses with their total premium across all quoted policies
PRINT 'Using CTE for aggregation...';

WITH BusinessPremiums AS (
    SELECT
        b.Id AS BusinessId,
        b.BusinessName,
        b.StateCode,
        COUNT(q.Id) AS QuoteCount,
        SUM(CASE WHEN q.Status = 2 THEN q.AnnualPremium ELSE 0 END) AS TotalQuotedPremium,
        AVG(CASE WHEN q.Status = 2 THEN q.AnnualPremium END) AS AvgPremium
    FROM Businesses b
    LEFT JOIN Quotes q ON b.Id = q.BusinessId
    GROUP BY b.Id, b.BusinessName, b.StateCode
)
SELECT
    BusinessName,
    StateCode,
    QuoteCount,
    TotalQuotedPremium,
    AvgPremium,
    CASE
        WHEN AvgPremium < 5000 THEN 'Small'
        WHEN AvgPremium < 25000 THEN 'Medium'
        ELSE 'Large'
    END AS AccountSize
FROM BusinessPremiums
WHERE QuoteCount > 0
ORDER BY TotalQuotedPremium DESC;

GO

-- ============================================================================
-- TRANSACTION HANDLING FOR QUOTE CREATION
-- INTERVIEW TIP: Show understanding of ACID properties
-- ============================================================================

PRINT 'Transaction example for quote creation...';

/*
DECLARE @QuoteId INT;
DECLARE @QuoteNumber NVARCHAR(20) = 'QT-' + FORMAT(GETDATE(), 'yyyyMMdd') + '-' + CAST(NEWID() AS NVARCHAR(5));

BEGIN TRY
    BEGIN TRANSACTION;

    -- Insert quote
    INSERT INTO Quotes (QuoteNumber, BusinessId, ProductType, StateCode, ClassificationCode,
                        CoverageLimit, Deductible, EffectiveDate, ExpirationDate,
                        BasePremium, AnnualPremium, QuoteExpirationDate)
    VALUES (@QuoteNumber, 1, 2, 'CA', '41677',
            1000000, 1000, DATEADD(day, 1, GETDATE()), DATEADD(year, 1, DATEADD(day, 1, GETDATE())),
            5500.00, 5830.00, DATEADD(day, 30, GETDATE()));

    SET @QuoteId = SCOPE_IDENTITY();

    -- Log the action
    INSERT INTO AuditLog (TableName, RecordId, Action, NewValues)
    VALUES ('Quotes', @QuoteId, 'INSERT', @QuoteNumber);

    COMMIT TRANSACTION;
    PRINT 'Quote created successfully: ' + @QuoteNumber;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    RAISERROR (@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH;
*/

GO

-- ============================================================================
-- INDEX MAINTENANCE
-- ============================================================================

-- Check index fragmentation
PRINT 'Checking index fragmentation...';

SELECT
    OBJECT_NAME(ips.object_id) AS TableName,
    i.name AS IndexName,
    ips.index_type_desc AS IndexType,
    ips.avg_fragmentation_in_percent AS Fragmentation,
    ips.page_count AS PageCount,
    CASE
        WHEN ips.avg_fragmentation_in_percent < 5 THEN 'None needed'
        WHEN ips.avg_fragmentation_in_percent < 30 THEN 'REORGANIZE'
        ELSE 'REBUILD'
    END AS RecommendedAction
FROM sys.dm_db_index_physical_stats(DB_ID(), NULL, NULL, NULL, 'LIMITED') ips
INNER JOIN sys.indexes i ON ips.object_id = i.object_id AND ips.index_id = i.index_id
WHERE ips.page_count > 100  -- Only check indexes with significant data
ORDER BY ips.avg_fragmentation_in_percent DESC;

GO

PRINT 'Optimization scripts complete.';
GO
