-- Fixed version of sel_portfoy with correct calculations
-- This addresses issues with kar-zarar (P&L) and maliyet (cost basis) calculations

CREATE PROCEDURE [dbo].[sel_portfoy_fixed]
	@hisseAdi varchar(20) = NULL
AS

SELECT
    a.HisseAdi,
    a.realizedKar as [Gerceklesen Kar],  -- Realized profit from closed positions
    a.aktiflot as [Acik Lot],             -- Open lots
    a.piyasasatis as [Piyasa Satis],      -- Current market sell price

    -- Average cost of OPEN positions only
    a.aktifHarcanan / a.aktiflot as [Acik Pozisyon Maliyet],

    -- Average cost of ALL positions (for reference)
    a.toplamHarcanan / a.totallot as [Toplam Ortalama Maliyet],

    -- Current market value of open positions
    (a.aktiflot * a.piyasaalis) as [Portfoy Degeri],

    -- Unrealized P&L on open positions only
    ((a.piyasasatis * a.aktiflot) - a.aktifHarcanan) as [Acik Pozisyon Kar-Zarar],

    -- Total P&L (realized + unrealized)
    (a.realizedKar + ((a.piyasasatis * a.aktiflot) - a.aktifHarcanan)) as [Toplam Kar-Zarar]

FROM (
    SELECT
        v.[HisseAdi],

        -- Realized profit from closed positions
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

ORDER BY [Toplam Kar-Zarar] DESC

/*
EXPLANATION OF CALCULATIONS:

1. [Acik Pozisyon Maliyet] = aktifHarcanan / aktiflot
   - Average cost per lot for currently OPEN positions

2. [Acik Pozisyon Kar-Zarar] = (piyasasatis * aktiflot) - aktifHarcanan
   - UNREALIZED profit/loss on open positions
   - Formula: (Current market value) - (Cost of open positions)

3. [Toplam Kar-Zarar] = realizedKar + unrealizedKar
   - TOTAL profit/loss including both closed and open positions
   - Formula: (Closed position profits) + (Open position unrealized P&L)

EXAMPLE:
- Bought 100 lots at 10.00 TL = 1000 TL spent
- Sold 30 lots at 12.00 TL = 360 TL received, profit = 60 TL (kar field)
- Still holding 70 lots (bought at 10.00 TL average)
- Current market price: 11.00 TL

aktifHarcanan = 70 * 10.00 = 700 TL
aktiflot = 70
realizedKar = 60 TL (from the 30 lots sold)

[Acik Pozisyon Maliyet] = 700 / 70 = 10.00 TL
[Acik Pozisyon Kar-Zarar] = (11.00 * 70) - 700 = 770 - 700 = 70 TL
[Toplam Kar-Zarar] = 60 + 70 = 130 TL
*/
