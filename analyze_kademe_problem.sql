-- Analiz: KademeStrateji neden maliyet sürekli piyasadan yüksek?

PRINT '=== 1. Genel Durum: Maliyet > Piyasa Olan Hisseler ==='
SELECT TOP 10
    HisseAdi,
    kar as [Gerceklesen Kar],
    aktiflot as [Acik Lot],
    ROUND(piyasasatis, 2) as [Piyasa Fiyat],
    ROUND(maliyet, 2) as [Ortalama Maliyet],
    ROUND(maliyet - piyasasatis, 2) as [Fark],
    ROUND(((maliyet - piyasasatis) / NULLIF(maliyet, 0)) * 100, 2) as [% Fark],
    portfoy as [Portfoy Degeri],
    ROUND((piyasasatis * aktiflot) - (maliyet * aktiflot), 2) as [Acik Poz Zarar]
FROM (
    SELECT
        a.HisseAdi,
        a.realizedKar as kar,
        a.aktiflot,
        a.piyasasatis,
        a.aktifHarcanan / NULLIF(a.aktiflot, 0) as maliyet,
        (a.aktiflot * a.piyasaalis) as portfoy
    FROM (
        SELECT
            v.[HisseAdi],
            SUM(CASE WHEN aktifMi = 0 THEN kar ELSE 0 END) as realizedKar,
            SUM(Lot * aktifMi) as aktiflot,
            AVG(h.PiyasaSatis) as piyasasatis,
            AVG(h.PiyasaAlis) as piyasaalis,
            SUM(AlisFiyati * Lot * aktifMi) as aktifHarcanan
        FROM [Robot].[dbo].[vHisseHareket] v
        INNER JOIN [Robot].[dbo].[Hisse] h ON v.HisseAdi = h.HisseAdi
        GROUP BY v.[HisseAdi]
        HAVING SUM(Lot * aktifMi) > 0
    ) AS a
) AS portfoy
WHERE maliyet > piyasasatis
ORDER BY [% Fark] DESC

PRINT ''
PRINT '=== 2. Bir Hisse Detayi: En Yüksek Maliyetli Olanı İnceleyelim ==='
DECLARE @problematicHisse varchar(20)

SELECT TOP 1 @problematicHisse = HisseAdi
FROM (
    SELECT
        v.[HisseAdi],
        AVG(h.PiyasaSatis) as piyasasatis,
        SUM(AlisFiyati * Lot * aktifMi) / NULLIF(SUM(Lot * aktifMi), 0) as maliyet
    FROM [Robot].[dbo].[vHisseHareket] v
    INNER JOIN [Robot].[dbo].[Hisse] h ON v.HisseAdi = h.HisseAdi
    GROUP BY v.[HisseAdi]
    HAVING SUM(Lot * aktifMi) > 0
) AS a
WHERE maliyet > piyasasatis
ORDER BY ((maliyet - piyasasatis) / NULLIF(maliyet, 0)) * 100 DESC

PRINT 'Analiz edilen hisse: ' + @problematicHisse
PRINT ''

-- Bu hissenin tüm pozisyonları
PRINT '--- Açık Pozisyonlar (Yeni -> Eski) ---'
SELECT TOP 20
    Id,
    HisseAdi,
    Lot,
    AlisFiyati,
    AlisTarihi,
    DATEDIFF(day, AlisTarihi, GETDATE()) as [Gun Once]
FROM HisseHareket
WHERE HisseAdi = @problematicHisse
  AND AktifMi = 1
ORDER BY AlisTarihi DESC

-- Kapalı pozisyonlar
PRINT ''
PRINT '--- Son 10 Kapanan Pozisyon ---'
SELECT TOP 10
    Id,
    HisseAdi,
    Lot,
    AlisFiyati,
    SatisFiyati,
    Kar,
    AlisTarihi,
    SatisTarihi,
    DATEDIFF(day, AlisTarihi, SatisTarihi) as [Elde Tutma Gun]
FROM HisseHareket
WHERE HisseAdi = @problematicHisse
  AND AktifMi = 0
ORDER BY SatisTarihi DESC

-- Hisse konfigürasyonu
PRINT ''
PRINT '--- Hisse Konfigürasyonu ---'
SELECT
    HisseAdi,
    Marj,
    MarjTipi,
    CASE MarjTipi
        WHEN 0 THEN 'Kademe (spread bazlı)'
        WHEN 1 THEN 'Binde (per-mille)'
        ELSE 'Bilinmeyen'
    END as MarjTipiAciklama,
    AlimTutari,
    Butce,
    AlisAktif,
    SatisAktif,
    PiyasaAlis,
    PiyasaSatis
