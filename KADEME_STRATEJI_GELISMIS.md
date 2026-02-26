# KademeStratejiGelismis - Hibrit Grid+Core Strateji Dokumantasyonu

**Son Guncelleme:** 2026-02-24
**Durum:** Canli calisiyor
**Dosya:** `Denali/KademeStratejiGelismis.cs`

---

## Genel Bakis

KademeStratejiGelismis, orijinal `KademeStrateji`'nin hibrit versiyonudur. Grid'in kisa vadeli kazanc gucunu korurken, uzun vade birikim yapan ve sermayeyi koruyan bir sistem sunar.

**Orijinal KademeStrateji DOKUNULMADI** - kararli versiyon olarak calismaya devam ediyor (`Denali/KademeStrateji.cs`).

### Temel Farklar (KademeStrateji vs KademeStratejiGelismis)

| Ozellik | KademeStrateji | KademeStratejiGelismis |
|---------|---------------|----------------------|
| Satis | Tek katman (marj karsilama) | 4 katmanli satis |
| Core birikim | Yok | Grid satis lotunun %X'i core olarak tutulur |
| Zaman indirimi | Yok | 30/60/90/120+ gun eski pozisyonlar indirimli satilir |
| Trailing stop | Yok | Core pozisyonlarda tepe fiyat takibi |
| Trend korumasi | Yok | BIST100/30 %5+ dususte alim durdurulur |
| EMA filtresi | Yok | XU030 EMA8 < EMA21 → yarim lot |
| ATR gecis | Yok | Butce %60+ oldugunda grid genisler |
| Butce limiti | Yok | %80 esiginde alim durur |
| Dinamik core | Yok | Momentum + gunluk % + endeks bazli core orani |
| Marj tipleri | 0 (kademe), 1 (binde) | 0, 1, 2 (ATR) |
| Minimum marj | %0.5 sabit | Yok (ATR fallback binde 7) |

---

## Mimari

### Ana Akis (`Baslat` metodu)

```
Baslat(Sistem, hisseAdi)
  │
  ├─ Baglanti ve fiyat kontrolu
  ├─ Hisse config yukle (Default fallback)
  ├─ Gunluk veri kaydet (ATR icin)
  ├─ Marj hesapla (MarjTipi + ATR gecis)
  │
  ├─ SATIS (oncelikli)
  │   └─ GelismisSatisYap() → 4 katmanli
  │
  └─ ALIS
      ├─ TrendKorumasiKontrol()
      ├─ RiskYoneticisi.RiskHesapla() → Fibonacci + butce limiti
      ├─ EndeksDegerlendir() → lot boleni
      ├─ EndeksEMAKontrol() → EMA carpani (Kural 1)
      └─ HisseAlimKontrol() + Al()
```

### 4 Katmanli Satis (`GelismisSatisYap`)

```
GelismisSatisYap(Sistem, hisseAdi, satisFiyati, marj, hisse)
  │
  ├─ KATMAN 1: Grid Satis + Core Ayirimi
  │   ├─ HisseSatimKontrol(hisseAdi, fiyat, marj, pozisyonTipi=0)
  │   ├─ DinamikCoreOranHesapla() → %0-70 arasi
  │   ├─ Grid lotu: item.Lot - coreLot → kapat (AktifMi=false)
  │   └─ Core lotu: yeni HisseHareket (PozisyonTipi=1) ac
  │
  ├─ KATMAN 2: Zaman Bazli Marj Indirimi
  │   ├─ HisseSatimKontrolZamanli(hisseAdi, fiyat, marj)
  │   └─ SP icerisinde: 30+gun %75, 60+ %50, 90+ %25, 120+ basabas
  │
  ├─ KATMAN 3: Core Pozisyon Cikisi
  │   ├─ CoreSatimKontrol(hisseAdi, fiyat, coreMarj, trailingStop)
  │   └─ SP: kar >= coreMarj VEYA tepe dusus >= trailingStop%
  │
  └─ KATMAN 4: Trailing Stop Tepe Guncelleme
      └─ CoreTepeNoktasiGuncelle(hisseAdi, guncelFiyat)
```

### Dinamik Core Oran Hesaplama (`DinamikCoreOranHesapla`)

3 faktore gore %0-70 arasi core orani belirlenir:

