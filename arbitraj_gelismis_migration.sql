-- ============================================================
-- Gelismis Arbitraj Strateji - Veritabani Migration
-- Tarih: 2026-02-24
-- Aciklama: 3 tablo + 4 SP + ornek veri
-- ============================================================

-- ============================================================
-- TABLO 1: ArbitrajGelismis (Config)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ArbitrajGelismis')
BEGIN
    CREATE TABLE [dbo].[ArbitrajGelismis] (
        Id bigint IDENTITY(1,1) PRIMARY KEY,
        HisseAdi varchar(50) NOT NULL,
        ArbitrajTipi int NOT NULL DEFAULT 0,           -- 0=Spot-VIOP, 1=Takvim Spread
        YakinVadeKodu varchar(20) NOT NULL,             -- '0426' (MMYY)
        UzakVadeKodu varchar(20) NULL,                  -- Takvim spread icin: '0626'
        YakinVadeSonGun smalldatetime NULL,
        UzakVadeSonGun smalldatetime NULL,
        BistLot int NOT NULL DEFAULT 0,
        ViopLot int NOT NULL DEFAULT 1,
        GirisMarji float NOT NULL DEFAULT 2.0,          -- Spread >= bu ise firsat
        CikisMarji float NOT NULL DEFAULT 0.5,          -- Spread <= bu ise kapat
        AktifMi bit NOT NULL DEFAULT 1,
        Tarih smalldatetime NOT NULL DEFAULT GETDATE()
    )
    PRINT 'ArbitrajGelismis tablosu olusturuldu.'
END
ELSE
    PRINT 'ArbitrajGelismis tablosu zaten mevcut.'
GO

-- ============================================================
-- TABLO 2: ArbitrajSpreadLog (Izleme verisi)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ArbitrajSpreadLog')
BEGIN
    CREATE TABLE [dbo].[ArbitrajSpreadLog] (
        Id bigint IDENTITY(1,1) PRIMARY KEY,
        ArbitrajGelismisId bigint NOT NULL,
        HisseAdi varchar(50) NOT NULL,
        ArbitrajTipi int NOT NULL,
        Bacak1Fiyat decimal(18,2) NOT NULL,             -- BIST veya yakin vade VIOP
        Bacak2Fiyat decimal(18,2) NOT NULL,             -- VIOP yakin veya uzak vade
        SpreadYuzde decimal(18,4) NOT NULL,
        SpreadTutar decimal(18,2) NOT NULL,
        GirisSinyali bit NOT NULL DEFAULT 0,
        CikisSinyali bit NOT NULL DEFAULT 0,
        AtlanmaAciklamasi varchar(500) NULL,
        Bist100Yuzde decimal(8,4) NULL,
        Bist30Yuzde decimal(8,4) NULL,
        Viop30Yuzde decimal(8,4) NULL,
        Tarih datetime NOT NULL DEFAULT GETDATE()
    )
    PRINT 'ArbitrajSpreadLog tablosu olusturuldu.'
END
ELSE
    PRINT 'ArbitrajSpreadLog tablosu zaten mevcut.'
GO

-- ============================================================
-- TABLO 3: ArbitrajGelismisHareket (Ileri faz icin hazirlik)
-- ============================================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ArbitrajGelismisHareket')
BEGIN
    CREATE TABLE [dbo].[ArbitrajGelismisHareket] (
        Id bigint IDENTITY(1,1) PRIMARY KEY,
        ArbitrajGelismisId bigint NOT NULL,
        RobotAdi varchar(50) NOT NULL,
        HisseAdi varchar(50) NOT NULL,
        ArbitrajTipi int NOT NULL,
        Bacak1Sembol varchar(30) NOT NULL,
        Bacak1Yon varchar(5) NOT NULL,                  -- 'ALIS' / 'SATIS'
        Bacak1GirisFiyat decimal(18,2) NOT NULL,
        Bacak1CikisFiyat decimal(18,2) NULL,
        Bacak1Lot int NOT NULL,
        Bacak2Sembol varchar(30) NOT NULL,
        Bacak2Yon varchar(5) NOT NULL,
        Bacak2GirisFiyat decimal(18,2) NOT NULL,
        Bacak2CikisFiyat decimal(18,2) NULL,
        Bacak2Lot int NOT NULL,
        GirisSpreadYuzde decimal(18,4) NOT NULL,
        CikisSpreadYuzde decimal(18,4) NULL,
        Kar decimal(18,2) NULL,
        AktifMi bit NOT NULL DEFAULT 1,
        PozisyonTarihi datetime NOT NULL DEFAULT GETDATE(),
        KapanisTarihi datetime NULL
    )
    PRINT 'ArbitrajGelismisHareket tablosu olusturuldu.'
