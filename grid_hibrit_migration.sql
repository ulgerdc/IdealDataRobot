USE [Robot]
GO

-- =====================================================
-- Grid Hibrit Strateji Migration
-- Tarih: 2026-02-22
-- Aciklama: Grid + Core + TimeDecay + BudgetLimit
-- GERIYE UYUMLU: Mevcut KademeStrateji etkilenmez
-- =====================================================

-- =====================================================
-- FAZ 1: TABLO DEGISIKLIKLERI
-- =====================================================

-- HisseHareket tablosu: PozisyonTipi ve TepeNoktasi ekle
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HisseHareket') AND name = 'PozisyonTipi')
BEGIN
    ALTER TABLE [dbo].[HisseHareket] ADD PozisyonTipi int NOT NULL DEFAULT 0
    PRINT 'HisseHareket.PozisyonTipi eklendi'
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HisseHareket') AND name = 'TepeNoktasi')
BEGIN
    ALTER TABLE [dbo].[HisseHareket] ADD TepeNoktasi decimal(18,2) NULL
    PRINT 'HisseHareket.TepeNoktasi eklendi'
END
GO

-- HisseHareketOcak24 (arsiv) tablosu: ayni kolonlar
IF OBJECT_ID('HisseHareketOcak24') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HisseHareketOcak24') AND name = 'PozisyonTipi')
    BEGIN
        ALTER TABLE [dbo].[HisseHareketOcak24] ADD PozisyonTipi int NOT NULL DEFAULT 0
        PRINT 'HisseHareketOcak24.PozisyonTipi eklendi'
    END

    IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HisseHareketOcak24') AND name = 'TepeNoktasi')
    BEGIN
        ALTER TABLE [dbo].[HisseHareketOcak24] ADD TepeNoktasi decimal(18,2) NULL
        PRINT 'HisseHareketOcak24.TepeNoktasi eklendi'
    END
END
GO

-- Hisse tablosu: Core ve Budget parametreleri
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Hisse') AND name = 'CoreOran')
BEGIN
    ALTER TABLE [dbo].[Hisse] ADD CoreOran int DEFAULT 30
    PRINT 'Hisse.CoreOran eklendi'
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Hisse') AND name = 'CoreMarj')
BEGIN
    ALTER TABLE [dbo].[Hisse] ADD CoreMarj decimal(5,2) DEFAULT 100
    PRINT 'Hisse.CoreMarj eklendi'
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Hisse') AND name = 'TrailingStopYuzde')
BEGIN
    ALTER TABLE [dbo].[Hisse] ADD TrailingStopYuzde decimal(5,2) DEFAULT 5.0
    PRINT 'Hisse.TrailingStopYuzde eklendi'
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Hisse') AND name = 'ButceLimitYuzde')
BEGIN
    ALTER TABLE [dbo].[Hisse] ADD ButceLimitYuzde decimal(5,2) DEFAULT 60.0
    PRINT 'Hisse.ButceLimitYuzde eklendi'
END
GO

-- Default satirini guncelle (yeni kolonlar icin degerler ata)
UPDATE [dbo].[Hisse]
SET CoreOran = ISNULL(CoreOran, 30),
    CoreMarj = ISNULL(CoreMarj, 100),
    TrailingStopYuzde = ISNULL(TrailingStopYuzde, 5.0),
    ButceLimitYuzde = ISNULL(ButceLimitYuzde, 60.0)
WHERE HisseAdi = 'Default'
GO

-- =====================================================
-- FAZ 2: STORED PROCEDURE DEGISIKLIKLERI
-- =====================================================