```
1. Momentum (5 gunluk):
   >= 10%  → %60 core
   >= 5%   → %50
   >= 2%   → %40
   >= 0%   → baseCore (config, default 30)
   >= -5%  → %15
   < -5%   → %5

2. Gunluk boost:
   Hisse gunu +3%+ → core oranina +10 ekle (max 70)

3. Endeks filtresi:
   EMA8 < EMA21 → core oranini x0.5 (yariyla)
```

### Pragmatik Hibrit 3 Kural

**Kural 1 - XU030 EMA Filtresi** (`IdealManager.EndeksEMAKontrol`):
- IdealPro `GrafikFiyatOku("IMKBX'XU030", "G", "Kapanis")` ile gunluk kapanislar alinir
- `Sistem.MA(kapanislar, "Exp", 8)` ve `Sistem.MA(kapanislar, "Exp", 21)` ile EMA hesaplanir
- EMA8 < EMA21 → return 0.5 (yarim lot), aksi halde 1.0
- Hata durumunda 1.0 (filtre devre disi)
- Warmup gerektirmez — IdealPro tarihsel veriyi saglıyor

**Kural 2 - Butce ATR Gecisi** (`HesaplaMarj` icerisinde):
- `ButceAtrGecisYuzde` (default 60) esigi kontrol edilir
- Acik pozisyon tutari / butce >= esik → MarjTipi otomatik 2 (ATR) yapilir
- Grid araligi genisler, daha az pozisyon acar
- RiskDetay'a "ATR marjina gecildi" loglaniyor

**Kural 3 - Core + TimeDecay + BudgetLimit** (her zaman aktif):
- Core ayirimi: Katman 1'de aktif
- TimeDecay: Katman 2'de aktif (30/60/90/120+ gun)
- BudgetLimit: RiskYoneticisi'nde %80 esiginde alim durur

### Trend Korumasi (`TrendKorumasiKontrol`)

3 seviye kontrol:

```
1. Endeks kontrolu:
   BIST100 < -5% VE BIST30 < -5% → alim durdur (return false)

2. Gun ici dusus:
   (yuksekGun - alisFiyati) / yuksekGun > %8 → alim durdur

3. Ilk fiyat dusus (uyari):
   (ilkFiyat - alisFiyati) / ilkFiyat > %30 → RiskDetay'a log yaz (alim devam eder)
```

### Marj Hesaplama (`HesaplaMarj`)

```
MarjTipi = 0 (Kademe): marj = Hisse.Marj * (alisFiyati - satisFiyati)
MarjTipi = 1 (Binde):  marj = (Hisse.Marj / 1000) * alisFiyati
MarjTipi = 2 (ATR):    marj = ATRHesapla() * AtrCarpan (fallback: binde 7)

+ Butce ATR gecisi: ButceAtrGecisYuzde asilirsa → MarjTipi zorla 2
```

---

## Veritabani Yapisi

### Tablo Degisiklikleri

**HisseHareket** (ek kolonlar):
| Kolon | Tip | Default | Aciklama |
|-------|-----|---------|----------|
| PozisyonTipi | int | 0 | 0=Grid, 1=Core |
| TepeNoktasi | decimal(18,2) | NULL | Core trailing stop icin en yuksek fiyat |

**HisseHareketOcak24** (arsiv): Ayni kolonlar eklendi.

**Hisse** (ek kolonlar):
| Kolon | Tip | Default | Aciklama |
|-------|-----|---------|----------|
| CoreOran | int | 30 | Dinamik core hesaplamada base deger (%) |
| CoreMarj | decimal(5,2) | 100 | Core cikis kar hedefi (binde) |
| TrailingStopYuzde | decimal(5,2) | 5.0 | Core trailing stop yuzde esigi |
| ButceLimitYuzde | decimal(5,2) | 80 | Hard butce limiti (alim durur) |
| ButceAtrGecisYuzde | float | 60 | Bu esikten sonra ATR marjina gecis |
| AtrPeriyot | int | 14 | ATR hesaplama periyodu |
| AtrCarpan | decimal(5,2) | 0.50 | ATR degerine carpan |
| AtrZamanDilimi | char(1) | 'D' | ATR zaman dilimi (D=gunluk, W=haftalik) |

