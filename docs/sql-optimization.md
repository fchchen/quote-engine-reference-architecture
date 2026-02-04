# SQL Server Optimization Guide

This document covers SQL Server concepts commonly discussed in technical interviews, with practical examples from the Quote Engine database.

## Clustered vs Non-Clustered Indexes

### Understanding the Difference

| Aspect | Clustered Index | Non-Clustered Index |
|--------|-----------------|---------------------|
| Data Storage | Leaf nodes contain actual data rows | Leaf nodes contain index key + row locator |
| Per Table | Only 1 (defines physical order) | Up to 999 |
| Best For | Range queries, ORDER BY | Point lookups, WHERE clauses |
| Maintenance | More expensive on INSERT/UPDATE | Less expensive |

### Visual Representation

```
CLUSTERED INDEX (on Id):
┌─────────────────────────────────────────┐
│                 Root                     │
│            [50] [100] [150]              │
└─────────────────────────────────────────┘
        │         │         │
        ▼         ▼         ▼
┌─────────┐ ┌─────────┐ ┌─────────┐
│ 1-49    │ │ 51-99   │ │ 101-149 │
│ (Data)  │ │ (Data)  │ │ (Data)  │
└─────────┘ └─────────┘ └─────────┘

NON-CLUSTERED INDEX (on TaxId):
┌─────────────────────────────────────────┐
│                 Root                     │
│        [12-xxx] [45-xxx] [78-xxx]        │
└─────────────────────────────────────────┘
        │         │         │
        ▼         ▼         ▼
┌─────────────┐ ┌─────────────┐ ┌─────────────┐
│ 01-xxx → 3  │ │ 34-xxx → 15 │ │ 67-xxx → 8  │
│ 12-xxx → 1  │ │ 45-xxx → 4  │ │ 78-xxx → 7  │
│ 23-xxx → 2  │ │ 56-xxx → 6  │ │ 89-xxx → 9  │
│ (Ptr to CI) │ │ (Ptr to CI) │ │ (Ptr to CI) │
└─────────────┘ └─────────────┘ └─────────────┘
```

### Example from Quote Engine

```sql
-- Clustered: Primary Key (Id)
-- Physical data order matches Id order
CREATE CLUSTERED INDEX on Businesses (Id);

-- Non-Clustered: TaxId lookups
-- Separate structure for quick TaxId searches
CREATE UNIQUE NONCLUSTERED INDEX IX_Businesses_TaxId
    ON Businesses (TaxId);

-- Non-Clustered with INCLUDE columns
-- Covers the query without needing to access base table
CREATE NONCLUSTERED INDEX IX_Businesses_StateCode_BusinessType
    ON Businesses (StateCode, BusinessType)
    INCLUDE (BusinessName, TaxId);
```

## Finding and Deleting Duplicates

This is one of the most common SQL interview questions.

### Method 1: GROUP BY with HAVING

```sql
-- Find duplicates
SELECT
    BusinessName,
    TaxId,
    COUNT(*) AS DuplicateCount
FROM Businesses
GROUP BY BusinessName, TaxId
HAVING COUNT(*) > 1;
```

### Method 2: ROW_NUMBER() (Preferred)

```sql
-- Identify which records are duplicates
WITH DuplicateCTE AS (
    SELECT
        Id,
        BusinessName,
        TaxId,
        ROW_NUMBER() OVER (
            PARTITION BY BusinessName, TaxId
            ORDER BY CreatedDate DESC  -- Keep most recent
        ) AS RowNum
    FROM Businesses
)
SELECT * FROM DuplicateCTE WHERE RowNum > 1;
```

### Deleting Duplicates

```sql
-- Delete duplicates, keeping most recent
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
```

**Interview Tip**: Always mention reviewing before deleting, and using transactions for safety.

## Index Design for Common Queries

### Query Pattern Analysis

```sql
-- Query 1: Get rate for quote
SELECT BaseRate, MinPremium, StateTaxRate
FROM RateTables
WHERE StateCode = 'CA'
  AND ClassificationCode = '8810'
  AND ProductType = 1
  AND IsActive = 1;

-- Index to support this query:
CREATE NONCLUSTERED INDEX IX_RateTables_Lookup
    ON RateTables (StateCode, ClassificationCode, ProductType)
    WHERE IsActive = 1;  -- Filtered index
```

### Column Order in Composite Indexes

The order of columns in a composite index matters:

```sql
-- Index on (StateCode, BusinessType)
-- GOOD for: WHERE StateCode = 'CA'
-- GOOD for: WHERE StateCode = 'CA' AND BusinessType = 1
-- BAD for: WHERE BusinessType = 1 (can't use index)

-- Think of it like a phone book:
-- (LastName, FirstName) helps find "Smith, John"
-- But NOT for finding all "John"s
```

### INCLUDE Columns

