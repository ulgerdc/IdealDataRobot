# KademeStrateji Geliştirmeleri

## 🔴 Tespit Edilen Problemler

### Problem 1: Eski Yüksek Fiyatlı Pozisyonlar Satılamıyor
**Durum:** BRSAN hissesinde 567 gün önce 665.50 TL'den alınan pozisyonlar, şu an 406 TL seviyesinde ve %64 kayıpla açık durumda.

**Neden:**
- Marj = 5 binde (%0.5) = ~2-3 TL kar hedefi
- Satış fiyatı: 665.50 + 2 = 667.50 TL olmalı
- Mevcut fiyat: 406 TL → **ASLA SATILAMAZ**

### Problem 2: Marj Çok Düşük
**Mevcut:** 5 binde (%0.5 kar hedefi)
- Sadece yatay/yükselen piyasalarda çalışır
- %5-10-20 düşüşlerde çalışmaz

**Örnek Hesaplama:**
```
400 TL'lik hisse için:
- Marj = (5 / 1000) * 400 = 2 TL
- Kar hedefi = %0.5
```

### Problem 3: Satış Marjı Asimetrik
- **Alışta:** Marj × risk çarpanı (1-8 arası fibonacci)
- **Satışta:** Marj sabit
- **Sorun:** Yüksek riskli dönemde alınan pozisyonlar daha zor satılıyor

### Problem 4: Trend Koruması Yok
- Hisse %50 düşmüş olsa bile alım yapıyor
- Moving average kontrolü yok
- Düşüş trendi tespiti yok

---

## ✅ Çözümler ve İyileştirmeler

### 1. Stop-Loss Mekanizması ✅
**Özellik:** Belirli yüzde kayıp durumunda pozisyonu zarar kes

```csharp
// %15 veya daha fazla kayıpla pozisyonları sat
private static List<HisseHareket> BulStopLossPozisyonlari(
    string hisseAdi, double satisFiyati, double stopLossYuzde)
{
    // SQL: WHERE ((AlisFiyati - SatisFiyati) / AlisFiyati * 100) >= StopLossYuzde
}
```

**Avantajlar:**
- Kayıpları sınırlar
- Sermayeyi korur
- Daha iyi pozisyonlara geçiş imkanı

**Konfigürasyon:**
- Hisse tablosuna `StopLossYuzde` alanı eklenebilir
- Varsayılan: %15
- Hisse bazında özelleştirilebilir

### 2. Zaman Bazlı Satış ✅
**Özellik:** X günden eski pozisyonları piyasadan sat

```csharp
// 90+ gün açık kalan ve zarar eden pozisyonları sat
private static List<HisseHareket> BulEskiPozisyonlari(
    string hisseAdi, double satisFiyati, int gunSayisi)
{
    // SQL: WHERE DATEDIFF(day, AlisTarihi, GETDATE()) >= GunSayisi
}
```

**Avantajlar:**
- "Ölü" pozisyonlardan kurtulur
- Portföyü temizler
- Yeni fırsatlar için sermaye serbest bırakır

**Konfigürasyon:**
- Varsayılan: 90 gün
- Hisse tablosuna `ZamanSatisGun` alanı eklenebilir

### 3. Trend Koruması ✅
**Özellik:** Düşüş trendinde alımı azalt/durdur

```csharp
private static bool TrendKorumasiKontrol(dynamic Sistem, Hisse hisse, double alisFiyati)
{
    // 1. Endeks kontrolü: Tüm endeksler %5+ düşüşte ise alma
    if (bist100Yuzde < -5.0 && bist30Yuzde < -5.0)
        return false;

    // 2. Gün içi kontrolü: %8+ düşmüşse alma
    double gunIciDusus = ((yuksekGun - alisFiyati) / yuksekGun) * 100;
    if (gunIciDusus > 8.0)
        return false;

    // 3. İlk fiyattan %30+ düşüşse dikkatli ol
    double ilkFiyatDusus = ((hisse.IlkFiyat - alisFiyati) / hisse.IlkFiyat) * 100;
    if (ilkFiyatDusus > 30.0)
        // Log tut ama engelleme (opsiyonel)

    return true;
}
```

