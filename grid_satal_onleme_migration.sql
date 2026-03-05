-- Grid Sat-Al Dongusu Onleme Migration
-- Tarih: 2026-03-03
-- Aciklama: sel_hisseAlimKontrol SP'sine bugun satilan pozisyonlarin SatisFiyati kontrolu ekleniyor.
-- Sorun: Satis yapildiktan sonra ayni/yakin fiyattan tekrar alim yapiliyor (AktifMi=0 olunca kontrol kaciyor).
-- Cozum: Bugun kapanan pozisyonlarin SatisFiyati da marj araligi kontrolune dahil ediliyor.

ALTER PROC [dbo].[sel_hisseAlimKontrol]
    @HisseAdi varchar(50),
    @AlisFiyati decimal(18,2),
    @Marj decimal(18,2)
AS
BEGIN
    SELECT * FROM HisseHareket WITH(NOLOCK)
    WHERE HisseAdi = @HisseAdi
    AND (
        -- Mevcut: aktif pozisyon yakinlik kontrolu
        (AktifMi = 1
         AND AlisFiyati <= @AlisFiyati + @Marj
         AND AlisFiyati >= @AlisFiyati - @Marj)
        OR
        -- YENI: bugun satilan pozisyonlarin satis fiyati yakinlik kontrolu
        (AktifMi = 0
         AND SatisTarihi >= CAST(GETDATE() AS DATE)
         AND SatisFiyati <= @AlisFiyati + @Marj
         AND SatisFiyati >= @AlisFiyati - @Marj)
    )
END
