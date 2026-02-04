-- ============================================================================
-- Quote Engine Seed Data
-- Sample data for development and testing
-- ============================================================================

USE QuoteEngine;
GO

-- ============================================================================
-- CLASSIFICATION CODES
-- ============================================================================
PRINT 'Inserting classification codes...';

-- Workers' Compensation Classifications
INSERT INTO ClassificationCodes (Code, Description, ProductType, BaseRate, HazardGroup, IsActive)
VALUES
    ('8810', 'Clerical Office Employees', 1, 0.25, 'A', 1),
    ('8742', 'Salespersons - Outside', 1, 0.35, 'A', 1),
    ('8820', 'Attorneys - All Employees', 1, 0.30, 'A', 1),
    ('8832', 'Physicians - All Employees', 1, 0.45, 'B', 1),
    ('5183', 'Plumbing - Residential', 1, 4.50, 'D', 1),
    ('5190', 'Electrical Work', 1, 3.80, 'D', 1),
    ('5403', 'Carpentry - Residential', 1, 5.20, 'D', 1),
    ('9015', 'Building Operation', 1, 2.10, 'C', 1),
    ('9586', 'Parking Lots - Attended', 1, 2.80, 'C', 1),
    ('DEFAULT', 'Default Classification', 1, 2.00, 'C', 1);

-- General Liability Classifications
INSERT INTO ClassificationCodes (Code, Description, ProductType, BaseRate, HazardGroup, IsActive)
VALUES
    ('41677', 'Restaurant - No Liquor', 2, 5.50, 'B', 1),
    ('41675', 'Restaurant - With Liquor', 2, 8.50, 'C', 1),
    ('91111', 'Retail Store', 2, 3.20, 'B', 1),
    ('41650', 'Office - Professional', 2, 2.50, 'A', 1),
    ('91302', 'Contractor - General', 2, 12.00, 'D', 1),
    ('91340', 'Manufacturer - Light', 2, 6.50, 'C', 1),
    ('91341', 'Manufacturer - Heavy', 2, 9.00, 'D', 1),
    ('DEFAULT', 'Default Classification', 2, 5.00, 'B', 1);

GO

-- ============================================================================
-- RATE TABLES
-- ============================================================================
PRINT 'Inserting rate tables...';

DECLARE @States TABLE (Code CHAR(2), TaxRate DECIMAL(6,4));
INSERT INTO @States VALUES
    ('CA', 0.0328),
    ('TX', 0.0180),
    ('NY', 0.0350),
    ('FL', 0.0150),
    ('IL', 0.0350),
    ('PA', 0.0200),
    ('OH', 0.0150),
    ('GA', 0.0250),
    ('NC', 0.0200),
    ('MI', 0.0250);

-- Workers' Compensation Rates
INSERT INTO RateTables (StateCode, ClassificationCode, ProductType, BaseRate, MinPremium, StateTaxRate, EffectiveDate, IsActive)
SELECT
    s.Code,
    c.Code,
    1, -- Workers' Comp
    CASE WHEN s.Code = 'CA' THEN c.BaseRate * 1.35 ELSE c.BaseRate END, -- CA has higher rates
    1000.00,
    s.TaxRate,
    '2024-01-01',
    1
FROM @States s
CROSS JOIN ClassificationCodes c
WHERE c.ProductType = 1;

-- General Liability Rates
INSERT INTO RateTables (StateCode, ClassificationCode, ProductType, BaseRate, MinPremium, StateTaxRate, EffectiveDate, IsActive)
SELECT
    s.Code,
    c.Code,
    2, -- General Liability
    c.BaseRate,
    500.00,
    s.TaxRate,
    '2024-01-01',
    1
FROM @States s
CROSS JOIN ClassificationCodes c
WHERE c.ProductType = 2;

-- BOP Rates (Default only)
INSERT INTO RateTables (StateCode, ClassificationCode, ProductType, BaseRate, MinPremium, StateTaxRate, EffectiveDate, IsActive)
SELECT Code, 'DEFAULT', 3, 9.00, 750.00, TaxRate, '2024-01-01', 1 FROM @States;

