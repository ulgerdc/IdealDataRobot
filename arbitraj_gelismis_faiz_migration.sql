-- ============================================================
-- Gelismis Arbitraj - Faiz Bazli Adil Deger Migration
-- Tarih: 2026-02-24
-- Aciklama: YillikFaiz kolonu + SpreadLog 3 kolon + SP guncellemeleri
-- ============================================================

-- ============================================================
-- 1. ArbitrajGelismis tablosuna YillikFaiz kolonu ekle
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ArbitrajGelismis') AND name = 'YillikFaiz')
BEGIN
    ALTER TABLE ArbitrajGelismis ADD YillikFaiz float NOT NULL DEFAULT 50.0
    PRINT 'ArbitrajGelismis.YillikFaiz kolonu eklendi.'
END
ELSE
    PRINT 'ArbitrajGelismis.YillikFaiz kolonu zaten mevcut.'
GO

-- ============================================================
-- 2. ArbitrajSpreadLog tablosuna 3 kolon ekle
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ArbitrajSpreadLog') AND name = 'AdilSpreadYuzde')
BEGIN
    ALTER TABLE ArbitrajSpreadLog ADD AdilSpreadYuzde decimal(18,4) NULL
    PRINT 'ArbitrajSpreadLog.AdilSpreadYuzde kolonu eklendi.'
END
ELSE
    PRINT 'ArbitrajSpreadLog.AdilSpreadYuzde kolonu zaten mevcut.'
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ArbitrajSpreadLog') AND name = 'NetPrimYuzde')
BEGIN
    ALTER TABLE ArbitrajSpreadLog ADD NetPrimYuzde decimal(18,4) NULL
    PRINT 'ArbitrajSpreadLog.NetPrimYuzde kolonu eklendi.'
END
ELSE
    PRINT 'ArbitrajSpreadLog.NetPrimYuzde kolonu zaten mevcut.'
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ArbitrajSpreadLog') AND name = 'KalanGun')
BEGIN
    ALTER TABLE ArbitrajSpreadLog ADD KalanGun int NULL
    PRINT 'ArbitrajSpreadLog.KalanGun kolonu eklendi.'
END
ELSE
    PRINT 'ArbitrajSpreadLog.KalanGun kolonu zaten mevcut.'
GO

-- ============================================================
-- 3. SP guncelle: sel_arbitrajGelismis (+YillikFaiz)
-- ============================================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sel_arbitrajGelismis')
    DROP PROCEDURE sel_arbitrajGelismis
GO

CREATE PROCEDURE [dbo].[sel_arbitrajGelismis]
AS
BEGIN
    SET NOCOUNT ON
    SELECT Id, HisseAdi, ArbitrajTipi, YakinVadeKodu, UzakVadeKodu,
           YakinVadeSonGun, UzakVadeSonGun, BistLot, ViopLot,
           GirisMarji, CikisMarji, AktifMi, YillikFaiz
    FROM ArbitrajGelismis
    WHERE AktifMi = 1
END
GO
PRINT 'sel_arbitrajGelismis SP guncellendi (+YillikFaiz).'
GO

-- ============================================================
-- 4. SP guncelle: ins_arbitrajSpreadLog (+3 param)
-- ============================================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'ins_arbitrajSpreadLog')
    DROP PROCEDURE ins_arbitrajSpreadLog
GO

CREATE PROCEDURE [dbo].[ins_arbitrajSpreadLog]
    @ArbitrajGelismisId bigint,
    @HisseAdi varchar(50),
    @ArbitrajTipi int,
    @Bacak1Fiyat decimal(18,2),
    @Bacak2Fiyat decimal(18,2),
    @SpreadYuzde decimal(18,4),
    @SpreadTutar decimal(18,2),
    @GirisSinyali bit,
    @CikisSinyali bit,
    @AtlanmaAciklamasi varchar(500) = NULL,
    @Bist100Yuzde decimal(8,4) = NULL,
    @Bist30Yuzde decimal(8,4) = NULL,
    @Viop30Yuzde decimal(8,4) = NULL,
    @AdilSpreadYuzde decimal(18,4) = NULL,
    @NetPrimYuzde decimal(18,4) = NULL,
    @KalanGun int = NULL
AS
BEGIN
    SET NOCOUNT ON
    INSERT INTO ArbitrajSpreadLog
        (ArbitrajGelismisId, HisseAdi, ArbitrajTipi, Bacak1Fiyat, Bacak2Fiyat,
         SpreadYuzde, SpreadTutar, GirisSinyali, CikisSinyali, AtlanmaAciklamasi,
         Bist100Yuzde, Bist30Yuzde, Viop30Yuzde,
         AdilSpreadYuzde, NetPrimYuzde, KalanGun)
    VALUES
        (@ArbitrajGelismisId, @HisseAdi, @ArbitrajTipi, @Bacak1Fiyat, @Bacak2Fiyat,
         @SpreadYuzde, @SpreadTutar, @GirisSinyali, @CikisSinyali, @AtlanmaAciklamasi,
         @Bist100Yuzde, @Bist30Yuzde, @Viop30Yuzde,
         @AdilSpreadYuzde, @NetPrimYuzde, @KalanGun)
END
GO
PRINT 'ins_arbitrajSpreadLog SP guncellendi (+AdilSpreadYuzde, NetPrimYuzde, KalanGun).'
GO

-- ============================================================
-- 5. Mevcut verileri guncelle
-- ============================================================
UPDATE ArbitrajGelismis SET YillikFaiz = 50.0
PRINT 'Tum satirlar icin YillikFaiz=50.0 set edildi.'

UPDATE ArbitrajGelismis SET YakinVadeSonGun = '2026-04-30' WHERE YakinVadeKodu = '0426'
UPDATE ArbitrajGelismis SET UzakVadeSonGun = '2026-06-30' WHERE UzakVadeKodu = '0626'
PRINT 'Vade son gun tarihleri guncellendi.'
GO

PRINT '=== Faiz migration tamamlandi ==='
GO