FROM Hisse
WHERE HisseAdi = @problematicHisse

PRINT ''
PRINT '=== 3. Alım/Satım Fiyat Dağılımı Analizi ==='
SELECT
    @problematicHisse as HisseAdi,
    COUNT(*) as [Toplam Acik Pozisyon],
    MIN(AlisFiyati) as [En Dusuk Alis],
    MAX(AlisFiyati) as [En Yuksek Alis],
    AVG(AlisFiyati) as [Ortalama Alis],
    (SELECT AVG(PiyasaSatis) FROM Hisse WHERE HisseAdi = @problematicHisse) as [Guncel Piyasa],
    SUM(Lot) as [Toplam Lot],
    SUM(AlisFiyati * Lot) / SUM(Lot) as [Agirlikli Ortalama Maliyet]
FROM HisseHareket
WHERE HisseAdi = @problematicHisse
  AND AktifMi = 1

PRINT ''
PRINT '=== 4. Fiyat Seviye Dağılımı (Histogram) ==='
-- Kaç lot hangi fiyat aralığında?
SELECT
    CASE
        WHEN AlisFiyati < (SELECT PiyasaSatis FROM Hisse WHERE HisseAdi = @problematicHisse) * 0.90 THEN 'Cok Dusuk (<-10%)'
        WHEN AlisFiyati < (SELECT PiyasaSatis FROM Hisse WHERE HisseAdi = @problematicHisse) * 0.95 THEN 'Dusuk (-5% to -10%)'
        WHEN AlisFiyati < (SELECT PiyasaSatis FROM Hisse WHERE HisseAdi = @problematicHisse) * 1.00 THEN 'Yakin (-0% to -5%)'
        WHEN AlisFiyati < (SELECT PiyasaSatis FROM Hisse WHERE HisseAdi = @problematicHisse) * 1.05 THEN 'Biraz Yuksek (0% to +5%)'
        WHEN AlisFiyati < (SELECT PiyasaSatis FROM Hisse WHERE HisseAdi = @problematicHisse) * 1.10 THEN 'Yuksek (+5% to +10%)'
        ELSE 'Cok Yuksek (>+10%)'
    END as [Fiyat Seviyesi],
    COUNT(*) as [Pozisyon Sayisi],
    SUM(Lot) as [Toplam Lot],
    SUM(AlisFiyati * Lot) as [Toplam Tutar],
    ROUND(AVG(AlisFiyati), 2) as [Ort Alis Fiyati]
FROM HisseHareket
WHERE HisseAdi = @problematicHisse
  AND AktifMi = 1
GROUP BY
    CASE
        WHEN AlisFiyati < (SELECT PiyasaSatis FROM Hisse WHERE HisseAdi = @problematicHisse) * 0.90 THEN 'Cok Dusuk (<-10%)'
        WHEN AlisFiyati < (SELECT PiyasaSatis FROM Hisse WHERE HisseAdi = @problematicHisse) * 0.95 THEN 'Dusuk (-5% to -10%)'
        WHEN AlisFiyati < (SELECT PiyasaSatis FROM Hisse WHERE HisseAdi = @problematicHisse) * 1.00 THEN 'Yakin (-0% to -5%)'
        WHEN AlisFiyati < (SELECT PiyasaSatis FROM Hisse WHERE HisseAdi = @problematicHisse) * 1.05 THEN 'Biraz Yuksek (0% to +5%)'
        WHEN AlisFiyati < (SELECT PiyasaSatis FROM Hisse WHERE HisseAdi = @problematicHisse) * 1.10 THEN 'Yuksek (+5% to +10%)'
        ELSE 'Cok Yuksek (>+10%)'
    END
ORDER BY [Toplam Lot] DESC

PRINT ''
PRINT '=== 5. Problem Teşhisi ==='
PRINT 'Yukarıdaki verilerden şunları kontrol edin:'
PRINT '1. Açık pozisyonların çoğu yüksek fiyattan mı alınmış?'
PRINT '2. Eski (30+ gün) yüksek fiyatlı pozisyonlar var mı?'
PRINT '3. Marj ayarı çok düşük mü? (Satış gerçekleşemiyor)'
PRINT '4. Piyasa düşerken çok fazla alım mı yapılıyor?'
PRINT '5. EndeksDegerlendir ile lot artışı agresif mi?'
