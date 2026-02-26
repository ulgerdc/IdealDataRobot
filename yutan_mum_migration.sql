-- =============================================
-- Yutan Mum (Bullish Engulfing) Stratejisi
-- Migration Script
-- 2026-02-25
-- =============================================

-- 1.1 HisseGunlukVeri'ye Acilis + Hacim kolonlari
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HisseGunlukVeri') AND name = 'Acilis')
BEGIN
    ALTER TABLE HisseGunlukVeri ADD Acilis decimal(18,2) NULL
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('HisseGunlukVeri') AND name = 'Hacim')
BEGIN
    ALTER TABLE HisseGunlukVeri ADD Hacim bigint NULL DEFAULT 0
END
GO

-- ins_hisseGunlukVeri SP guncelle (+@Acilis, +@Hacim optional params)
IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'ins_hisseGunlukVeri')
    DROP PROCEDURE ins_hisseGunlukVeri
GO

CREATE PROCEDURE ins_hisseGunlukVeri
    @HisseAdi varchar(50),
    @Yuksek decimal(18,2),
    @Dusuk decimal(18,2),
    @Kapanis decimal(18,2),
    @Acilis decimal(18,2) = NULL,
    @Hacim bigint = NULL
AS
BEGIN
    SET NOCOUNT ON

    IF EXISTS (SELECT 1 FROM HisseGunlukVeri WHERE HisseAdi = @HisseAdi AND CAST(Tarih AS DATE) = CAST(GETDATE() AS DATE))
    BEGIN
        UPDATE HisseGunlukVeri
        SET Yuksek = @Yuksek, Dusuk = @Dusuk, Kapanis = @Kapanis,
            Acilis = ISNULL(@Acilis, Acilis), Hacim = ISNULL(@Hacim, Hacim)
        WHERE HisseAdi = @HisseAdi AND CAST(Tarih AS DATE) = CAST(GETDATE() AS DATE)
    END
    ELSE
    BEGIN
        INSERT INTO HisseGunlukVeri (HisseAdi, Yuksek, Dusuk, Kapanis, Acilis, Hacim, Tarih)
        VALUES (@HisseAdi, @Yuksek, @Dusuk, @Kapanis, @Acilis, @Hacim, GETDATE())
    END
END
GO

-- 1.2 Bist100Hisseler tablosu
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'Bist100Hisseler')
BEGIN
    CREATE TABLE Bist100Hisseler (
        Id bigint IDENTITY(1,1) PRIMARY KEY,
        HisseAdi varchar(50) NOT NULL UNIQUE,
        AktifMi bit NOT NULL DEFAULT 1,
        EklenmeTarihi smalldatetime DEFAULT GETDATE()
    )
END
GO

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sel_bist100Hisseler')
    DROP PROCEDURE sel_bist100Hisseler
GO

CREATE PROCEDURE sel_bist100Hisseler
AS
BEGIN
    SET NOCOUNT ON
    SELECT HisseAdi FROM Bist100Hisseler WHERE AktifMi = 1 ORDER BY HisseAdi
END
GO