**HisseGunlukVeri** (yeni tablo):
| Kolon | Tip | Aciklama |
|-------|-----|----------|
| Id | bigint (PK) | Otomatik |
| HisseAdi | nvarchar(50) | Hisse kodu |
| Tarih | date | Islem gunu |
| Yuksek | decimal(18,2) | Gun ici en yuksek |
| Dusuk | decimal(18,2) | Gun ici en dusuk |
| Kapanis | decimal(18,2) | Kapanis fiyati |

Index: `IX_HisseGunlukVeri_HisseAdi (HisseAdi, Tarih DESC)`

### Stored Procedure'ler

**Degistirilenler:**
| SP | Degisiklik |
|----|-----------|
| `ins_HisseHareket` | +@PozisyonTipi parametre, INSERT'e PozisyonTipi eklendi |
| `sel_hisseSatimKontrol` | +@PozisyonTipi parametre filtresi |
| `sel_hisseHareket` | GridAcikTutar/CoreAcikTutar hesaplama eklendi |
| `ins_arsiv` | PozisyonTipi ve TepeNoktasi arsiv tablosuna kopyalaniyor |

**Yeni SP'ler:**
| SP | Islem |
|----|-------|
| `sel_hisseSatimKontrolZamanli` | 30+ gunluk Grid pozisyonlar icin kademeli marj indirimi (%75→%50→%25→basabas) |
| `sel_hisseSatimKontrolCore` | Core pozisyonlari hedef kar veya trailing stop ile satar |
| `upd_hisseHareketTepeNoktasi` | Core pozisyonlarin TepeNoktasi'ni gunceller (sadece yukseliyorsa) |
| `ins_hisseGunlukVeri` | Gunluk fiyat verisi upsert (ATR hesaplama icin) |
| `sel_hisseATR` | True Range hesaplayip ATR ortalama doner |

### SQL Migration Dosyalari

Uygulanma sirasi:
1. `grid_hibrit_migration.sql` — Faz 2: Temel tablo degisiklikleri + SP'ler (UYGULANMIS)
2. `grid_hibrit_migration_v2_atr.sql` — Faz 7: ATR tablosu + SP'ler (UYGULANMIS)
3. `pragmatik_hibrit_migration.sql` — Faz 9: ButceAtrGecisYuzde kolonu (UYGULANMIS)

---

## C# Dosya Haritasi

| Dosya | Degisiklik | Detay |
|-------|-----------|-------|
| `Denali/KademeStratejiGelismis.cs` | **YENI** | Tum strateji mantigi: Baslat, GelismisSatisYap, DinamikCoreOranHesapla, HesaplaMarj, TrendKorumasiKontrol |
| `Denali/Hisse.cs` | DEGISTIRILDI | +CoreOran, +CoreMarj, +TrailingStopYuzde, +ButceLimitYuzde, +ButceAtrGecisYuzde, +AtrPeriyot, +AtrCarpan, +AtrZamanDilimi |
| `Denali/HisseHareket.cs` | DEGISTIRILDI | +PozisyonTipi, +TepeNoktasi |
| `Denali/HissePozisyonlari.cs` | DEGISTIRILDI | +GridAcikTutar, +CoreAcikTutar |
| `Denali/DatabaseManager.cs` | DEGISTIRILDI | Degistirilen: HisseHareketEkleGuncelle (+PozisyonTipi), HisseSatimKontrol (+pozisyonTipi param). Yeni: HisseSatimKontrolZamanli, CoreSatimKontrol, CoreTepeNoktasiGuncelle, HisseHareketLotGuncelle, GunlukVeriKaydet, MomentumHesapla, ATRHesapla |
| `Denali/IdealManager.cs` | DEGISTIRILDI | +Bist30FiyatGetir, +EndeksEMAKontrol (IdealPro GrafikFiyatOku + MA API) |
| `Denali/RiskYoneticisi.cs` | DEGISTIRILDI | ButceLimitYuzde esiginde return 0 (alim engeli), RiskDetay logu |
| `Denali/KademeStrateji.cs` | DOKUNULMADI | Orijinal strateji oldugu gibi calisiyor |

---

## RiskDetay Log Patternleri

Strateji olaylarini takip etmek icin `RiskDetay.Data` alaninda kullanilan text pattern'leri:

