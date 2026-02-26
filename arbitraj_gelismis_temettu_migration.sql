-- ============================================================
-- Gelismis Arbitraj - Temettu Duzeltmesi Migration
-- Tarih: 2026-02-25
-- Aciklama: TemettuTutar + TemettuTarihi kolonlari, SP guncelleme
-- ============================================================

-- ============================================================
-- 1. ArbitrajGelismis tablosuna temettu kolonlari ekle
-- ============================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ArbitrajGelismis') AND name = 'TemettuTutar')
BEGIN
    ALTER TABLE ArbitrajGelismis ADD TemettuTutar float NOT NULL DEFAULT 0
    PRINT 'ArbitrajGelismis.TemettuTutar kolonu eklendi.'
END
ELSE
    PRINT 'ArbitrajGelismis.TemettuTutar kolonu zaten mevcut.'
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('ArbitrajGelismis') AND name = 'TemettuTarihi')
BEGIN
    ALTER TABLE ArbitrajGelismis ADD TemettuTarihi smalldatetime NULL
    PRINT 'ArbitrajGelismis.TemettuTarihi kolonu eklendi.'
END
ELSE
    PRINT 'ArbitrajGelismis.TemettuTarihi kolonu zaten mevcut.'
GO

-- ============================================================
-- 2. sel_arbitrajGelismis SP guncelle (+TemettuTutar, +TemettuTarihi)
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
           GirisMarji, CikisMarji, AktifMi, YillikFaiz,
           TemettuTutar, TemettuTarihi
    FROM ArbitrajGelismis
    WHERE AktifMi = 1
END
GO
PRINT 'sel_arbitrajGelismis SP guncellendi (+TemettuTutar, +TemettuTarihi).'
GO

PRINT '=== Temettu migration tamamlandi ==='
GO
