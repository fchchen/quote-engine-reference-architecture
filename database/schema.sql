-- ============================================================================
-- Quote Engine Database Schema
-- SQL Server Express / Azure SQL
-- ============================================================================
-- INTERVIEW TALKING POINTS:
-- - Demonstrates proper index strategy for quote lookups
-- - Shows normalization vs. denormalization trade-offs
-- - Includes audit columns for compliance
-- ============================================================================

USE master;
GO

-- Create database (for local development)
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'QuoteEngine')
BEGIN
    CREATE DATABASE QuoteEngine;
END
GO

USE QuoteEngine;
GO

-- ============================================================================
-- BUSINESSES TABLE
-- Stores business entity information
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Businesses')
BEGIN
    CREATE TABLE Businesses (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        BusinessName NVARCHAR(200) NOT NULL,
        DbaName NVARCHAR(200) NULL,
        TaxId NVARCHAR(12) NOT NULL,
        BusinessType INT NOT NULL,
        StateCode CHAR(2) NOT NULL,
        ClassificationCode NVARCHAR(10) NULL,
        Address NVARCHAR(500) NULL,
        City NVARCHAR(100) NULL,
        ZipCode NVARCHAR(10) NULL,
        Phone NVARCHAR(20) NULL,
        Email NVARCHAR(200) NULL,
        DateEstablished DATE NULL,
        EmployeeCount INT NULL,
        AnnualRevenue DECIMAL(18,2) NULL,
        AnnualPayroll DECIMAL(18,2) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME2 NULL,
        CreatedBy NVARCHAR(100) NULL,
        ModifiedBy NVARCHAR(100) NULL
    );

    -- INTERVIEW QUESTION: Why these indexes?
    -- TaxId: Unique business identifier, frequently searched
    CREATE UNIQUE NONCLUSTERED INDEX IX_Businesses_TaxId
        ON Businesses (TaxId);

    -- BusinessName: For autocomplete/search functionality
    CREATE NONCLUSTERED INDEX IX_Businesses_BusinessName
        ON Businesses (BusinessName);

    -- State + BusinessType: Common filter combination
    CREATE NONCLUSTERED INDEX IX_Businesses_StateCode_BusinessType
        ON Businesses (StateCode, BusinessType)
        INCLUDE (BusinessName, TaxId);

    -- Active businesses only (filtered index)
    CREATE NONCLUSTERED INDEX IX_Businesses_IsActive
        ON Businesses (IsActive)
        WHERE IsActive = 1;
END
GO

-- ============================================================================
-- QUOTES TABLE
-- Stores quote calculations and results
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Quotes')
BEGIN
    CREATE TABLE Quotes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        QuoteNumber NVARCHAR(20) NOT NULL,
        BusinessId INT NOT NULL,
        ProductType INT NOT NULL,
        StateCode CHAR(2) NOT NULL,
        ClassificationCode NVARCHAR(10) NULL,
        CoverageLimit DECIMAL(18,2) NOT NULL,
        Deductible DECIMAL(18,2) NOT NULL,
        EffectiveDate DATE NOT NULL,
        ExpirationDate DATE NOT NULL,
        BasePremium DECIMAL(18,2) NOT NULL,
        TotalAdjustments DECIMAL(18,2) NOT NULL DEFAULT 0,
        StateTax DECIMAL(18,2) NOT NULL DEFAULT 0,
        PolicyFee DECIMAL(18,2) NOT NULL DEFAULT 0,
        AnnualPremium DECIMAL(18,2) NOT NULL,
        RiskScore INT NOT NULL DEFAULT 50,
        RiskTier INT NOT NULL DEFAULT 2,
        Status INT NOT NULL DEFAULT 1,
        QuoteDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        QuoteExpirationDate DATETIME2 NOT NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME2 NULL,

        CONSTRAINT FK_Quotes_Business
            FOREIGN KEY (BusinessId) REFERENCES Businesses(Id)
    );

    -- Unique quote number
    CREATE UNIQUE NONCLUSTERED INDEX IX_Quotes_QuoteNumber
        ON Quotes (QuoteNumber);

    -- Business quote lookup
    CREATE NONCLUSTERED INDEX IX_Quotes_BusinessId
        ON Quotes (BusinessId)
        INCLUDE (QuoteNumber, ProductType, AnnualPremium, Status, QuoteDate);

    -- Quote search by criteria
    CREATE NONCLUSTERED INDEX IX_Quotes_BusinessId_ProductType_Status
        ON Quotes (BusinessId, ProductType, Status)
        INCLUDE (QuoteNumber, AnnualPremium, QuoteDate);

    -- Date range queries (reporting)
    CREATE NONCLUSTERED INDEX IX_Quotes_QuoteDate
        ON Quotes (QuoteDate)
        INCLUDE (QuoteNumber, ProductType, AnnualPremium, Status);
END
GO