| Pattern | Tetikleyen | Aciklama |
|---------|-----------|----------|
| `"Grid satis: X lot, fiyat: Y, core ayrildi"` | Katman 1 | Normal grid satisi yapildi, core ayrildi |
| `"Zaman bazli satis: X lot, fiyat: Y"` | Katman 2 | 30+ gun eski pozisyon indirimli satildi |
| `"Core satis: X lot, fiyat: Y"` | Katman 3 | Core hedef veya trailing stop tetiklendi |
| `"DinamikCore: X% (mom5=Y%, daily=Z%, ema=W)"` | DinamikCoreOranHesapla | Core oran hesaplama detayi |
| `"Butce hard limiti: %X >= %Y - alim durduruldu"` | RiskYoneticisi | Butce esigi asildi, alim engellendi |
| `"Butce %X >= %Y - ATR marjina gecildi"` | HesaplaMarj | Sabit marjdan ATR'ye gecis |
| `"Trend korumasi - alim yapilmadi"` | TrendKorumasiKontrol | BIST100+30 < -5% |
| `"Gun ici %X dusus - alim yapilmadi"` | TrendKorumasiKontrol | Gun ici %8+ dusus |
| `"Ilk fiyattan %X dusus - dikkatli alim"` | TrendKorumasiKontrol | %30+ dusus uyarisi (alim devam eder) |
| `"YuksekGunGetir izin vermedi"` | RiskYoneticisi | Fiyat + marj >= gun yuksegi |

---

## Yapilandirma Rehberi

### Hisse Bazli Config

Her hisse icin `Hisse` tablosundaki parametreler ayarlanabilir. `Default` satiri fallback olarak kullanilir.

```sql
-- Ornek: THYAO icin ozel config
UPDATE Hisse SET
    CoreOran = 25,           -- %25 core ayir (default 30)
    CoreMarj = 80,           -- Core cikis hedefi binde 80 (default 100)
    TrailingStopYuzde = 4.0, -- Trailing stop %4 (default 5)
    ButceLimitYuzde = 85,    -- Hard limit %85 (default 80)
    ButceAtrGecisYuzde = 65, -- %65'te ATR'ye gec (default 60)
    MarjTipi = 1,            -- Binde marj
    Marj = 5                 -- Binde 5
WHERE HisseAdi = 'THYAO';

-- ATR marj kullanmak icin
UPDATE Hisse SET
    MarjTipi = 2,         -- ATR marj
    AtrPeriyot = 14,      -- 14 gunluk ATR
    AtrCarpan = 0.50,     -- ATR x 0.5
    AtrZamanDilimi = 'D'  -- Gunluk
WHERE HisseAdi = 'SASA';
```

### Onemli Esikler

| Parametre | Default | Aciklama |
|-----------|---------|----------|
| CoreOran | 30 | DinamikCoreOranHesapla'da base deger |
| CoreMarj | 100 | Binde 100 = %10 kar hedefi |
| TrailingStopYuzde | 5.0 | Tepe fiyattan %5 dususte sat |
| ButceLimitYuzde | 80 | %80'de alim tamamen durur |
| ButceAtrGecisYuzde | 60 | %60'ta sabit marjdan ATR'ye gecis |
| TrendKorumasiKontrol | -5%, 8%, 30% | Endeks dusus, gun ici dusus, ilk fiyat dusus |
| EMA carpani | 0.5 | XU030 EMA8<EMA21 durumunda lot carpani |
| Fibonacci serisi | 1,2,3,5,8,13,21,34,55,89,144 | Butce kullanim bazli marj genisletici |

---

## Backtest Sonuclari (2024-02-22 ~ 2026-02-22)

### Tum Yaklasimlar Karsilastirmasi

| Hisse | Saf Grid | Hibrit Sabit | ATR+EMA | ADX v2 | **Pragmatik** |
|-------|----------|-------------|---------|--------|---------------|
| EKGYO (volatil) | %37.72 | **%40.95** | %18.06 | %29.6 | %32.67 |
| THYAO (karisik) | %22.73 | **%23.64** | %10.58 | %15.68 | %19.2 |
| KCHOL (yatay) | **%26.12** | %10.16 | %12.07 | %13.25 | %12.72 |
| SASA (dusus) | -%27.26 | -%31.97 | **-%13.29** | -%35.39 | -%18.58 |

