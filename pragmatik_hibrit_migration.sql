-- Pragmatik Hibrit Strateji - Migration Script
-- Tarih: 2026-02-23
-- Aciklama: Butce ATR gecis yuzdesini ekler, ButceLimitYuzde default'u 80'e gunceller

-- 1. Hisse tablosuna ButceAtrGecisYuzde kolonu ekle
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('Hisse') AND name = 'ButceAtrGecisYuzde')
BEGIN
    ALTER TABLE Hisse ADD ButceAtrGecisYuzde float NOT NULL DEFAULT 60;
    PRINT 'ButceAtrGecisYuzde kolonu eklendi';
END
ELSE
BEGIN
    PRINT 'ButceAtrGecisYuzde kolonu zaten mevcut';
END
GO

-- 2. ButceLimitYuzde default 60 olan satirlari 80'e guncelle
UPDATE Hisse SET ButceLimitYuzde = 80 WHERE ButceLimitYuzde = 60;
PRINT 'ButceLimitYuzde 60 -> 80 guncellendi';

-- 3. Default satirinin ButceAtrGecisYuzde degerini ayarla
UPDATE Hisse SET ButceAtrGecisYuzde = 60 WHERE HisseAdi = 'Default';
PRINT 'Default satiri ButceAtrGecisYuzde = 60 ayarlandi';
