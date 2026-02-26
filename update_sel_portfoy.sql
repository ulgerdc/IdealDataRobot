-- Script to update sel_portfoy procedure with corrected calculations
-- Date: 2025-10-13
-- Changes: Fixed maliyet calculation and added clearer P&L reporting

-- Backup the old procedure first
PRINT 'Creating backup of old procedure...'
IF OBJECT_ID('dbo.sel_portfoy_backup_20251013', 'P') IS NOT NULL
    DROP PROCEDURE dbo.sel_portfoy_backup_20251013
GO

EXEC sp_rename 'dbo.sel_portfoy', 'sel_portfoy_backup_20251013'
GO

PRINT 'Old procedure backed up as sel_portfoy_backup_20251013'
PRINT ''

-- Create the new version
PRINT 'Creating new sel_portfoy procedure...'
GO

CREATE PROCEDURE [dbo].[sel_portfoy]
	@hisseAdi varchar(20) = NULL
AS

SELECT
    a.HisseAdi,
    a.realizedKar as kar,              -- Realized profit (for backwards compatibility)
    a.aktiflot,                         -- Open lots
    a.piyasasatis,                      -- Current market sell price

    -- FIXED: Average cost of OPEN positions only (was including sold positions)
    a.aktifHarcanan / a.aktiflot as maliyet,

    -- Current market value of open positions
    (a.aktiflot * a.piyasaalis) as portfoy,

    -- Total P&L (realized + unrealized) - maintains backwards compatibility
    (a.realizedKar + ((a.piyasasatis * a.aktiflot) - a.aktifHarcanan)) as [kar-zarar]

FROM (
    SELECT
        v.[HisseAdi],

        -- Realized profit from closed positions only
        SUM(CASE WHEN aktifMi = 0 THEN kar ELSE 0 END) as realizedKar,

        -- Open lots
        SUM(Lot * aktifMi) as aktiflot,

        -- Current market prices
        AVG(h.PiyasaSatis) as piyasasatis,
        AVG(h.PiyasaAlis) as PiyasaAlis,

        -- Total money spent on all purchases
        SUM(AlisFiyati * Lot) as toplamHarcanan,

        -- Total lots (open + closed)
        SUM(Lot) as totallot,

        -- Money spent on open positions only
        SUM(AlisFiyati * Lot * aktifMi) as aktifHarcanan

    FROM [Robot].[dbo].[vHisseHareket] v
    INNER JOIN [Robot].[dbo].[Hisse] h ON v.HisseAdi = h.HisseAdi
    WHERE (@hisseAdi IS NULL OR v.HisseAdi = @hisseAdi)
    GROUP BY v.[HisseAdi]
    HAVING SUM(Lot * aktifMi) > 0  -- Only show stocks with open positions
) AS a

ORDER BY [kar-zarar] DESC

GO

PRINT ''
PRINT '=== UPDATE COMPLETE ==='
PRINT 'The sel_portfoy procedure has been updated with corrected calculations.'
PRINT ''
PRINT 'Key changes:'
PRINT '1. maliyet now shows average cost of OPEN positions only (was including sold lots)'
PRINT '2. kar-zarar calculation logic unchanged but now clearer internally'
PRINT '3. Results are backwards compatible - same column names and total P&L'
PRINT ''
PRINT 'To restore old version: EXEC sp_rename ''sel_portfoy_backup_20251013'', ''sel_portfoy'''