-- ============================================================================
-- RATE TABLES
-- Stores premium rates by state, classification, and product
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'RateTables')
BEGIN
    CREATE TABLE RateTables (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        StateCode CHAR(2) NOT NULL,
        ClassificationCode NVARCHAR(10) NOT NULL,
        ProductType INT NOT NULL,
        BaseRate DECIMAL(10,4) NOT NULL,
        MinPremium DECIMAL(18,2) NOT NULL,
        StateTaxRate DECIMAL(6,4) NOT NULL,
        EffectiveDate DATE NOT NULL,
        ExpirationDate DATE NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME2 NULL
    );

    -- INTERVIEW TIP: This composite index is critical for rate lookups
    -- The query pattern is: WHERE StateCode = @state AND ClassificationCode = @class AND ProductType = @product
    CREATE UNIQUE NONCLUSTERED INDEX IX_RateTables_Lookup
        ON RateTables (StateCode, ClassificationCode, ProductType)
        WHERE IsActive = 1;

    -- Date range for rate versioning
    CREATE NONCLUSTERED INDEX IX_RateTables_EffectiveDate
        ON RateTables (EffectiveDate, ExpirationDate)
        INCLUDE (BaseRate, MinPremium, StateTaxRate);
END
GO

-- ============================================================================
-- CLASSIFICATION CODES
-- Reference table for business classifications
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ClassificationCodes')
BEGIN
    CREATE TABLE ClassificationCodes (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        Code NVARCHAR(10) NOT NULL,
        Description NVARCHAR(200) NOT NULL,
        ProductType INT NOT NULL,
        BaseRate DECIMAL(10,4) NOT NULL,
        HazardGroup NVARCHAR(5) NULL,
        IsActive BIT NOT NULL DEFAULT 1,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME2 NULL
    );

    CREATE UNIQUE NONCLUSTERED INDEX IX_ClassificationCodes_Code_ProductType
        ON ClassificationCodes (Code, ProductType);

    CREATE NONCLUSTERED INDEX IX_ClassificationCodes_ProductType
        ON ClassificationCodes (ProductType)
        WHERE IsActive = 1;
END
GO

-- ============================================================================
-- POLICIES TABLE
-- Stores bound policies from quotes
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Policies')
BEGIN
    CREATE TABLE Policies (
        Id INT IDENTITY(1,1) PRIMARY KEY,
        PolicyNumber NVARCHAR(20) NOT NULL,
        QuoteId INT NOT NULL,
        BusinessId INT NOT NULL,
        ProductType INT NOT NULL,
        StateCode CHAR(2) NOT NULL,
        CoverageLimit DECIMAL(18,2) NOT NULL,
        Deductible DECIMAL(18,2) NOT NULL,
        EffectiveDate DATE NOT NULL,
        ExpirationDate DATE NOT NULL,
        AnnualPremium DECIMAL(18,2) NOT NULL,
        Status INT NOT NULL DEFAULT 1,
        BoundDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        CancelledDate DATETIME2 NULL,
        CreatedDate DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        ModifiedDate DATETIME2 NULL,

        CONSTRAINT FK_Policies_Quote
            FOREIGN KEY (QuoteId) REFERENCES Quotes(Id),
        CONSTRAINT FK_Policies_Business
            FOREIGN KEY (BusinessId) REFERENCES Businesses(Id)
    );

    CREATE UNIQUE NONCLUSTERED INDEX IX_Policies_PolicyNumber
        ON Policies (PolicyNumber);

    CREATE NONCLUSTERED INDEX IX_Policies_BusinessId
        ON Policies (BusinessId)
        INCLUDE (PolicyNumber, ProductType, AnnualPremium, Status);

    -- Active policies lookup
    CREATE NONCLUSTERED INDEX IX_Policies_Active
        ON Policies (BusinessId, Status, ExpirationDate)
        WHERE Status = 1;
END
GO

-- ============================================================================
-- AUDIT LOG TABLE
-- Tracks changes to key tables
-- ============================================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLog')
BEGIN
    CREATE TABLE AuditLog (
        Id BIGINT IDENTITY(1,1) PRIMARY KEY,
        TableName NVARCHAR(100) NOT NULL,
        RecordId INT NOT NULL,
        Action NVARCHAR(20) NOT NULL, -- INSERT, UPDATE, DELETE
        OldValues NVARCHAR(MAX) NULL,
        NewValues NVARCHAR(MAX) NULL,
        UserId NVARCHAR(100) NULL,
        Timestamp DATETIME2 NOT NULL DEFAULT GETUTCDATE()
    );

    CREATE NONCLUSTERED INDEX IX_AuditLog_Table_Record
        ON AuditLog (TableName, RecordId)
        INCLUDE (Action, Timestamp);

    CREATE NONCLUSTERED INDEX IX_AuditLog_Timestamp
        ON AuditLog (Timestamp);
END
GO

PRINT 'Schema creation complete.';
GO
