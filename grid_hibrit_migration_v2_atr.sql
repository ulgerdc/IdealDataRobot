USE [Robot]
GO

-- =====================================================
-- Grid Hibrit Strateji Migration V2 - ATR Destegi
-- Tarih: 2026-02-23
-- Aciklama: Gunluk/Haftalik ATR hesaplama icin tablo ve SP'ler
-- =====================================================

-- Gunluk fiyat verisi tablosu
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE object_id = OBJECT_ID('HisseGunlukVeri') AND type = 'U')
BEGIN
    CREATE TABLE [dbo].[HisseGunlukVeri] (
        Id bigint IDENTITY(1,1) PRIMARY KEY,
        HisseAdi varchar(50) NOT NULL,
        Tarih date NOT NULL,
        Yuksek decimal(18,2) NOT NULL,
        Dusuk decimal(18,2) NOT NULL,
        Kapanis decimal(18,2) NOT NULL,
        CONSTRAINT UQ_HisseGunlukVeri UNIQUE (HisseAdi, Tarih)
    )
    CREATE INDEX IX_HisseGunlukVeri_HisseAdi ON [dbo].[HisseGunlukVeri] (HisseAdi, Tarih DESC)
    PRINT 'HisseGunlukVeri tablosu olusturuldu'
END
GO

-- Hisse tablosuna ATR MarjTipi destegi (MarjTipi=2 ATR bazli)
-- ve ATR parametreleri ekle
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Hisse') AND name = 'AtrPeriyot')
BEGIN
    ALTER TABLE [dbo].[Hisse] ADD AtrPeriyot int DEFAULT 14
    PRINT 'Hisse.AtrPeriyot eklendi'
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Hisse') AND name = 'AtrCarpan')
BEGIN
    ALTER TABLE [dbo].[Hisse] ADD AtrCarpan decimal(5,2) DEFAULT 0.50
    PRINT 'Hisse.AtrCarpan eklendi'
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Hisse') AND name = 'AtrZamanDilimi')
BEGIN
    -- D = Gunluk, W = Haftalik
    ALTER TABLE [dbo].[Hisse] ADD AtrZamanDilimi char(1) DEFAULT 'D'
    PRINT 'Hisse.AtrZamanDilimi eklendi'
END
GO

-- Default satirini guncelle
UPDATE [dbo].[Hisse]
SET AtrPeriyot = ISNULL(AtrPeriyot, 14),
    AtrCarpan = ISNULL(AtrCarpan, 0.50),
    AtrZamanDilimi = ISNULL(AtrZamanDilimi, 'D')
WHERE HisseAdi = 'Default'
GO

-- =====================================================
-- STORED PROCEDURE'LAR
-- =====================================================