**Avantajlar:**
- Düşüş trendinde büyük kayıpları önler
- "Ucuzladıkça al" tuzağından kaçınır
- Risk yönetimi gelişir

### 4. Gelişmiş Satış Mekanizması ✅
**Özellik:** 3 katmanlı satış stratejisi

```csharp
private static void GelismisSatisYap(...)
{
    // 1. Normal satış: Marj hedefine ulaşanlar
    var normalSatislar = DatabaseManager.HisseSatimKontrol(...);

    // 2. Stop-loss: %15+ kayıp olanlar
    var stopLossPozisyonlar = BulStopLossPozisyonlari(...);

    // 3. Zaman bazlı: 90+ gün eski olanlar
    var eskiPozisyonlar = BulEskiPozisyonlari(...);
}
```

**Öncelik Sırası:**
1. Önce karlı pozisyonları sat
2. Sonra stop-loss tetikleyenleri sat
3. En son eski pozisyonları sat

---

## 📊 Marj Önerileri

### Mevcut Durum Analizi

| Hisse | Mevcut Marj | Kar Hedefi | Sorun |
|-------|-------------|------------|-------|
| Çoğu hisse | 5 binde | %0.5 | ÇOK DÜŞÜK |
| BRSAN | 5 binde | ~2 TL | %64 kayıpla pozisyonlar var |
| SASA | 5 binde | ~0.15 TL | Satılamaz |

### Önerilen Marj Değerleri

| Hisse Volatilitesi | Önerilen Marj | Kar Hedefi | Açıklama |
|-------------------|---------------|------------|----------|
| Düşük (GARAN, AKBNK) | 15-20 binde | %1.5-2.0 | Bankalar, büyük kaplar |
| Orta (EREGL, ASELS) | 20-25 binde | %2.0-2.5 | Sanayi hisseleri |
| Yüksek (SASA, BRSAN) | 25-30 binde | %2.5-3.0 | Volatil hisseler |

### Marj Güncelleme Örnekleri

```sql
-- Tüm hisseleri 20 binde yap (dikkatli!)
UPDATE Hisse
SET Marj = 20
WHERE Marj < 20 AND HisseAdi <> 'Default'

-- Belirli hisseler için
UPDATE Hisse SET Marj = 25 WHERE HisseAdi = 'BRSAN'
UPDATE Hisse SET Marj = 30 WHERE HisseAdi = 'SASA'
UPDATE Hisse SET Marj = 15 WHERE HisseAdi IN ('GARAN', 'AKBNK', 'ISCTR')

-- Volatilite bazlı (örnek)
UPDATE Hisse SET Marj = 25 WHERE HisseAdi IN (
    SELECT HisseAdi FROM (
        SELECT HisseAdi,
               STDEV(AlisFiyati) / AVG(AlisFiyati) as Volatilite
        FROM HisseHareket
        WHERE AlisTarihi > DATEADD(month, -3, GETDATE())
        GROUP BY HisseAdi
        HAVING STDEV(AlisFiyati) / AVG(AlisFiyati) > 0.10
    ) AS VolatilHisseler
)
```

---

## 🚀 Kullanım Kılavuzu

### 1. Veritabanı Setup

```bash
sqlcmd -S . -d Robot -U sa -P 1 -i kademe_gelismis_setup.sql
```

Bu script:
- Gerekli alanları ekler (StopLossYuzde, ZamanSatisGun, GelismisStrateji)
- Mevcut sorunlu pozisyonları analiz eder
- Marj önerilerini gösterir

### 2. Test Hissesi Seçimi