-- Commercial Auto Rates
INSERT INTO RateTables (StateCode, ClassificationCode, ProductType, BaseRate, MinPremium, StateTaxRate, EffectiveDate, IsActive)
SELECT Code, 'DEFAULT', 4, 1200.00, 1500.00, TaxRate, '2024-01-01', 1 FROM @States;

-- Professional Liability Rates
INSERT INTO RateTables (StateCode, ClassificationCode, ProductType, BaseRate, MinPremium, StateTaxRate, EffectiveDate, IsActive)
SELECT Code, 'DEFAULT', 5, 4.25, 1000.00, TaxRate, '2024-01-01', 1 FROM @States;

-- Cyber Liability Rates
INSERT INTO RateTables (StateCode, ClassificationCode, ProductType, BaseRate, MinPremium, StateTaxRate, EffectiveDate, IsActive)
SELECT Code, 'DEFAULT', 6, 2.50, 500.00, TaxRate, '2024-01-01', 1 FROM @States;

GO

-- ============================================================================
-- SAMPLE BUSINESSES
-- ============================================================================
PRINT 'Inserting sample businesses...';

INSERT INTO Businesses (BusinessName, DbaName, TaxId, BusinessType, StateCode, ClassificationCode, Address, City, ZipCode, Phone, Email, DateEstablished, EmployeeCount, AnnualRevenue, AnnualPayroll, IsActive)
VALUES
    ('Sunrise Bakery LLC', 'Sunrise Bakery', '12-3456789', 2, 'CA', '41677', '123 Main Street', 'Los Angeles', '90001', '(555) 123-4567', 'info@sunrisebakery.com', '2015-03-15', 12, 850000.00, 320000.00, 1),
    ('Tech Solutions Inc', 'TechSol', '23-4567890', 6, 'CA', '8810', '456 Innovation Drive', 'San Francisco', '94102', '(555) 234-5678', 'contact@techsol.com', '2018-07-20', 45, 5200000.00, 3800000.00, 1),
    ('Johnson Plumbing Services', 'Johnson Plumbing', '34-5678901', 5, 'TX', '5183', '789 Trade Center Blvd', 'Houston', '77001', '(555) 345-6789', 'service@johnsonplumbing.com', '2010-01-05', 28, 2100000.00, 1400000.00, 1),
    ('Green Leaf Landscaping', 'Green Leaf', '45-6789012', 5, 'FL', '0042', '321 Garden Way', 'Orlando', '32801', '(555) 456-7890', 'info@greenleaflandscape.com', '2012-04-10', 35, 1800000.00, 980000.00, 1),
    ('Metro Medical Associates', 'Metro Medical', '56-7890123', 7, 'NY', '8832', '555 Healthcare Plaza', 'New York', '10001', '(555) 567-8901', 'admin@metromedical.com', '2008-09-01', 85, 12500000.00, 7200000.00, 1),
    ('Quick Stop Convenience', 'Quick Stop', '67-8901234', 1, 'IL', '91111', '888 Commerce Street', 'Chicago', '60601', '(555) 678-9012', 'manager@quickstop.com', '2019-11-15', 8, 650000.00, 180000.00, 1),
    ('Anderson Law Group', 'Anderson Law', '78-9012345', 10, 'PA', '8820', '100 Legal Center Drive', 'Philadelphia', '19101', '(555) 789-0123', 'office@andersonlaw.com', '2005-06-01', 22, 4800000.00, 2900000.00, 1),
    ('Midwest Manufacturing Corp', 'Midwest Mfg', '89-0123456', 4, 'OH', '91340', '2000 Industrial Parkway', 'Cleveland', '44101', '(555) 890-1234', 'info@midwestmfg.com', '1995-02-28', 150, 18500000.00, 8500000.00, 1),
    ('Citywide Transportation Services', 'Citywide Trans', '90-1234567', 8, 'GA', '7380', '500 Freight Lane', 'Atlanta', '30301', '(555) 901-2345', 'dispatch@citywidetrans.com', '2014-08-12', 65, 6200000.00, 3100000.00, 1),
    ('Premier Real Estate Group', 'Premier RE', '01-2345678', 9, 'NC', '8741', '750 Realty Drive', 'Charlotte', '28201', '(555) 012-3456', 'contact@premierrealestate.com', '2016-05-20', 18, 2800000.00, 1100000.00, 1);

GO

PRINT 'Seed data insertion complete.';
GO