-- Gunluk veri kaydet (upsert: gun icerisinde gunceller)
IF OBJECT_ID('[dbo].[ins_hisseGunlukVeri]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[ins_hisseGunlukVeri]
GO

CREATE PROC [dbo].[ins_hisseGunlukVeri]
    @HisseAdi varchar(50),
    @Yuksek decimal(18,2),
    @Dusuk decimal(18,2),
    @Kapanis decimal(18,2)
AS
BEGIN
    DECLARE @tarih date = CAST(GETDATE() AS date)

    IF EXISTS (SELECT 1 FROM HisseGunlukVeri WHERE HisseAdi = @HisseAdi AND Tarih = @tarih)
    BEGIN
        UPDATE HisseGunlukVeri
        SET Yuksek = CASE WHEN @Yuksek > Yuksek THEN @Yuksek ELSE Yuksek END,
            Dusuk = CASE WHEN @Dusuk < Dusuk THEN @Dusuk ELSE Dusuk END,
            Kapanis = @Kapanis
        WHERE HisseAdi = @HisseAdi AND Tarih = @tarih
    END
    ELSE
    BEGIN
        INSERT INTO HisseGunlukVeri (HisseAdi, Tarih, Yuksek, Dusuk, Kapanis)
        VALUES (@HisseAdi, @tarih, @Yuksek, @Dusuk, @Kapanis)
    END
END
GO

-- ATR hesapla (Gunluk veya Haftalik)
IF OBJECT_ID('[dbo].[sel_hisseATR]', 'P') IS NOT NULL
    DROP PROCEDURE [dbo].[sel_hisseATR]
GO

CREATE PROC [dbo].[sel_hisseATR]
    @HisseAdi varchar(50),
    @Periyot int = 14,
    @ZamanDilimi char(1) = 'D'  -- D=Gunluk, W=Haftalik
AS
BEGIN
    -- Haftalik mod icin gunluk verileri haftalik OHLC'ye donustur
    IF @ZamanDilimi = 'W'
    BEGIN
        ;WITH HaftalikVeri AS (
            SELECT
                DATEPART(YEAR, Tarih) AS Yil,
                DATEPART(WEEK, Tarih) AS Hafta,
                MAX(Yuksek) AS Yuksek,
                MIN(Dusuk) AS Dusuk,
                -- Haftanin son kapanis degeri
                (SELECT TOP 1 Kapanis FROM HisseGunlukVeri
                 WHERE HisseAdi = @HisseAdi
                   AND DATEPART(YEAR, Tarih) = DATEPART(YEAR, v.Tarih)
                   AND DATEPART(WEEK, Tarih) = DATEPART(WEEK, v.Tarih)
                 ORDER BY Tarih DESC) AS Kapanis
            FROM HisseGunlukVeri v
            WHERE HisseAdi = @HisseAdi
            GROUP BY DATEPART(YEAR, Tarih), DATEPART(WEEK, Tarih), Tarih
        ),
        -- True Range hesapla (haftalik)
        SiraliHafta AS (
            SELECT *, ROW_NUMBER() OVER (ORDER BY Yil DESC, Hafta DESC) AS RowNum
            FROM HaftalikVeri
        ),
        TrueRange AS (
            SELECT
                s1.RowNum,
                CASE
                    WHEN s2.Kapanis IS NULL THEN s1.Yuksek - s1.Dusuk
                    ELSE (
                        SELECT MAX(v) FROM (VALUES
                            (s1.Yuksek - s1.Dusuk),
                            (ABS(s1.Yuksek - s2.Kapanis)),
                            (ABS(s1.Dusuk - s2.Kapanis))
                        ) AS T(v)
                    )
                END AS TR
            FROM SiraliHafta s1
            LEFT JOIN SiraliHafta s2 ON s2.RowNum = s1.RowNum + 1
            WHERE s1.RowNum <= @Periyot
        )
        SELECT ROUND(AVG(TR), 2) AS ATR FROM TrueRange
    END
    ELSE
    BEGIN
        -- Gunluk True Range hesapla
        ;WITH SiraliGun AS (
            SELECT *, ROW_NUMBER() OVER (ORDER BY Tarih DESC) AS RowNum
            FROM HisseGunlukVeri
            WHERE HisseAdi = @HisseAdi
        ),
        TrueRange AS (
            SELECT
                s1.RowNum,
                s1.Tarih,
                CASE
                    WHEN s2.Kapanis IS NULL THEN s1.Yuksek - s1.Dusuk
                    ELSE (
                        SELECT MAX(v) FROM (VALUES
                            (s1.Yuksek - s1.Dusuk),
                            (ABS(s1.Yuksek - s2.Kapanis)),
                            (ABS(s1.Dusuk - s2.Kapanis))
                        ) AS T(v)
                    )
                END AS TR
            FROM SiraliGun s1
            LEFT JOIN SiraliGun s2 ON s2.RowNum = s1.RowNum + 1
            WHERE s1.RowNum <= @Periyot
        )
        SELECT ROUND(AVG(TR), 2) AS ATR FROM TrueRange
    END
END
GO

-- =====================================================
-- DOGRULAMA
-- =====================================================
/*
-- Tablo var mi?
SELECT TOP 5 * FROM HisseGunlukVeri

-- Hisse ATR parametreleri
SELECT HisseAdi, AtrPeriyot, AtrCarpan, AtrZamanDilimi FROM Hisse WHERE HisseAdi = 'Default'

-- Test veri ekle
EXEC ins_hisseGunlukVeri 'THYAO', 320.00, 315.00, 318.00

-- ATR hesapla (en az 14 gunluk veri gerekli)
EXEC sel_hisseATR 'THYAO', 14, 'D'
*/