-- BIST100 hisse listesi INSERT (idempotent)
IF NOT EXISTS (SELECT 1 FROM Bist100Hisseler)
BEGIN
    INSERT INTO Bist100Hisseler (HisseAdi) VALUES
    ('AEFES'),('AFYON'),('AGESA'),('AKBNK'),('AKFGY'),
    ('AKSA'),('AKSEN'),('ALARK'),('ALFAS'),('ARCLK'),
    ('ASELS'),('ASUZU'),('BERA'),('BIMAS'),('BIOEN'),
    ('BRSAN'),('BRYAT'),('BUCIM'),('CCOLA'),('CIMSA'),
    ('CWENE'),('DOAS'),('ECILC'),('EGEEN'),('EKGYO'),
    ('ENERY'),('ENJSA'),('ENKAI'),('EREGL'),('EUPWR'),
    ('FROTO'),('GARAN'),('GENIL'),('GESAN'),('GUBRF'),
    ('GWIND'),('HALKB'),('HEKTS'),('ISDMR'),('ISFIN'),
    ('ISGYO'),('ISKUR'),('ISMEN'),('ISSEN'),('KCHOL'),
    ('KMPUR'),('KONTR'),('KONYA'),('KOZAA'),('KOZAL'),
    ('KRDMD'),('LMKDC'),('LOGO'),('MGROS'),('MPARK'),
    ('ODAS'),('OTKAR'),('OYAKC'),('PETKM'),('PGSUS'),
    ('QUAGR'),('SAHOL'),('SASA'),('SDTTR'),('SISE'),
    ('SKBNK'),('SMRTG'),('SOKM'),('TABGD'),('TATGD'),
    ('TAVHL'),('TCELL'),('THYAO'),('TKFEN'),('TKNSA'),
    ('TOASO'),('TRGYO'),('TSKB'),('TTKOM'),('TTRAK'),
    ('TUKAS'),('TUPRS'),('TURSG'),('ULKER'),('VAKBN'),
    ('VESBE'),('VESTL'),('YEOTK'),('YKBNK'),('YYLGD'),
    ('ZOREN'),('AGHOL'),('ANSGR'),('AYDEM'),('BASGZ'),
    ('BTCIM'),('CANTE'),('DOHOL'),('KCAER'),('KLSER')
END
GO

-- 1.3 YutanMumConfig tablosu
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'YutanMumConfig')
BEGIN
    CREATE TABLE YutanMumConfig (
        Id bigint IDENTITY(1,1) PRIMARY KEY,
        ToplamButce float DEFAULT 100000,
        MaxAktifBatch int DEFAULT 3,
        MaxGunSayisi int DEFAULT 5,
        MinHacimCarpani float DEFAULT 1.0,
        IslemSaati varchar(10) DEFAULT '10:15',
        MinButcePerHisse float DEFAULT 1000,
        AktifMi bit DEFAULT 1
    )
END
GO

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sel_yutanMumConfig')
    DROP PROCEDURE sel_yutanMumConfig
GO

CREATE PROCEDURE sel_yutanMumConfig
AS
BEGIN
    SET NOCOUNT ON
    SELECT TOP 1 * FROM YutanMumConfig
END
GO

-- Default config satiri
IF NOT EXISTS (SELECT 1 FROM YutanMumConfig)
BEGIN
    INSERT INTO YutanMumConfig (ToplamButce, MaxAktifBatch, MaxGunSayisi, MinHacimCarpani, IslemSaati, MinButcePerHisse, AktifMi)
    VALUES (100000, 3, 5, 1.0, '10:15', 1000, 1)
END
GO

-- 1.4 YutanMumBatch tablosu
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'YutanMumBatch')
BEGIN
    CREATE TABLE YutanMumBatch (
        Id bigint IDENTITY(1,1) PRIMARY KEY,
        RobotAdi varchar(50),
        BatchTarihi smalldatetime DEFAULT GETDATE(),
        HisseSayisi int DEFAULT 0,
        ToplamAlimTutari float DEFAULT 0,
        ToplamKar float DEFAULT 0,
        AktifMi bit DEFAULT 1,
        KapanisTarihi smalldatetime NULL,
        KapanisNedeni varchar(50) NULL
    )
END
GO

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'ins_yutanMumBatch')
    DROP PROCEDURE ins_yutanMumBatch
GO

CREATE PROCEDURE ins_yutanMumBatch
    @RobotAdi varchar(50),
    @HisseSayisi int,
    @ToplamAlimTutari float
AS
BEGIN
    SET NOCOUNT ON
    INSERT INTO YutanMumBatch (RobotAdi, HisseSayisi, ToplamAlimTutari)
    VALUES (@RobotAdi, @HisseSayisi, @ToplamAlimTutari)

    SELECT SCOPE_IDENTITY() AS BatchId
END
GO

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sel_yutanMumAktifBatchler')
    DROP PROCEDURE sel_yutanMumAktifBatchler
GO

CREATE PROCEDURE sel_yutanMumAktifBatchler
AS
BEGIN
    SET NOCOUNT ON
    SELECT * FROM YutanMumBatch WHERE AktifMi = 1 ORDER BY BatchTarihi
