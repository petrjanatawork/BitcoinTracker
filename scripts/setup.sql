-- =============================================================================
-- Bitcoin Tracker Database Setup Script (T-SQL)
-- 
-- This script creates the BitcoinTracker database and BitcoinRates table
-- to support the Bitcoin Tracker application.
--
-- Usage:
--   1. Connect to your SQL Server instance using SQL Server Management Studio,
--      Azure Data Studio, or sqlcmd.
--   2. Execute this script against the master database to create the database.
--   3. Then execute the table creation against the BitcoinTracker database.
--
-- Example with sqlcmd:
--   sqlcmd -S localhost -U sa -P YourStrong!Password -i setup.sql
-- =============================================================================

-- -----------------------------------------------------------------------------
-- Step 1: Create the BitcoinTracker database (if it doesn't exist)
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'BitcoinTracker')
BEGIN
    CREATE DATABASE BitcoinTracker;
    PRINT 'Database BitcoinTracker created successfully.';
END
ELSE
BEGIN
    PRINT 'Database BitcoinTracker already exists.';
END
GO

-- -----------------------------------------------------------------------------
-- Step 2: Use the BitcoinTracker database
-- -----------------------------------------------------------------------------
USE BitcoinTracker;
GO

-- -----------------------------------------------------------------------------
-- Step 3: Create the BitcoinRates table
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[BitcoinRates]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[BitcoinRates](
        [Id] [int] IDENTITY(1,1) NOT NULL,
        [Timestamp] [datetime2](7) NOT NULL,
        [PriceEur] [decimal](18, 2) NOT NULL,
        [PriceCzk] [decimal](18, 2) NOT NULL,
        [Note] [nvarchar](max) NULL,
        CONSTRAINT [PK_BitcoinRates] PRIMARY KEY CLUSTERED ([Id] ASC)
            WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, 
                  IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, 
                  ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
    );
    PRINT 'Table BitcoinRates created successfully.';
END
ELSE
BEGIN
    PRINT 'Table BitcoinRates already exists.';
END
GO

-- -----------------------------------------------------------------------------
-- Step 4: Create index on Timestamp for better performance on sorted queries
-- -----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_BitcoinRates_Timestamp' AND object_id = OBJECT_ID('BitcoinRates'))
BEGIN
    CREATE NONCLUSTERED INDEX [IX_BitcoinRates_Timestamp] ON [dbo].[BitcoinRates]([Timestamp] DESC);
    PRINT 'Index IX_BitcoinRates_Timestamp created successfully.';
END
ELSE
BEGIN
    PRINT 'Index IX_BitcoinRates_Timestamp already exists.';
END
GO

-- -----------------------------------------------------------------------------
-- Step 5: Insert sample data (optional - comment out if not needed)
-- -----------------------------------------------------------------------------
-- Uncomment the following block to insert sample data for testing:
/*
IF NOT EXISTS (SELECT TOP 1 1 FROM BitcoinRates)
BEGIN
    INSERT INTO [dbo].[BitcoinRates] ([Timestamp], [PriceEur], [PriceCzk], [Note])
    VALUES 
        (DATEADD(hour, -24, GETUTCDATE()), 42000.50, 980000.00, 'Initial test data'),
        (DATEADD(hour, -12, GETUTCDATE()), 42150.75, 985000.50, 'Test entry 2'),
        (DATEADD(hour, -6, GETUTCDATE()), 42300.25, 990000.75, 'Test entry 3'),
        (GETUTCDATE(), 42500.00, 995000.00, 'Latest test data');
    
    PRINT 'Sample data inserted successfully.';
END
*/
GO

PRINT 'BitcoinTracker database setup completed successfully.';
GO