END
ELSE
    PRINT 'ArbitrajGelismisHareket tablosu zaten mevcut.'
GO

-- ============================================================
-- SP 1: sel_arbitrajGelismis - Aktif config listesi
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
           GirisMarji, CikisMarji, AktifMi
    FROM ArbitrajGelismis
    WHERE AktifMi = 1
END
GO
PRINT 'sel_arbitrajGelismis SP olusturuldu.'
GO

-- ============================================================
-- SP 2: ins_arbitrajSpreadLog - Spread log kaydet
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
    @Viop30Yuzde decimal(8,4) = NULL
AS
BEGIN
    SET NOCOUNT ON
    INSERT INTO ArbitrajSpreadLog
        (ArbitrajGelismisId, HisseAdi, ArbitrajTipi, Bacak1Fiyat, Bacak2Fiyat,
         SpreadYuzde, SpreadTutar, GirisSinyali, CikisSinyali, AtlanmaAciklamasi,
         Bist100Yuzde, Bist30Yuzde, Viop30Yuzde)
    VALUES
        (@ArbitrajGelismisId, @HisseAdi, @ArbitrajTipi, @Bacak1Fiyat, @Bacak2Fiyat,
         @SpreadYuzde, @SpreadTutar, @GirisSinyali, @CikisSinyali, @AtlanmaAciklamasi,
         @Bist100Yuzde, @Bist30Yuzde, @Viop30Yuzde)
END
GO
PRINT 'ins_arbitrajSpreadLog SP olusturuldu.'
GO

-- ============================================================
-- SP 3: sel_arbitrajGelismisKontrol - Aktif pozisyon kontrol
-- ============================================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sel_arbitrajGelismisKontrol')
    DROP PROCEDURE sel_arbitrajGelismisKontrol
GO

CREATE PROCEDURE [dbo].[sel_arbitrajGelismisKontrol]
    @HisseAdi varchar(50),
    @ArbitrajTipi int
AS
BEGIN
    SET NOCOUNT ON
    SELECT TOP 1 Id, ArbitrajGelismisId, RobotAdi, HisseAdi, ArbitrajTipi,
           Bacak1Sembol, Bacak1Yon, Bacak1GirisFiyat, Bacak1CikisFiyat, Bacak1Lot,
           Bacak2Sembol, Bacak2Yon, Bacak2GirisFiyat, Bacak2CikisFiyat, Bacak2Lot,
           GirisSpreadYuzde, CikisSpreadYuzde, Kar, AktifMi,
           PozisyonTarihi, KapanisTarihi
    FROM ArbitrajGelismisHareket
    WHERE HisseAdi = @HisseAdi
      AND ArbitrajTipi = @ArbitrajTipi
      AND AktifMi = 1
    ORDER BY PozisyonTarihi DESC
END
GO
PRINT 'sel_arbitrajGelismisKontrol SP olusturuldu.'
GO

-- ============================================================
-- SP 4: sel_arbitrajSonSpread - Son spread getir (tekrar onleme)
-- ============================================================
IF EXISTS (SELECT * FROM sys.procedures WHERE name = 'sel_arbitrajSonSpread')
    DROP PROCEDURE sel_arbitrajSonSpread
GO

CREATE PROCEDURE [dbo].[sel_arbitrajSonSpread]
    @ArbitrajGelismisId bigint
AS
BEGIN
    SET NOCOUNT ON
    SELECT TOP 1 SpreadYuzde, Tarih
    FROM ArbitrajSpreadLog
    WHERE ArbitrajGelismisId = @ArbitrajGelismisId
    ORDER BY Tarih DESC
END
GO
PRINT 'sel_arbitrajSonSpread SP olusturuldu.'
GO

-- ============================================================
-- ORNEK VERI
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM ArbitrajGelismis WHERE HisseAdi = 'ASELS' AND ArbitrajTipi = 0)
BEGIN
    INSERT INTO ArbitrajGelismis (HisseAdi, ArbitrajTipi, YakinVadeKodu, UzakVadeKodu, BistLot, ViopLot, GirisMarji, CikisMarji)
    VALUES
        ('ASELS', 0, '0426', NULL, 100, 1, 2.0, 0.5),    -- Spot-VIOP
        ('THYAO', 0, '0426', NULL, 100, 1, 1.5, 0.3),    -- Spot-VIOP
        ('ASELS', 1, '0426', '0626', 0, 1, 1.0, 0.2)     -- Takvim Spread
    PRINT 'Ornek veriler eklendi.'
END
ELSE
    PRINT 'Ornek veriler zaten mevcut.'
GO

PRINT '=== Migration tamamlandi ==='
GO