END
GO

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sel_yutanMumBugunBatchVar')
    DROP PROCEDURE sel_yutanMumBugunBatchVar
GO

CREATE PROCEDURE sel_yutanMumBugunBatchVar
AS
BEGIN
    SET NOCOUNT ON
    SELECT TOP 1 Id FROM YutanMumBatch
    WHERE CAST(BatchTarihi AS DATE) = CAST(GETDATE() AS DATE) AND AktifMi = 1
END
GO

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'upd_yutanMumBatchKapat')
    DROP PROCEDURE upd_yutanMumBatchKapat
GO

CREATE PROCEDURE upd_yutanMumBatchKapat
    @BatchId bigint,
    @ToplamKar float,
    @KapanisNedeni varchar(50)
AS
BEGIN
    SET NOCOUNT ON
    UPDATE YutanMumBatch
    SET AktifMi = 0, KapanisTarihi = GETDATE(), ToplamKar = @ToplamKar, KapanisNedeni = @KapanisNedeni
    WHERE Id = @BatchId
END
GO

-- 1.5 YutanMumHareket tablosu
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'YutanMumHareket')
BEGIN
    CREATE TABLE YutanMumHareket (
        Id bigint IDENTITY(1,1) PRIMARY KEY,
        BatchId bigint FOREIGN KEY REFERENCES YutanMumBatch(Id),
        HisseAdi varchar(50),
        Lot int,
        AlisFiyati decimal(18,2),
        SatisFiyati decimal(18,2) NULL,
        Kar decimal(18,2) NULL,
        AlisTarihi smalldatetime DEFAULT GETDATE(),
        SatisTarihi smalldatetime NULL,
        AktifMi bit DEFAULT 1,
        DunkuAcilis decimal(18,2) NULL,
        DunkuKapanis decimal(18,2) NULL,
        BugunAcilis decimal(18,2) NULL,
        BugunKapanis decimal(18,2) NULL,
        DunkuHacim bigint NULL,
        BugunHacim bigint NULL
    )
END
GO

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'ins_yutanMumHareket')
    DROP PROCEDURE ins_yutanMumHareket
GO

CREATE PROCEDURE ins_yutanMumHareket
    @BatchId bigint,
    @HisseAdi varchar(50),
    @Lot int,
    @AlisFiyati decimal(18,2),
    @DunkuAcilis decimal(18,2) = NULL,
    @DunkuKapanis decimal(18,2) = NULL,
    @BugunAcilis decimal(18,2) = NULL,
    @BugunKapanis decimal(18,2) = NULL,
    @DunkuHacim bigint = NULL,
    @BugunHacim bigint = NULL
AS
BEGIN
    SET NOCOUNT ON
    INSERT INTO YutanMumHareket (BatchId, HisseAdi, Lot, AlisFiyati, DunkuAcilis, DunkuKapanis, BugunAcilis, BugunKapanis, DunkuHacim, BugunHacim)
    VALUES (@BatchId, @HisseAdi, @Lot, @AlisFiyati, @DunkuAcilis, @DunkuKapanis, @BugunAcilis, @BugunKapanis, @DunkuHacim, @BugunHacim)
END
GO

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'sel_yutanMumBatchHareketler')
    DROP PROCEDURE sel_yutanMumBatchHareketler
GO

CREATE PROCEDURE sel_yutanMumBatchHareketler
    @BatchId bigint
AS
BEGIN
    SET NOCOUNT ON
    SELECT * FROM YutanMumHareket WHERE BatchId = @BatchId AND AktifMi = 1
END
GO

IF EXISTS (SELECT 1 FROM sys.procedures WHERE name = 'upd_yutanMumHareketSat')
    DROP PROCEDURE upd_yutanMumHareketSat
GO

CREATE PROCEDURE upd_yutanMumHareketSat
    @Id bigint,
    @SatisFiyati decimal(18,2)
AS
BEGIN
    SET NOCOUNT ON
    UPDATE YutanMumHareket
    SET SatisFiyati = @SatisFiyati,
        Kar = Lot * (@SatisFiyati - AlisFiyati),
        AktifMi = 0,
        SatisTarihi = GETDATE()
    WHERE Id = @Id
END
GO