### Onemli Bulgular

- **Pragmatik yaklasim** tek ayarla en iyi degil ama **tum piyasa kosullarinda tutarli**
- SASA'da butce %81'de tutuldu (eski ADX %154'tu → sermaye korumasi basarili)
- Hibrit Sabit: Yukselis/karisik hisselerde en iyi (EKGYO, THYAO)
- Saf Grid: Yatay piyasada en iyi (KCHOL) - butce limiti kisitlayici olabiliyor
- ATR+EMA: Dusus trendinde en iyi koruma
- ADX otomatik mod algilama guvenilir degil (yavas dusus = dusuk ADX = yanlis YATAY rejimi)

### Pine Script Dosyalari

| Dosya | Icerik |
|-------|--------|
| `grid_strategy_backtest.pine` | Temel grid + hibrit backtest |
| `grid_pragmatic_backtest.pine` | Pragmatik 3 kurallik final backtest |

---

## Izleme ve Debug

### Strateji Dogrulama Raporu

Web arayuzu: `http://localhost:5000/StratejiDogrulama`

Sayfada gorulenler:
- Grid/Core performans kartlari (kar, islem, kazanc orani, ort tutma)
- Butce korumasi sayaci
- 6 kural sayaci badge (RiskDetay pattern matching)
- Grid vs Core kar bar chart + olay dagilimi doughnut
- Yas dagilimi tablosu (0-7, 7-30, 30-60, 60-90, 90+ gun)
- Hisse bazli detay tablosu
- Son 20 strateji olayi

### SQL Ile Hizli Kontrol

```sql
-- Aktif pozisyon dagilimi
SELECT PozisyonTipi, COUNT(*) Adet, SUM(Lot*AlisFiyati) Tutar
FROM HisseHareket WHERE AktifMi=1
GROUP BY PozisyonTipi;

-- Core pozisyonlar ve tepe noktalari
SELECT HisseAdi, Lot, AlisFiyati, TepeNoktasi,
       DATEDIFF(DAY, AlisTarihi, GETDATE()) Yas
FROM HisseHareket
WHERE AktifMi=1 AND PozisyonTipi=1
ORDER BY HisseAdi;

-- Son strateji olaylari
SELECT TOP 50 Tarih, HisseAdi, Data
FROM RiskDetay
WHERE Data LIKE '%Grid satis%' OR Data LIKE '%Core satis%'
   OR Data LIKE '%Zaman bazli%' OR Data LIKE '%Butce hard%'
   OR Data LIKE '%ATR marjina%' OR Data LIKE '%Trend korumasi%'
ORDER BY Tarih DESC;

-- Butce kullanim ozeti
SELECT h.HisseAdi, h.Butce,
       SUM(hh.Lot * hh.AlisFiyati) KullanilanButce,
       ROUND(SUM(hh.Lot * hh.AlisFiyati) / h.Butce * 100, 1) YuzdePct
FROM HisseHareket hh
JOIN Hisse h ON hh.HisseAdi = h.HisseAdi
WHERE hh.AktifMi = 1
GROUP BY h.HisseAdi, h.Butce
ORDER BY YuzdePct DESC;
```

---

## Bilinen Kisitlamalar

1. **DinamikCoreOranHesapla momentum hesabi** 5 gunluk HisseGunlukVeri'ye bagli - yeni eklenen hisseler icin ilk 5 gun momentum=0 doner (baseCore kullanilir)
2. **ATR fallback** binde 7 sabit - ATR verisi yoksa grid araligi sabit kalir
3. **KCHOL problemi**: Butce limiti %80 yatay piyasada kisitlayici, hisse bazinda %90 onerisi var ama henuz test edilmedi
4. **EMA filtresi** tum hisselere ayni uygulanir - sektore ozel ayrima yok
5. **Core ayirimi satis sirasinda yapilir** - alimda degil. Yani once tam lot alinir, satis geldiginde core ayrilir
6. **Trailing stop sadece core'da** - grid pozisyonlarda trailing stop yok
7. **sel_hisseSatimKontrolZamanli** SP icerisinde marj indirimi sabit katsayilarla (0.75, 0.50, 0.25, 0) - dinamik degil

---

## Gelecek Iyilestirme Fikirleri

### Kisa Vade (oncelikli)
- [ ] Canli test sonuclarini degerlendir, parametreleri ayarla
- [ ] KCHOL gibi yatay hisseler icin ButceLimitYuzde=90 test et
- [ ] sel_portfoy SP'sini PozisyonTipi ayrimini gosterecek sekilde guncelle
- [ ] Denali.Test'te SistemMock ile KademeStratejiGelismis unit test yaz
- [ ] Mevcut hisseleri KademeStratejiGelismis'e tasi (canli testte basariliysa)

### Orta Vade
- [ ] Hisse bazli EMA periyotlari (bazi sektorler icin EMA13/EMA34 daha iyi olabilir)
- [ ] Core orani icin sektor bazli base deger (bankacilik vs teknoloji farkli davranir)
- [ ] ATR zaman dilimi otomatik secimi (volatil hisseler icin haftalik daha iyi)
- [ ] Grid ve Core karlarini ayri kaydetme (HisseHareket.Kar alaninda Grid/Core ayrimi net degil)
- [ ] StockPortfolioReports web arayuzune Grid/Core ayrimi detay eklentileri

### Uzun Vade
- [ ] Machine learning ile core oran optimizasyonu (tarihsel veriye gore)
- [ ] Sektorel korelasyon analizi (BIST30 yerine sektor endeksi EMA filtresi)
- [ ] Dinamik trailing stop (volatiliteye gore trailing stop yuzdesini degistir)
- [ ] Multi-timeframe analiz (gunluk + haftalik ATR kombinasyonu)
- [ ] Kademeli pozisyon buyutme (DCA benzeri, dususte daha fazla al ama kontrollü)

---

## Hizli Referans: Metod → SP → Tablo Eslesmesi

```
KademeStratejiGelismis.Baslat()
  ├── DatabaseManager.HisseGetir()           → SELECT Hisse WHERE HisseAdi=X
  ├── DatabaseManager.GunlukVeriKaydet()     → SP: ins_hisseGunlukVeri → HisseGunlukVeri
  ├── HesaplaMarj()
  │   ├── DatabaseManager.AcikHissePozisyonlariGetir() → SP: sel_hissehareket
  │   └── DatabaseManager.ATRHesapla()       → SP: sel_hisseATR → HisseGunlukVeri
  │
  ├── GelismisSatisYap()
  │   ├── DatabaseManager.HisseSatimKontrol()          → SP: sel_hisseSatimKontrol
  │   ├── DinamikCoreOranHesapla()
  │   │   └── DatabaseManager.MomentumHesapla()        → SELECT HisseGunlukVeri
  │   ├── DatabaseManager.HisseHareketLotGuncelle()    → UPDATE HisseHareket SET Lot
  │   ├── DatabaseManager.HisseHareketEkleGuncelle()   → SP: ins_HisseHareket
  │   ├── DatabaseManager.HisseSatimKontrolZamanli()   → SP: sel_hisseSatimKontrolZamanli
  │   ├── DatabaseManager.CoreSatimKontrol()           → SP: sel_hisseSatimKontrolCore
  │   ├── DatabaseManager.CoreTepeNoktasiGuncelle()    → SP: upd_hisseHareketTepeNoktasi
  │   └── DatabaseManager.RiskDetayEkle()              → SP: ins_RiskDetay
  │
  ├── TrendKorumasiKontrol()
  │   └── IdealManager.Bist100/30EndeksYuzde()         → Sistem.YuzeyselVeriOku()
  │
  ├── RiskYoneticisi.RiskHesapla()
  │   └── DatabaseManager.AcikHissePozisyonlariGetir() → SP: sel_hissehareket
  │
  ├── RiskYoneticisi.EndeksDegerlendir()               → Sistem.YuzeyselVeriOku()
  ├── IdealManager.EndeksEMAKontrol()                  → Sistem.GrafikFiyatOku() + MA()
  ├── DatabaseManager.HisseAlimKontrol()               → SP: sel_hisseAlimKontrol
  ├── IdealManager.Al()                                → Sistem.EmirGonder()
  └── DatabaseManager.HisseGuncelle()                  → SP: upd_hisse
```