```sql
-- Test için bir hisse seç (örnek: ARENA)
UPDATE Hisse
SET GelismisStrateji = 1,
    Marj = 20,  -- %2 kar hedefi
    StopLossYuzde = 15,  -- %15 stop-loss
    ZamanSatisGun = 90  -- 90 gün zaman bazlı satış
WHERE HisseAdi = 'ARENA'
```

### 3. Stratejiyi Çalıştırma

```csharp
// Denali.csproj dosyasına KademeStratejiGelismis.cs'yi ekle
// Compile: msbuild Denali\Denali.csproj

// Test:
KademeStratejiGelismis.Baslat(Sistem, "ARENA");
```

### 4. Sonuçları İzleme

```sql
-- Stop-loss tetikleyenleri gör
EXEC sel_portfoy @hisseAdi = 'ARENA'

-- Risk detaylarını kontrol et
SELECT TOP 20 * FROM RiskDetay
WHERE HisseAdi = 'ARENA'
ORDER BY Tarih DESC

-- Kapatılan pozisyonları gör
SELECT * FROM HisseHareket
WHERE HisseAdi = 'ARENA'
  AND AktifMi = 0
  AND SatisTarihi > DATEADD(day, -7, GETDATE())
ORDER BY SatisTarihi DESC
```

---

## 📈 Beklenen Sonuçlar

### Kısa Vadede (1-2 Hafta)
1. ✅ Eski yüksek fiyatlı pozisyonlar temizlenecek
2. ✅ Stop-loss ile büyük kayıplar önlenecek
3. ✅ Ortalama maliyet piyasaya yaklaşacak

### Orta Vadede (1-3 Ay)
1. ✅ Daha dengeli portföy yapısı
2. ✅ Daha az "ölü" pozisyon
3. ✅ Daha iyi sermaye rotasyonu

### Uzun Vadede (3+ Ay)
1. ✅ Sürdürülebilir karlılık
2. ✅ Düşük volatilite
3. ✅ Risk-ayarlı getiri artışı

---

## ⚠️ Dikkat Edilmesi Gerekenler

### 1. Marj Güncellemesi
- **DİKKAT:** Marj güncellemeleri mevcut açık pozisyonları etkiler
- Önce tek bir hisse ile test edin
- Sonuçları gözlemleyin
- Yavaşça diğer hisselere yayın

### 2. Stop-Loss Ayarı
- %15 varsayılan değer orta düzeyde korumadır
- Volatil hisseler için %20'ye çıkarılabilir
- Düşük volatiliteli hisseler için %10'a indirilebilir

### 3. Zaman Bazlı Satış
- 90 gün makul bir süredir
- Düşük frekanslı stratejiler için 120-180 gün olabilir
- Yüksek frekanslı stratejiler için 60 gün olabilir

### 4. Trend Koruması
- Çok agresif ayarlar alım fırsatlarını kaçırabilir
- Çok gevşek ayarlar koruma sağlamaz
- Test ederek fine-tuning yapın

---

## 🔧 Gelecek İyileştirmeler (TODO)

### Öncelik Yüksek
- [ ] Dinamik marj sistemi (pozisyon yaşına göre)
- [ ] Portföy seviyesinde risk limitleri
- [ ] Hisse bazında backtesting araçları

### Öncelik Orta
- [ ] Machine learning ile volatilite tahmini
- [ ] Korelasyon bazlı pozisyon limitleri
- [ ] Otomatik marj optimizasyonu

### Öncelik Düşük
- [ ] Web dashboard (real-time monitoring)
- [ ] Email/SMS alertleri
- [ ] Performans raporlama

---

## 📞 Destek ve Dokümantasyon

- **Kod:** `Denali/KademeStratejiGelismis.cs`
- **Setup:** `kademe_gelismis_setup.sql`
- **Analiz:** `analyze_kademe_problem.sql`
- **Ana Dokümantasyon:** `CLAUDE.md`

**Not:** Bu geliştirmeler geriye uyumludur. Eski `KademeStrateji` çalışmaya devam edecektir.
