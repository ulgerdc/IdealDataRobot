-- Portfoy PozisyonTipi Ayrimi Migration
-- vHisseHareket view'una PozisyonTipi eklenir
-- sel_portfoy ve sel_portfoy_cikanlar SP'lerine PozisyonTipi GROUP BY + filtre eklenir

-- 1. vHisseHareket view guncelle — PozisyonTipi ekle
ALTER VIEW [dbo].[vHisseHareket]
AS

SELECT
	Id,
	[HisseAdi],
	Lot,
	AlisFiyati,
	SatisFiyati,
	kar,
	SatisTarihi,
	AlisTarihi,
	aktifMi,
	PozisyonTipi
FROM [Robot].[dbo].[HisseHareket]

UNION ALL

SELECT
	Id,
	[HisseAdi],
	Lot,
	AlisFiyati,
	SatisFiyati,
	kar,
	SatisTarihi,
	AlisTarihi,
	aktifMi,
	PozisyonTipi
FROM [Robot].[dbo].[HisseHareketOcak24]

UNION ALL

SELECT
	Id,
	[HisseAdi],
	Lot,
	AlisHedefi AS AlisFiyati,
	(CASE
		WHEN Durum = 'KarAlindi' THEN SatisHedefi
		WHEN Durum = 'StopOldu' THEN StopHedefi
		WHEN Durum = 'Satilabilir' THEN NULL
	END) AS SatisFiyati,
	kar,
	SatisTarihi,
	AlisTarihi,
	(CASE
		WHEN Durum = 'KarAlindi' THEN 0
		WHEN Durum = 'StopOldu' THEN 0
		WHEN Durum = 'Satilabilir' THEN 1
	END) AS aktifMi,
	0 AS PozisyonTipi  -- HisseEmir hep Grid
FROM [Robot].[dbo].HisseEmir WHERE Durum <> 'Iptal'

GO

-- 2. sel_portfoy SP guncelle — PozisyonTipi GROUP BY + opsiyonel filtre
ALTER PROCEDURE [dbo].[sel_portfoy]
	@hisseAdi varchar(20) = NULL,
	@pozisyonTipi int = NULL  -- NULL=hepsi, 0=Grid, 1=Core
AS

SELECT
    a.HisseAdi,
    a.PozisyonTipi,
    a.realizedKar AS kar,
    a.aktiflot,
    a.piyasasatis,
    a.aktifHarcanan / a.aktiflot AS maliyet,
    (a.aktiflot * a.piyasaalis) AS portfoy,
    (a.realizedKar + ((a.piyasasatis * a.aktiflot) - a.aktifHarcanan)) AS [kar-zarar]
FROM (
    SELECT
        v.[HisseAdi],
        v.PozisyonTipi,
        SUM(CASE WHEN aktifMi = 0 THEN kar ELSE 0 END) AS realizedKar,
        SUM(Lot * aktifMi) AS aktiflot,
        AVG(h.PiyasaSatis) AS piyasasatis,
        AVG(h.PiyasaAlis) AS PiyasaAlis,
        SUM(AlisFiyati * Lot) AS toplamHarcanan,
        SUM(Lot) AS totallot,
        SUM(AlisFiyati * Lot * aktifMi) AS aktifHarcanan
    FROM [Robot].[dbo].[vHisseHareket] v
    INNER JOIN [Robot].[dbo].[Hisse] h ON v.HisseAdi = h.HisseAdi
    WHERE (@hisseAdi IS NULL OR v.HisseAdi = @hisseAdi)
      AND (@pozisyonTipi IS NULL OR v.PozisyonTipi = @pozisyonTipi)
    GROUP BY v.[HisseAdi], v.PozisyonTipi
    HAVING SUM(Lot * aktifMi) > 0
) AS a

ORDER BY [kar-zarar] DESC

GO

-- 3. sel_portfoy_cikanlar SP guncelle — ayni pattern
ALTER PROCEDURE [dbo].[sel_portfoy_cikanlar]
	@hisseAdi varchar(20) = NULL,
	@pozisyonTipi int = NULL  -- NULL=hepsi, 0=Grid, 1=Core
AS

SELECT
	a.HisseAdi,
	a.PozisyonTipi,
	a.kar,
	a.aktiflot,
	a.piyasasatis,
	0 AS maliyet,
	(a.aktiflot * a.piyasaalis) AS portfoy,
	0 AS [kar-zarar]
FROM (
    SELECT
		v.[HisseAdi],
		v.PozisyonTipi,
		SUM(kar) AS kar,
		SUM(Lot * aktifMi) AS aktiflot,
		AVG(h.PiyasaSatis) AS piyasasatis,
		AVG(h.PiyasaAlis) AS PiyasaAlis,
		SUM(AlisFiyati * Lot * aktifMi) AS toplamHarcanan
    FROM [Robot].[dbo].[vHisseHareket] v
	INNER JOIN [Robot].[dbo].[Hisse] h
	ON v.HisseAdi = h.HisseAdi
	WHERE (@hisseAdi IS NULL OR v.HisseAdi = @hisseAdi)
	  AND (@pozisyonTipi IS NULL OR v.PozisyonTipi = @pozisyonTipi)
	  AND aktifmi = 0
	  AND NOT EXISTS(SELECT 1 FROM [Robot].[dbo].[vHisseHareket] WHERE HisseAdi = h.HisseAdi AND aktifMi = 1)
	GROUP BY v.[HisseAdi], v.PozisyonTipi
) AS a

ORDER BY kar DESC

GO
