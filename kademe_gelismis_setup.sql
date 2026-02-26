-- KademeStratejiGelismis için veritabanı güncellemeleri ve konfigürasyon
-- Tarih: 2025-10-13

PRINT '=== KademeStratejiGelismis - Veritabanı Setup ==='
PRINT ''

-- 1. Hisse tablosuna yeni alanlar ekle (opsiyonel - şimdilik kullanılmıyor ama eklenebilir)
PRINT '--- Hisse Tablosu Güncellemeleri ---'

-- Stop-Loss yüzdesi ekleme (ileride kullanılabilir)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Hisse') AND name = 'StopLossYuzde')
BEGIN
    ALTER TABLE Hisse ADD StopLossYuzde decimal(5,2) NULL
    UPDATE Hisse SET StopLossYuzde = 15.0 WHERE StopLossYuzde IS NULL
    PRINT 'StopLossYuzde alanı eklendi (varsayılan: %15)'
END
ELSE
BEGIN
    PRINT 'StopLossYuzde alanı zaten mevcut'
END

-- Zaman bazlı satış için gün sayısı (ileride kullanılabilir)
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Hisse') AND name = 'ZamanSatisGun')
BEGIN
    ALTER TABLE Hisse ADD ZamanSatisGun int NULL
    UPDATE Hisse SET ZamanSatisGun = 90 WHERE ZamanSatisGun IS NULL
    PRINT 'ZamanSatisGun alanı eklendi (varsayılan: 90 gün)'
END
ELSE
BEGIN
    PRINT 'ZamanSatisGun alanı zaten mevcut'
END

-- Gelişmiş strateji aktif/pasif kontrolü
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Hisse') AND name = 'GelismisStrateji')
BEGIN
    ALTER TABLE Hisse ADD GelismisStrateji bit NULL
    UPDATE Hisse SET GelismisStrateji = 0 WHERE GelismisStrateji IS NULL
    PRINT 'GelismisStrateji alanı eklendi (varsayılan: 0=kapalı)'
END
ELSE
BEGIN
    PRINT 'GelismisStrateji alanı zaten mevcut'
END

PRINT ''
PRINT '--- Mevcut Sorunlu Hisseler Analizi ---'

-- Hangi hisselerde stop-loss pozisyonları var?
SELECT
    h.HisseAdi,
    COUNT(*) as [Stop-Loss Pozisyon Sayisi],
    SUM(h.Lot) as [Toplam Lot],
    AVG(h.AlisFiyati) as [Ortalama Alis],
    s.PiyasaSatis as [Guncel Fiyat],
    AVG((h.AlisFiyati - s.PiyasaSatis) / h.AlisFiyati * 100) as [Ortalama Kayip %]
FROM HisseHareket h
INNER JOIN Hisse s ON h.HisseAdi = s.HisseAdi
WHERE h.AktifMi = 1
  AND ((h.AlisFiyati - s.PiyasaSatis) / h.AlisFiyati * 100) >= 15.0
GROUP BY h.HisseAdi, s.PiyasaSatis
ORDER BY [Ortalama Kayip %] DESC

PRINT ''
PRINT '--- Eski Pozisyonlar (90+ Gün) Analizi ---'

SELECT
    h.HisseAdi,
    COUNT(*) as [Eski Pozisyon Sayisi],
    SUM(h.Lot) as [Toplam Lot],
    AVG(DATEDIFF(day, h.AlisTarihi, GETDATE())) as [Ortalama Yas (Gun)],
    AVG(h.AlisFiyati) as [Ortalama Alis],
    s.PiyasaSatis as [Guncel Fiyat],
    SUM(h.Lot * h.AlisFiyati) as [Toplam Tutar]
FROM HisseHareket h
INNER JOIN Hisse s ON h.HisseAdi = s.HisseAdi
WHERE h.AktifMi = 1
  AND DATEDIFF(day, h.AlisTarihi, GETDATE()) >= 90
  AND h.AlisFiyati > s.PiyasaSatis
GROUP BY h.HisseAdi, s.PiyasaSatis
ORDER BY [Toplam Tutar] DESC

PRINT ''
PRINT '=== MARJ ÖNERİLERİ ==='
PRINT ''
PRINT 'Mevcut marj ayarları çok düşük. Aşağıdaki güncellemeleri öneririz:'
PRINT ''

-- Marj önerileri göster
SELECT
    HisseAdi,
    Marj as [Mevcut Marj (binde)],
    CASE
        WHEN Marj < 10 THEN 20
        WHEN Marj < 20 THEN 30
        ELSE Marj
    END as [Önerilen Marj (binde)],
    MarjTipi,
    CASE MarjTipi
        WHEN 0 THEN 'Kademe bazlı'
        WHEN 1 THEN 'Binde (per-mille)'
        ELSE 'Bilinmeyen'
    END as [Marj Tipi]
FROM Hisse
WHERE HisseAdi <> 'Default'
  AND Marj < 20
ORDER BY Marj ASC

PRINT ''
PRINT '=== MARJ GÜNCELLEMESİ (Dikkatli Kullanın!) ==='
PRINT 'Aşağıdaki komutları çalıştırarak marjları güncelleyebilirsiniz:'
PRINT ''
PRINT '-- Tüm hisseler için marjı 20 bindeyap (2%):'
PRINT '-- UPDATE Hisse SET Marj = 20 WHERE Marj < 20 AND HisseAdi <> ''Default'''
PRINT ''
PRINT '-- Belirli bir hisse için:'
PRINT '-- UPDATE Hisse SET Marj = 25 WHERE HisseAdi = ''BRSAN'''
PRINT ''
PRINT 'NOT: Marj güncellemeleri dikkatli yapılmalı, mevcut açık pozisyonları etkiler!'

PRINT ''
PRINT '=== SETUP TAMAMLANDI ==='
PRINT 'KademeStratejiGelismis kullanmaya hazır.'
PRINT ''
PRINT 'Sonraki adımlar:'
PRINT '1. Marj ayarlarını gözden geçirin ve gerekiyorsa güncelleyin'
PRINT '2. GelismisStrateji=1 yaparak test hissesi seçin'
PRINT '3. AlimSatimRobotu veya Denali.Test ile test edin'
PRINT '4. Sonuçları gözlemleyin ve fine-tuning yapın'
