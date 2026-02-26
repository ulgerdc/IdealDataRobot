-- Test script to verify portfolio calculations
-- This creates a simple test scenario and compares old vs new calculations

-- First, let's look at actual data for one stock to understand current positions
PRINT '=== Sample Stock Data ==='
SELECT TOP 1
    h.HisseAdi,
    COUNT(*) as TotalPositions,
    SUM(CASE WHEN h.AktifMi = 1 THEN 1 ELSE 0 END) as OpenPositions,
    SUM(CASE WHEN h.AktifMi = 0 THEN 1 ELSE 0 END) as ClosedPositions
FROM HisseHareket h
GROUP BY h.HisseAdi
HAVING SUM(CASE WHEN h.AktifMi = 1 THEN 1 ELSE 0 END) > 0
ORDER BY COUNT(*) DESC

-- Get a sample stock to test with
DECLARE @testHisse varchar(20)
SELECT TOP 1 @testHisse = h.HisseAdi
FROM HisseHareket h
GROUP BY h.HisseAdi
HAVING SUM(CASE WHEN h.AktifMi = 1 THEN 1 ELSE 0 END) > 0
ORDER BY COUNT(*) DESC

PRINT ''
PRINT '=== Testing with stock: ' + @testHisse + ' ==='
PRINT ''

-- Show raw data for this stock
PRINT '--- Raw Position Data ---'
SELECT
    Id,
    HisseAdi,
    Lot,
    AlisFiyati,
    SatisFiyati,
    AktifMi,
    Kar,
    AlisTarihi,
    SatisTarihi
FROM HisseHareket
WHERE HisseAdi = @testHisse
ORDER BY AlisTarihi

-- Calculate manually what we expect
PRINT ''
PRINT '--- Manual Calculations ---'
SELECT
    @testHisse as HisseAdi,
    SUM(Lot * AktifMi) as OpenLots,
    SUM(AlisFiyati * Lot * AktifMi) as TotalSpentOnOpen,
    SUM(AlisFiyati * Lot * AktifMi) / NULLIF(SUM(Lot * AktifMi), 0) as AvgCostOfOpen,
    SUM(CASE WHEN AktifMi = 0 THEN Kar ELSE 0 END) as RealizedProfit,
    (SELECT AVG(PiyasaSatis) FROM Hisse WHERE HisseAdi = @testHisse) as CurrentMarketPrice
FROM HisseHareket
WHERE HisseAdi = @testHisse

-- Now let's calculate expected unrealized P&L
PRINT ''
PRINT '--- Expected Unrealized P&L ---'
SELECT
    @testHisse as HisseAdi,
    (
        (SELECT AVG(PiyasaSatis) FROM Hisse WHERE HisseAdi = @testHisse) *
        SUM(Lot * AktifMi)
    ) - SUM(AlisFiyati * Lot * AktifMi) as ExpectedUnrealizedPL
FROM HisseHareket
WHERE HisseAdi = @testHisse

-- Run OLD procedure
PRINT ''
PRINT '=== OLD PROCEDURE RESULTS (sel_portfoy) ==='
EXEC sel_portfoy @hisseAdi = @testHisse

-- Run NEW procedure
PRINT ''
PRINT '=== NEW PROCEDURE RESULTS (sel_portfoy_fixed) ==='
EXEC sel_portfoy_fixed @hisseAdi = @testHisse

-- Side-by-side comparison
PRINT ''
PRINT '=== SIDE-BY-SIDE COMPARISON ==='
SELECT
    'OLD' as Version,
    a.HisseAdi,
    a.kar as KarField,
    a.aktiflot as OpenLots,
    a.toplamHarcanan / a.totallot as Maliyet_Old,
    (a.piyasasatis - ((a.aktifHarcanan - a.kar) / a.aktiflot)) * a.aktiflot as KarZarar_Old
FROM (
    SELECT
        v.[HisseAdi],
        SUM(kar) kar,
        SUM(Lot * aktifMi) aktiflot,
        AVG(h.PiyasaSatis) piyasasatis,
        AVG(h.PiyasaAlis) PiyasaAlis,
        SUM(AlisFiyati * Lot) toplamHarcanan,
        SUM(Lot) totallot,
        SUM(AlisFiyati * Lot * aktifMi) aktifHarcanan
    FROM [Robot].[dbo].[vHisseHareket] v
    INNER JOIN [Robot].[dbo].[Hisse] h ON v.HisseAdi = h.HisseAdi
    WHERE v.HisseAdi = @testHisse
    GROUP BY v.[HisseAdi]
    HAVING SUM(Lot * aktifMi) > 0
) AS a

UNION ALL

SELECT
    'NEW' as Version,
    a.HisseAdi,
    a.realizedKar as KarField,
    a.aktiflot as OpenLots,
    a.aktifHarcanan / a.aktiflot as Maliyet_New,
    (a.realizedKar + ((a.piyasasatis * a.aktiflot) - a.aktifHarcanan)) as KarZarar_New
FROM (
    SELECT
        v.[HisseAdi],
        SUM(CASE WHEN aktifMi = 0 THEN kar ELSE 0 END) as realizedKar,
        SUM(Lot * aktifMi) as aktiflot,
        AVG(h.PiyasaSatis) as piyasasatis,
        AVG(h.PiyasaAlis) as PiyasaAlis,
        SUM(AlisFiyati * Lot) as toplamHarcanan,
        SUM(Lot) as totallot,
        SUM(AlisFiyati * Lot * aktifMi) as aktifHarcanan
    FROM [Robot].[dbo].[vHisseHareket] v
    INNER JOIN [Robot].[dbo].[Hisse] h ON v.HisseAdi = h.HisseAdi
    WHERE v.HisseAdi = @testHisse
    GROUP BY v.[HisseAdi]
    HAVING SUM(Lot * aktifMi) > 0
) AS a

PRINT ''
PRINT '=== TEST COMPLETE ==='
