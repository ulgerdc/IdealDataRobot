-- Arbitraj Global Butce Yonetimi Migration
-- Tarih: 2026-02-25

-- 1. Butce kolonu ekle
ALTER TABLE ArbitrajGelismis ADD Butce float NOT NULL DEFAULT 0;
GO

-- 2. Global config satiri
INSERT INTO ArbitrajGelismis (HisseAdi, ArbitrajTipi, YakinVadeKodu, YakinVadeSonGun, BistLot, ViopLot, GirisMarji, CikisMarji, AktifMi, YillikFaiz, TemettuTutar, Butce, Tarih)
VALUES ('_GLOBAL', -1, '', '2026-01-01', 0, 0, 0, 0, 0, 0, 0, 250000, GETDATE());
GO

-- 3. sel_arbitrajGelismis SP guncelle (Butce kolonu ekle)
ALTER PROCEDURE sel_arbitrajGelismis
AS
SELECT Id, HisseAdi, ArbitrajTipi, YakinVadeKodu, UzakVadeKodu,
       YakinVadeSonGun, UzakVadeSonGun, BistLot, ViopLot,
       GirisMarji, CikisMarji, AktifMi, YillikFaiz,
       TemettuTutar, TemettuTarihi, Butce
FROM ArbitrajGelismis
WHERE AktifMi = 1
GO