```sql
-- Without INCLUDE: Index lookup, then Key Lookup to get data
-- With INCLUDE: All data in index, no Key Lookup needed

CREATE NONCLUSTERED INDEX IX_Quotes_BusinessId
    ON Quotes (BusinessId)
    INCLUDE (QuoteNumber, ProductType, AnnualPremium, Status);

-- Query that benefits:
SELECT QuoteNumber, ProductType, AnnualPremium, Status
FROM Quotes
WHERE BusinessId = 123;
-- ^ All columns in index, no Key Lookup!
```

## Query Execution Plans

### Reading an Execution Plan

Key operators to know:

| Operator | Meaning | Good/Bad? |
|----------|---------|-----------|
| Index Seek | Direct lookup using index | Good |
| Index Scan | Full index scan | Can be bad |
| Table Scan | Full table scan | Usually bad |
| Key Lookup | Additional lookup for non-covered columns | Can be bad |
| Hash Match | Join or aggregation | Depends |
| Nested Loops | Join method | Good for small sets |

### Example Analysis

```sql
-- Enable execution plan
SET STATISTICS IO ON;
SET STATISTICS TIME ON;

-- Run query
SELECT b.BusinessName, COUNT(q.Id) AS QuoteCount
FROM Businesses b
LEFT JOIN Quotes q ON b.Id = q.BusinessId
WHERE b.StateCode = 'CA'
GROUP BY b.BusinessName;

-- Look for:
-- 1. Seek vs Scan
-- 2. Estimated vs Actual rows
-- 3. Key Lookup (consider adding INCLUDE)
-- 4. Large Sort operations (consider index)
```

## Transaction Handling

### ACID Properties

- **Atomicity**: All or nothing
- **Consistency**: Data integrity maintained
- **Isolation**: Concurrent transactions don't interfere
- **Durability**: Committed data survives failures

### Example Transaction

```sql
BEGIN TRY
    BEGIN TRANSACTION;

    -- Insert quote
    INSERT INTO Quotes (QuoteNumber, BusinessId, ...)
    VALUES (@QuoteNumber, @BusinessId, ...);

    DECLARE @QuoteId INT = SCOPE_IDENTITY();

    -- Log the action
    INSERT INTO AuditLog (TableName, RecordId, Action)
    VALUES ('Quotes', @QuoteId, 'INSERT');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    -- Re-throw error
    THROW;
END CATCH;
```

## Common Performance Issues

### 1. Missing Indexes

```sql
-- Find missing indexes (SQL Server DMV)
SELECT
    mid.statement AS TableName,
    mid.equality_columns,
    mid.inequality_columns,
    mid.included_columns,
    migs.avg_user_impact
FROM sys.dm_db_missing_index_details mid
JOIN sys.dm_db_missing_index_groups mig ON mid.index_handle = mig.index_handle
JOIN sys.dm_db_missing_index_group_stats migs ON mig.index_group_handle = migs.group_handle
ORDER BY migs.avg_user_impact DESC;
```

### 2. Parameter Sniffing

Problem: Query plan compiled for one parameter value doesn't work well for others.

```sql
-- Potential solution: OPTIMIZE FOR
SELECT * FROM Quotes
WHERE BusinessId = @BusinessId
OPTION (OPTIMIZE FOR (@BusinessId UNKNOWN));

-- Or recompile
OPTION (RECOMPILE);
```

### 3. Implicit Conversions

```sql
-- BAD: StateCode is CHAR(2), but comparing to NVARCHAR
WHERE StateCode = N'CA'  -- Forces conversion, can't use index

-- GOOD: Match types
WHERE StateCode = 'CA'
```

## Try It Yourself

### Exercise 1: Index Analysis

Run this query and analyze the execution plan:

```sql
SELECT q.QuoteNumber, b.BusinessName, q.AnnualPremium
FROM Quotes q
JOIN Businesses b ON q.BusinessId = b.Id
WHERE q.ProductType = 2
  AND q.Status = 2
  AND q.QuoteDate >= '2024-01-01'
ORDER BY q.QuoteDate DESC;
```

1. What indexes are being used?
2. Are there any Key Lookups?
3. What index would improve this query?

### Exercise 2: Write a Duplicate-Finding Query

Given this table:
```sql
CREATE TABLE Employees (
    Id INT PRIMARY KEY,
    Email NVARCHAR(200),
    Department NVARCHAR(100),
    HireDate DATE
);
```

Write a query to find employees with duplicate emails, keeping only the earliest hired.

### Exercise 3: Design Indexes

For this query pattern:
```sql
SELECT ProductType, StateCode, SUM(AnnualPremium) AS TotalPremium
FROM Quotes
WHERE QuoteDate BETWEEN @StartDate AND @EndDate
  AND Status = 2
GROUP BY ProductType, StateCode
ORDER BY TotalPremium DESC;
```

Design the optimal index(es).