-- ins_HisseHareket: PozisyonTipi parametresi eklendi
IF OBJECT_ID('[dbo].[ins_HisseHareket]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[ins_HisseHareket]
GO

CREATE PROC [dbo].[ins_HisseHareket]
    @Id bigint null,
    @HisseAdi varchar(50),
    @Lot int,
    @AlisFiyati decimal(18,2),
    @SatisFiyati decimal(18,2),
    @RobotAdi varchar(50),
    @PozisyonTipi int = 0
AS
BEGIN

    DECLARE @islemTarihi smalldatetime = GETDATE()
    DECLARE @tarih date = CAST(GETDATE() AS Date)

    SELECT GETDATE()

    IF (@Id = 0)
    BEGIN
        INSERT INTO [dbo].[HisseHareket]
            ([HisseAdi]
            ,[RobotAdi]
            ,[Lot]
            ,[AlisFiyati]
            ,AktifMi
            ,[AlisTarihi]
            ,[Tarih]
            ,[PozisyonTipi])
        VALUES
            (@HisseAdi,
             @RobotAdi,
             @Lot,
             @AlisFiyati,
             1,
             GETDATE(),
             @tarih,
             @PozisyonTipi)
    END
    ELSE
    BEGIN
        UPDATE [dbo].[HisseHareket]
        SET SatisFiyati = @SatisFiyati,
            AktifMi = 0,
            [Kar] = Lot * (@SatisFiyati - AlisFiyati),
            [SatisTarihi] = GETDATE()
        WHERE Id = @Id
    END

END
GO

-- sel_hisseSatimKontrol: PozisyonTipi filtresi eklendi
IF OBJECT_ID('[dbo].[sel_hisseSatimKontrol]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_hisseSatimKontrol]
GO

CREATE PROC [dbo].[sel_hisseSatimKontrol]
    @HisseAdi varchar(50),
    @SatisFiyati decimal(18,2),
    @Marj decimal(18,2),
    @PozisyonTipi int = 0
AS
BEGIN

    SELECT * FROM HisseHareket WITH(NOLOCK)
    WHERE HisseAdi = @HisseAdi
      AND AktifMi = 1
      AND PozisyonTipi = @PozisyonTipi
      AND AlisFiyati + @Marj <= @SatisFiyati

END
GO

-- sel_hisseHareket: Grid ve Core ayrimli butce kullanimi
IF OBJECT_ID('[dbo].[sel_hisseHareket]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_hisseHareket]
GO

CREATE PROC [dbo].[sel_hisseHareket]
    @HisseAdi varchar(50)
AS
BEGIN

    SELECT HisseAdi,
        SUM(Lot * AlisFiyati) ToplamAlisFiyati,
        SUM(Lot * SatisFiyati) ToplamSatisFiyati,
        SUM(CAST(AktifMi AS INT)) AcikPozisyonSayisi,
        SUM(Lot * AlisFiyati * CAST(AktifMi AS INT)) AcikPozisyonAlimTutari,
        SUM(Kar) ToplamKar,
        SUM(CASE WHEN PozisyonTipi = 0 AND AktifMi = 1 THEN Lot * AlisFiyati ELSE 0 END) GridAcikTutar,
        SUM(CASE WHEN PozisyonTipi = 1 AND AktifMi = 1 THEN Lot * AlisFiyati ELSE 0 END) CoreAcikTutar
    FROM HisseHareket WITH(NOLOCK)
    WHERE HisseAdi = @HisseAdi
    GROUP BY HisseAdi

END
GO

-- ins_arsiv: PozisyonTipi ve TepeNoktasi dahil
IF OBJECT_ID('[dbo].[ins_arsiv]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[ins_arsiv]
GO

CREATE PROC [dbo].[ins_arsiv]
AS
BEGIN

    INSERT INTO [dbo].[HisseHareketOcak24]
        (RobotAdi, HisseAdi, Lot, AlisFiyati, SatisFiyati, AktifMi, Kar, AlisTarihi, SatisTarihi, Tarih, PozisyonTipi, TepeNoktasi)
    SELECT RobotAdi, HisseAdi, Lot, AlisFiyati, SatisFiyati, AktifMi, Kar, AlisTarihi, SatisTarihi, Tarih, PozisyonTipi, TepeNoktasi
    FROM [dbo].[HisseHareket]
    WHERE AktifMi = 0

    DELETE [dbo].[HisseHareket] WHERE AktifMi = 0

END
GO

-- =====================================================
-- FAZ 3: YENI STORED PROCEDURE'LAR
-- =====================================================

-- sel_hisseSatimKontrolZamanli: Zaman bazli marj indirimi ile satilabilir pozisyonlar
IF OBJECT_ID('[dbo].[sel_hisseSatimKontrolZamanli]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_hisseSatimKontrolZamanli]
GO

CREATE PROC [dbo].[sel_hisseSatimKontrolZamanli]
    @HisseAdi varchar(50),
    @SatisFiyati decimal(18,2),
    @Marj decimal(18,2)
AS
BEGIN

    -- 30-60 gun: %75 marj, 60-90: %50, 90-120: %25, 120+: basabas
    SELECT *,
        DATEDIFF(day, AlisTarihi, GETDATE()) AS GunSayisi,
        CASE
            WHEN DATEDIFF(day, AlisTarihi, GETDATE()) >= 120 THEN 0.0
            WHEN DATEDIFF(day, AlisTarihi, GETDATE()) >= 90  THEN @Marj * 0.25
            WHEN DATEDIFF(day, AlisTarihi, GETDATE()) >= 60  THEN @Marj * 0.50
            WHEN DATEDIFF(day, AlisTarihi, GETDATE()) >= 30  THEN @Marj * 0.75
            ELSE @Marj
        END AS UygulanacakMarj
    FROM HisseHareket WITH(NOLOCK)
    WHERE HisseAdi = @HisseAdi
      AND AktifMi = 1
      AND PozisyonTipi = 0  -- Sadece Grid pozisyonlar
      AND DATEDIFF(day, AlisTarihi, GETDATE()) >= 30
      AND AlisFiyati +
          CASE
              WHEN DATEDIFF(day, AlisTarihi, GETDATE()) >= 120 THEN 0.0
              WHEN DATEDIFF(day, AlisTarihi, GETDATE()) >= 90  THEN @Marj * 0.25
              WHEN DATEDIFF(day, AlisTarihi, GETDATE()) >= 60  THEN @Marj * 0.50
              WHEN DATEDIFF(day, AlisTarihi, GETDATE()) >= 30  THEN @Marj * 0.75
              ELSE @Marj
          END <= @SatisFiyati

END
GO

-- sel_hisseSatimKontrolCore: Core hedef veya trailing stop tetiklenen pozisyonlar
IF OBJECT_ID('[dbo].[sel_hisseSatimKontrolCore]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_hisseSatimKontrolCore]
GO

CREATE PROC [dbo].[sel_hisseSatimKontrolCore]
    @HisseAdi varchar(50),
    @SatisFiyati decimal(18,2),
    @CoreMarj decimal(18,2),         -- binde cinsinden hedef (100 = %10)
    @TrailingStopYuzde decimal(5,2)   -- trailing stop yuzde (5 = %5)
AS
BEGIN

    SELECT *
    FROM HisseHareket WITH(NOLOCK)
    WHERE HisseAdi = @HisseAdi
      AND AktifMi = 1
      AND PozisyonTipi = 1  -- Sadece Core pozisyonlar
      AND (
          -- Yuksek hedef: alis + %CoreMarj
          @SatisFiyati >= AlisFiyati * (1.0 + @CoreMarj / 1000.0)
          OR
          -- Trailing stop: tepe fiyattan %TrailingStopYuzde dusus
          -- Aktivasyon: TepeNoktasi alisin en az %5 uzerinde olmali
          -- Guvenlik: asla alis fiyatinin altinda satilmaz
          (TepeNoktasi IS NOT NULL
           AND TepeNoktasi > AlisFiyati * 1.05
           AND @SatisFiyati >= AlisFiyati
           AND @SatisFiyati <= TepeNoktasi * (1.0 - @TrailingStopYuzde / 100.0))
      )

END
GO

-- upd_hisseHareketTepeNoktasi: Core pozisyonlarin TepeNoktasi guncelle
IF OBJECT_ID('[dbo].[upd_hisseHareketTepeNoktasi]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[upd_hisseHareketTepeNoktasi]
GO

CREATE PROC [dbo].[upd_hisseHareketTepeNoktasi]
    @HisseAdi varchar(50),
    @GuncelFiyat decimal(18,2)
AS
BEGIN

    UPDATE HisseHareket
    SET TepeNoktasi = @GuncelFiyat
    WHERE HisseAdi = @HisseAdi
      AND AktifMi = 1
      AND PozisyonTipi = 1  -- Sadece Core pozisyonlar
      AND (TepeNoktasi IS NULL OR TepeNoktasi < @GuncelFiyat)

END
GO

-- =====================================================
-- DOGRULAMA SORGULARI (Migration sonrasi calistirin)
-- =====================================================
/*
-- Mevcut kayitlar kontrol (hepsi PozisyonTipi=0, TepeNoktasi=NULL olmali)
SELECT TOP 5 PozisyonTipi, TepeNoktasi FROM HisseHareket

-- Default hisse ayarlari
SELECT HisseAdi, CoreOran, CoreMarj, TrailingStopYuzde, ButceLimitYuzde FROM Hisse WHERE HisseAdi='Default'

-- Mevcut KademeStrateji davranisi korunuyor mu? (PozisyonTipi=0 default)
EXEC sel_hisseSatimKontrol 'AKBNK', 50.0, 0.25

-- Yeni SP'ler calisiyor mu?
EXEC sel_hisseSatimKontrolZamanli 'AKBNK', 50.0, 0.25
EXEC sel_hisseSatimKontrolCore 'AKBNK', 50.0, 100, 5.0
EXEC upd_hisseHareketTepeNoktasi 'AKBNK', 55.0
*/
