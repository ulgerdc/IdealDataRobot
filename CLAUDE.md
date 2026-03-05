# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

This is a Turkish stock trading bot system (MyRobot) that implements multiple automated trading strategies for BIST (Borsa Istanbul) and VIOP (futures) markets. The solution contains multiple projects for different trading strategies, testing, and reporting.

## Solution Structure

### Projects

1. **Denali** - Core trading library (.NET Framework 4.6)
   - Main strategy implementations
   - Database management
   - IdealPro trading platform integration
   - Target: Class library (outputs `ideal.dll`)

2. **AlimSatimRobotu** - Primary trading bot executable (.NET 6.0)
   - Console application that runs trading strategies
   - References: System.Data.SqlClient

3. **Denali.Test** - Strategy testing project (.NET Framework 4.6.2)
   - Console application for testing strategies
   - Contains mock implementations (SistemMock)
   - References: Denali.IdealLibConverter

4. **Denali.IdealLibConverter** - Helper library (.NET Framework 4.6)
   - Utilities for IdealPro platform integration

5. **Denali.Portfoy** - Portfolio visualization (.NET Framework 4.6)
   - WinForms application for portfolio management UI

6. **StockPortfolioReports** - Web-based reporting (.NET 8.0)
   - ASP.NET Core Razor Pages application
   - Entity Framework Core with SQL Server
   - Modern reporting interface

## Build Commands

```bash
# Build entire solution
msbuild MyRobot.sln /p:Configuration=Debug
msbuild MyRobot.sln /p:Configuration=Release

# Build specific project
msbuild Denali\Denali.csproj /p:Configuration=Debug
dotnet build AlimSatimRobotu\AlimSatimRobotu.csproj
dotnet build StockPortfolioReports\StockPortfolioReports.csproj

# Run projects
dotnet run --project AlimSatimRobotu\AlimSatimRobotu.csproj
dotnet run --project Denali.Test\Denali.Test.csproj
dotnet run --project StockPortfolioReports\StockPortfolioReports.csproj
```

## Database Configuration

- **SQL Server Database**: `robot`
- **Connection String** (in `Denali/DatabaseManager.cs`):
  ```
  Data Source=.;Initial Catalog=robot;User ID=sa;Password=1;...
  ```
- **Key Tables**: `Hisse`, `HisseHareket`, `HisseEmir`, `Arbitraj`, `ArbitrajHareket`, `SabahCoskusuHareket`, `RiskDetay`
- **Database Scripts**: `Denali/script.sql` contains all stored procedures and schema
- **Archival Process**:
  - Run `exec_arsiv.bat` to archive closed positions
  - Moves completed trades from `HisseHareket` to `HisseHareketOcak24` (or similar archive table)
  - Use `sqlcmd -S . -d Robot -U sa -P 1` for command-line database operations

### Key Stored Procedures

- `ins_hisse` - Insert/update stock configuration (upsert operation)
- `ins_HisseHareket` - Record buy (@Id=0) or sell (@Id=existing) operations, calculates profit
- `sel_hisseAlimKontrol` - Check if buy price is too close to existing position (within margin)
- `sel_hisseSatimKontrol` - Find positions eligible for sale (profit >= margin)
- `sel_hisseHareket` - Get aggregated position stats (open positions, total profit, etc.)
- `sel_arbitraj` / `ins_arbitrajhareket` - Manage arbitrage positions
- `sel_sabahcoskusu` / `sel_sabahcoskusukontrol` - Morning rush strategy queries
- `ins_RiskDetay` - Log risk management decisions for analysis
- `sel_portfoy` - Calculate portfolio P&L with cost basis
- `ins_arsiv` - Archive closed positions to historical table

## Core Architecture

### Trading Strategy Pattern

All strategies follow a similar pattern with a `Baslat(dynamic Sistem)` method that:
1. Validates system connection and trading hours
2. Fetches market data via `IdealManager`
3. Applies risk management via `RiskYoneticisi`
4. Executes trades and logs to database via `DatabaseManager`

### Key Components

**IdealManager** (`Denali/IdealManager.cs`)
- Bridge to IdealPro trading platform (uses `dynamic` types)
- Market data: `AlisFiyatiGetir()`, `SatisFiyatiGetir()`, `YuksekGunGetir()`, etc.
- Order execution: `Al()`, `Sat()`, `ViopAl()`, `ViopSat()`
- Time checks: `SaatiKontrolEt()`, `AlisSaatiKontrolEt()`, `SatisSaatiKontrolEt()`
- Index tracking: BIST100 (XU100), BIST30 (XU030), VIOP30 (VIP-X030)
- VIOP contract naming: Automatically generates contract codes like `VIP'F_ASELS0224` (futures for ASELS expiring February 2024)
- Price utilities: `MakeTwoDigit()` for rounding, `YuzdeFarkiHesapla()` for spread calculation

**DatabaseManager** (`Denali/DatabaseManager.cs`)
- ADO.NET SQL Server operations
- CRUD for stocks, positions, orders
- Strategy-specific queries via stored procedures
- Generic `MapFromDataReader<T>()` for object mapping

**RiskYoneticisi** (`Denali/RiskYoneticisi.cs`)
- Fibonacci-based position sizing: `{1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144}`
- Index correlation analysis (BIST100, BIST30, VIOP30)
- Budget percentage-based risk calculation
- Different evaluators: `EndeksDegerlendir()`, `SabahCoskusuDegerlendir()`, `ArbitrajDegerlendir()`

### Trading Strategies

1. **KademeStrateji** (Grid Trading)
   - Buy/sell at fixed price intervals (kademe = step/level)
   - Configurable margin types: kademe-based or per-mille (binde)
   - Fibonacci position sizing based on market conditions

2. **ArbitrajStrateji** (Arbitrage)
   - Exploits price differences between BIST stocks and VIOP futures
   - Opens positions when spread >= configured margin
   - Closes when prices converge

3. **SabahCoskusuStrateji** (Morning Rush)
   - Trades during late session (17:58-17:59)
   - Only enters when all indices positive
   - Targets end-of-day momentum

4. **GridStrateji** (Advanced Grid)
   - Grid-based buy levels with profit targets
   - Position management in database
   - Referenced in Denali.Test for testing

5. **BesSekizOnucStrateji** (5-8-13 Strategy)
   - Moving average crossover strategy (5, 8, 13 periods implied)
   - Similar structure to ArbitrajStrateji

6. **ManuelAnalizStrateji** & **TestStrateji**
   - Manual analysis and testing strategies

### Domain Model

**Hisse** (Stock Configuration)
- `HisseAdi` - Stock symbol (e.g., "ASELS", "AKBNK")
- `Butce` - Allocated budget for this stock
- `AlimTutari` - Amount per buy order (divided by risk divisor)
- `SonAlimTutari` - Final buy amount (unused in current code)
- `Marj` - Margin/profit target (interpretation depends on `MarjTipi`)
- `MarjTipi` - Margin calculation type:
  - `0` = Kademe (step-based): `Marj * (AlisFiyati - SatisFiyati)`
  - `1` = Binde (per-mille): `(Marj / 1000) * AlisFiyati`
- `AlisAktif` / `SatisAktif` - Enable/disable buying/selling for this stock
- `PiyasaAlis` / `PiyasaSatis` - Current market prices (updated each cycle)
- `IlkFiyat`, `BaslangicKademe`, `PortfoyTarihi`, `SonIslemTarihi` - Historical tracking
- **Default Configuration**: A special row with `HisseAdi='Default'` provides fallback values for unconfigured stocks

**HisseHareket** (Stock Position/Movement)
- Position tracking: `HisseAdi`, `Lot`, `AlisFiyati`, `SatisFiyati`
- `AktifMi` - 1 for open positions, 0 for closed
- `Kar` - Profit/loss: `Lot * (SatisFiyati - AlisFiyati)`
- `RobotAdi` - Which robot/strategy opened this position
- `AlisTarihi`, `SatisTarihi`, `Tarih` - Timestamps

**HisseEmir** (Stock Order)
- Order management with states via `HisseEmirDurum` enum
- Tracking: `AlisTarihi`, `SatisTarihi`, `Kar`
- Note: Less commonly used than HisseHareket

**Arbitraj** (Arbitrage Configuration)
- `HisseAdi` - Stock to arbitrage
- `ViopLot`, `BistLot` - Lot sizes for futures and stock
- `Marj` - Minimum spread percentage to enter trade
- `AktifMi` - Enable/disable this arbitrage pair

**ArbitrajHareket** (Arbitrage Position)
- Tracks simultaneous VIOP and BIST positions
- Records entry and exit prices for both legs
- `PozisyonTarih`, `KapanisTarihi` - Position lifecycle
- Profit calculation: `(ViopSatisFiyati - BistAlisFiyati) * BistLot`

## IdealPro Platform Integration

The system integrates with IdealPro trading platform using `dynamic` types. The `Sistem` object provides:
- `Sistem.BaglantiVar` - Connection status
- `Sistem.Saat` - Current market time
- `Sistem.AlisFiyat()`, `Sistem.SatisFiyat()` - Market prices
- `Sistem.EmirGonder()` - Submit orders
- `Sistem.YuzeyselVeriOku()` - Index data with `NetPerDay` percentage
- `Sistem.Name` - Robot identifier

## Position Management Workflow

1. **Opening a Position** (Buy):
   - Strategy checks `sel_hisseAlimKontrol` - ensures no position exists within margin range
   - `IdealManager.Al()` submits order to IdealPro
   - `DatabaseManager.HisseHareketEkleGuncelle()` with `Id=0` creates new row with `AktifMi=1`

2. **Closing a Position** (Sell):
   - Strategy checks `sel_hisseSatimKontrol` - finds positions where `AlisFiyati + Marj <= SatisFiyati`
   - `IdealManager.Sat()` submits sell order
   - `DatabaseManager.HisseHareketEkleGuncelle()` with existing `Id` updates row:
     - Sets `SatisFiyati`, calculates `Kar`, sets `AktifMi=0`, records `SatisTarihi`

3. **Archival**:
   - Periodically run `exec_arsiv.bat` (calls stored procedure `ins_arsiv`)
   - Copies closed positions (`AktifMi=0`) to archive table
   - Deletes from `HisseHareket` to keep table lean

## Development Notes

- **Mixed Framework Versions**: Core library uses .NET Framework 4.6, newer projects use .NET 6.0/8.0
- **Turkish Language**: Most code comments, variable names, and database objects are in Turkish
  - Key terms: `Hisse` (stock), `Alim/Alis` (buy), `Satim/Satis` (sell), `Kademe` (step/level), `Marj` (margin), `Kar` (profit), `Butce` (budget), `Lot` (lot size)
- **Dynamic Types**: IdealPro integration uses `dynamic` extensively - no compile-time type checking
- **Direct SQL**: Uses ADO.NET with stored procedures, not ORM (except StockPortfolioReports with EF Core)
- **Time Format**: Trading hours use 24-hour format strings (e.g., "10:00:00", "17:59:59")
- **Price Precision**: Prices rounded to 2 decimal places consistently
- **Symbol Format**:
  - BIST stocks: `IMKBH'ASELS` (note the apostrophe after exchange code)
  - VIOP futures: `VIP'F_ASELS0224` (symbol + MMYY expiration)
  - Indices: `IMKBX'XU100`, `IMKBX'XU030`, `VIP'VIP-X030`

## Testing

The `Denali.Test` project provides:
- `SistemMock` - Mock implementation of IdealPro `Sistem` object
- `GridStrateji.cs` - Standalone grid strategy example
- Test runner in `Program.cs` that simulates price movements

## Common Workflows

### Adding a New Strategy

1. Create new class in `Denali` project
2. Implement `public static void Baslat(dynamic Sistem)` method
3. Add connection and time validation
4. Use `IdealManager` for market data
5. Apply `RiskYoneticisi` for position sizing
6. Execute via `IdealManager.Al()/Sat()`
7. Record in database via `DatabaseManager`

### Configuring a Stock for Trading

1. Insert/update row in `Hisse` table:
   ```sql
   EXEC ins_hisse
     @HisseAdi='ASELS',
     @IlkFiyat=100.00,
     @Butce=50000.00,
     @BaslangicKademe=0.05,
     @AlimTutari=5000.00,
     @SonAlimTutari=1000.00,
     @PortfoyTarihi='2024-01-01',
     @SonIslemTarihi='2024-01-01',
     @Aktif=1
   ```
2. Set `AlisAktif=1` and/or `SatisAktif=1` to enable trading
3. Configure `MarjTipi` (0 or 1) and `Marj` value appropriately
4. For arbitrage, add row to `Arbitraj` table with `ViopLot`, `BistLot`, and `Marj`

### Testing a Strategy

1. Update `Denali.Test/Program.cs` with strategy call
2. Configure `SistemMock` with test data
3. Run: `dotnet run --project Denali.Test`
4. Monitor database for recorded positions

### Monitoring Portfolio

- Use `sel_portfoy` stored procedure to see P&L by stock
- Use `sel_portfoy_cikanlar` to see stocks fully exited with realized profit
- StockPortfolioReports web app provides visual dashboard

### Debugging

- Strategies use `Sistem.Debug()` and `Sistem.Mesaj()` for logging
- Database table `RiskDetay` stores risk calculation details via `DatabaseManager.RiskDetayEkle()`
- Most debug calls are commented out in production code
- Query `HisseHareket` table to trace position history: `SELECT * FROM HisseHareket WHERE HisseAdi='ASELS' ORDER BY AlisTarihi DESC`

---

## Grid Hibrit Strateji - Proje Ilerleme Durumu

**Hedef:** Grid'in kisa vadeli kazanc gucunu korurken, uzun vade birikim yapan ve sermayeyi koruyan hibrit strateji. 3 iyilestirme: Core birikimi, zaman bazli marj indirimi, butce hard limiti.

### Tamamlanan Fazlar

#### Faz 1: TradingView Pine Script (Backtest) - TAMAMLANDI (2026-02-22)
- **Dosya:** `grid_strategy_backtest.pine`
- Grid + Core + TimeDecay + BudgetLimit simulasyonu
- 3 toggle ile saf grid vs hibrit karsilastirmasi
- Dashboard: grid kar, core kar, acik pozisyon, butce kullanimi
- **Durum:** TradingView'da THYAO, GARAN, ASELS vb. uzerinde test edilmeli

#### Faz 2: Veritabani Degisiklikleri - TAMAMLANDI (2026-02-22)
- **Dosya:** `grid_hibrit_migration.sql`
- **Veritabaninda calistirildi:** Basarili
- Tablo degisiklikleri:
  - `HisseHareket`: +PozisyonTipi (0=Grid, 1=Core), +TepeNoktasi
  - `HisseHareketOcak24`: ayni kolonlar (arsiv uyumu)
  - `Hisse`: +CoreOran(30), +CoreMarj(100), +TrailingStopYuzde(5.0), +ButceLimitYuzde(60.0)
- Degistirilen SP'ler: ins_HisseHareket, sel_hisseSatimKontrol, sel_hisseHareket, ins_arsiv
- Yeni SP'ler: sel_hisseSatimKontrolZamanli, sel_hisseSatimKontrolCore, upd_hisseHareketTepeNoktasi

#### Faz 3: C# Model Degisiklikleri - TAMAMLANDI (2026-02-22)
- `Denali/Hisse.cs`: +CoreOran, +CoreMarj, +TrailingStopYuzde, +ButceLimitYuzde
- `Denali/HisseHareket.cs`: +PozisyonTipi, +TepeNoktasi
- `Denali/HissePozisyonlari.cs`: +GridAcikTutar, +CoreAcikTutar

#### Faz 4: RiskYoneticisi Butce Hard Limiti - TAMAMLANDI (2026-02-22)
- `Denali/RiskYoneticisi.cs`: percentage >= ButceLimitYuzde ise return 0 (alim engellenir)
- RiskDetay'a log yaziliyor

#### Faz 5: DatabaseManager Yeni Metodlar - TAMAMLANDI (2026-02-22)
- `Denali/DatabaseManager.cs`:
  - Degistirilen: HisseHareketEkleGuncelle (+PozisyonTipi), HisseSatimKontrol (+pozisyonTipi param)
  - Yeni: HisseSatimKontrolZamanli, CoreSatimKontrol, CoreTepeNoktasiGuncelle, HisseHareketLotGuncelle

#### Faz 6: KademeStratejiGelismis Yeniden Yazim - TAMAMLANDI (2026-02-22)
- `Denali/KademeStratejiGelismis.cs`: GelismisSatisYap 4 katmanli:
  1. Grid Satis + Core Ayirimi (%70 sat, %30 core tut)
  2. Zaman Bazli Marj Indirimi (30+gun: %75, 60+: %50, 90+: %25, 120+: basabas)
  3. Core Pozisyon Cikisi (hedef %10 veya trailing stop %5)
  4. Trailing Stop Tepe Guncelleme
- **Build:** Basarili (ideal.dll)

#### Faz 7: ATR Bazli Dinamik Grid Araligi - TAMAMLANDI (2026-02-23)
- **Pine Script:** ATR zaman dilimi secenegi (D=Gunluk, W=Haftalik), ATR+EMA kombinasyonu
- **SQL:** `grid_hibrit_migration_v2_atr.sql` - HisseGunlukVeri tablosu, ins_hisseGunlukVeri (upsert), sel_hisseATR (True Range hesaplama)
- **Hisse tablosu:** +AtrPeriyot(14), +AtrCarpan(0.50), +AtrZamanDilimi('D')
- **DatabaseManager:** +GunlukVeriKaydet(), +ATRHesapla()
- **KademeStratejiGelismis:** MarjTipi=2 icin ATR bazli marj hesaplama, her dongude gunluk veri kaydedilir
- **Veritabaninda calistirildi:** Basarili

### Backtest Sonuclari (2024-02-22 ~ 2026-02-22)

Parametreler: CoreOran=%15, ButceLimitYuzde=%80, ATR(14) x0.5, EMA 5/8/21

| Hisse | Saf Grid ROI | Hibrit Sabit ROI | Hibrit ATR ROI | ATR+EMA ROI | Kazanan |
|-------|-------------|-----------------|---------------|-------------|---------|
| EKGYO | %37.72 | **%40.95** | - | - | Hibrit Sabit |
| THYAO | %22.73 | **%23.64** | %17.96 | - | Hibrit Sabit |
| KCHOL | **%26.12** | %10.16 | - | - | Saf Grid |
| SASA | -%27.26 | -%31.97 | -%14.78 | **-%13.29** | ATR+EMA |

Onemli bulgular:
- Yukselis/yatay hisselerde (EKGYO, THYAO): Hibrit sabit marj en iyi
- Dusus trendinde (SASA): ATR+EMA kombinasyonu zarari yariladi (-%27 -> -%13)
- SASA saf grid %174.9 butce kullanimi — hibrit %82'de tuttu (sermaye korumasi)
- KCHOL'de butce limiti cok kisitlayici — hisse bazinda ButceLimitYuzde=90 onerisi

### Backtest Detay Sonuclari (2024-02-22 ~ 2026-02-22)

Test edilen yaklasimlar ve ROI sonuclari:

| Hisse | Saf Grid | Hibrit Sabit(%15/%80) | ATR+EMA | ADX Hisse | ADX Komb v1 | ADX Komb v2 |
|-------|----------|----------------------|---------|-----------|-------------|-------------|
| EKGYO(volatil) | %37.72 | **%40.95** | %18.06 | %32.98 | %31.15 | %29.6 |
| THYAO(karisik) | %22.73 | **%23.64** | %10.58 | %17.35 | %18.56 | %15.68 |
| KCHOL(yatay) | **%26.12** | %10.16 | %12.07 | %16.03 | %9.21 | %13.25 |
| SASA(dusus) | -%27.26 | -%31.97 | **-%13.29** | -%40.04 | -%22.84 | -%35.39 |

Temel bulgular:
- Hibrit Sabit: Yukselis/karisik hisselerde en iyi (EKGYO, THYAO)
- Saf Grid: Yatay hisselerde en iyi (KCHOL)
- ATR+EMA: Dusus trendinde en iyi koruma (SASA zarari yariladi)
- ADX otomatik mod algilama tek basina guvenilir degil (SASA'da yavas dusus = dusuk ADX = yanlis YATAY rejimi)

### Faz 8: Pragmatik Hibrit Yaklasim (Pine Script) - TAMAMLANDI (2026-02-23)

ADX yerine basit ve etkili 3 kurallik sistem:

**Dosya:** `grid_pragmatic_backtest.pine`

**Temel strateji:** Hibrit Sabit (tum hisselere varsayilan)
**3 kural:**
1. XU030 EMA8 < EMA21 oldugunda → alim kisitla (yarim lot, x0.5)
2. Butce %60+ oldugunda → ATR bazli grid genislet (sabit marjdan ATR'ye gec)
3. Core(%15) + TimeDecay + BudgetLimit(%80) her zaman aktif

**Ek ozellik:** Hisse bazli butce/alim tutari yapilandirmasi (syminfo.ticker ile otomatik algilama)

**Hisse bazli config:**
| Hisse | Butce | Alim Tutari |
|-------|-------|-------------|
| EKGYO | 100K | 5000 |
| THYAO | 100K | 5000 |
| KCHOL | 150K | 5000 |
| SASA  | 75K  | 3000 |

### Pragmatik Hibrit Backtest Sonuclari (2024-02-22 ~ 2026-02-22)

| Hisse | Saf Grid | Hibrit Sabit | ATR+EMA | ADX v2 | **Pragmatik** |
|-------|----------|-------------|---------|--------|---------------|
| EKGYO | %37.72 | **%40.95** | %18.06 | %29.6 | **%32.67** |
| THYAO | %22.73 | **%23.64** | %10.58 | %15.68 | **%19.2** |
| KCHOL | **%26.12** | %10.16 | %12.07 | %13.25 | **%12.72** |
| SASA | -%27.26 | -%31.97 | **-%13.29** | -%35.39 | **-%18.58** |

**Temel bulgular:**
- Pragmatik yaklasim tek bir ayarla en iyi degil ama **tum piyasa kosullarinda tutarli**
- SASA: -%18.58 (ADX v2'nin -%35.39'undan cok daha iyi, sermaye korunuyor)
- SASA butce %81'de tutuldu (eski ADX'te %154'tu!)
- KCHOL: 150K butce ile ATR gecis orani %75→%40'a dustu, trade sayisi 2x artti
- EKGYO/THYAO: Core katkisi onemli (EKGYO +6967 TL, THYAO +4065 TL)
- ADX v2'ye gore 4 hissenin hepsinde daha iyi veya esit

### Faz 9: Pragmatik Hibrit C# Implementasyonu - TAMAMLANDI (2026-02-23)

3 pragmatik kural KademeStratejiGelismis'e aktarildi:

**Kural 1 - XU030 EMA Filtresi:**
- `IdealManager.EndeksEMAKontrol(Sistem)`: IdealPro'nun `GrafikVerileriniOku` + `MA` fonksiyonlariyla dogrudan EMA8/EMA21 karsilastirmasi
- EMA8 < EMA21 → 0.5 (yarim lot), aksi halde 1.0
- Hata durumunda 1.0 doner (filtre devre disi)
- Warmup gerektirmez — IdealPro tarihsel veriyi zaten saglar

**Kural 2 - Butce ATR Gecisi:**
- `HesaplaMarj()` icerisinde butce yuzde kontrolu
- `ButceAtrGecisYuzde` (default 60) asildiginda MarjTipi otomatik ATR'ye (2) zorlanir
- RiskDetay'a log yazilir
- Hisse tablosuna `ButceAtrGecisYuzde` kolonu eklendi

**Kural 3 - Core + TimeDecay:**
- Degisiklik yok, Faz 6'da implement edilmisti

**SQL Migration:** `pragmatik_hibrit_migration.sql`
- `ALTER TABLE Hisse ADD ButceAtrGecisYuzde float NOT NULL DEFAULT 60`
- `UPDATE Hisse SET ButceLimitYuzde = 80 WHERE ButceLimitYuzde = 60`

**Degisen dosyalar:**
| Dosya | Degisiklik |
|-------|-----------|
| `Denali/Hisse.cs` | +ButceAtrGecisYuzde property |
| `Denali/IdealManager.cs` | +Bist30FiyatGetir, +EndeksEMAKontrol (IdealPro API ile) |
| `Denali/KademeStratejiGelismis.cs` | Baslat: EMA carpani, HesaplaMarj: ATR gecis |
| `pragmatik_hibrit_migration.sql` | Yeni kolon + default guncelleme |

### Faz 10: Faiz Bazli Adil Deger Hesaplamasi (Arbitraj Gelismis) - TAMAMLANDI (2026-02-25)

VIOP spread'in "ucuz mu pahali mi" degerlendirmesi icin vadeye kalan gun ve faiz oranina dayali adil deger hesaplamasi.

**Formul (basit faiz):**
- `AdilSpread% = YillikFaiz * (KalanGun / 365)`
- `NetPrim% = GercekSpread% - AdilSpread%`
- Giris: `NetPrim >= GirisMarji`, Cikis: `NetPrim <= CikisMarji`
- Takvim spread: `AdilSpread% = YillikFaiz * ((UzakKalanGun - YakinKalanGun) / 365)`

**SQL Migration:** `arbitraj_gelismis_faiz_migration.sql`
- `ArbitrajGelismis`: +YillikFaiz (default 50.0, satir bazinda MB faizi)
- `ArbitrajSpreadLog`: +AdilSpreadYuzde, +NetPrimYuzde, +KalanGun
- SP guncelleme: `sel_arbitrajGelismis` (+YillikFaiz), `ins_arbitrajSpreadLog` (+3 param)
- Ornek veri: YillikFaiz=50.0, YakinVadeSonGun='2026-04-30', UzakVadeSonGun='2026-06-30'
- **Veritabaninda calistirildi:** Basarili

**Degisen dosyalar:**
| Dosya | Degisiklik |
|-------|-----------|
| `Denali/ArbitrajGelismisConfig.cs` | +YillikFaiz property |
| `Denali/ArbitrajSpreadLog.cs` | +AdilSpreadYuzde, +NetPrimYuzde, +KalanGun |
| `Denali/ArbitrajStratejiGelismis.cs` | +KalanGunHesapla(), +AdilSpreadHesapla(), SpotViopIzle/TakvimSpreadIzle netPrim bazli sinyal |
| `Denali/DatabaseManager.cs` | ArbitrajSpreadLogKaydet +3 parametre |

**Mesaj ciktisi ornegi:**
```
ASELS [SV] BIST:67.50 VIOP(0426):70.25 Spread:4.07% Adil:4.11% Prim:-0.04% [30g]
ASELS [TS] Y:67.50(0426) U:69.25(0626) Spread:2.59% Contango Adil:8.22% Prim:-5.63% [Y:65g U:126g]
```

**Build:** Basarili (ideal.dll, 0 error)

### Faz 11: Pozisyon Yonetimi Altyapisi (Arbitraj Gelismis) - TAMAMLANDI (2026-02-25)

ArbitrajStratejiGelismis'e pozisyon acma/kapama lifecycle'i eklendi. Emir satirlari comment-out — izleme + pozisyon kaydi modunda calisiyor.

**SQL Migration:** `arbitraj_gelismis_pozisyon_migration.sql`
- `ins_arbitrajGelismisHareket` SP: `@Id=0` → INSERT (AktifMi=1), `@Id>0` → UPDATE (Kar hesapla, AktifMi=0)
- Kar hesaplama bacak yonune gore: ALIS bacagi `(Cikis-Giris)*Lot`, SATIS bacagi `(Giris-Cikis)*Lot`
- **Veritabaninda calistirildi:** Basarili

**Degisen dosyalar:**
| Dosya | Degisiklik |
|-------|-----------|
| `arbitraj_gelismis_pozisyon_migration.sql` | YENI - ins_arbitrajGelismisHareket SP |
| `Denali/DatabaseManager.cs` | +ArbitrajGelismisHareketGuncelle metodu |
| `Denali/ArbitrajStratejiGelismis.cs` | SpotViopIzle/TakvimSpreadIzle pozisyon lifecycle |

**SpotViopIzle akisi:**
1. Fiyat + spread + adilSpread + netPrim hesapla (mevcut)
2. `ArbitrajGelismisKontrol(hisse, 0)` ile aktif pozisyon kontrol
3. Pozisyon varsa → cikis kontrol (`netPrim <= CikisMarji`): CikisFiyat + CikisSpread kaydet, AktifMi=0
4. Pozisyon yoksa → giris kontrol (`netPrim >= GirisMarji`): Bacak1=BIST ALIS, Bacak2=VIOP SATIS, yeni hareket kaydet
5. Emir satirlari comment-out: `// IdealManager.Al(...)`, `// IdealManager.ViopSatVade(...)`

**TakvimSpreadIzle akisi:**
- Ayni lifecycle, ArbitrajTipi=1
- Contango: Bacak1=Yakin vade ALIS, Bacak2=Uzak vade SATIS
- Backwardation: Bacak1=Yakin vade SATIS, Bacak2=Uzak vade ALIS
- Her iki bacak VIOP (BistLot kullanilmaz)

**Mesaj formati:**
```
ASELS [SV] BIST:67.50 VIOP(0426):70.25 Spread:4.07% Adil:4.11% Prim:-0.04% [30g] [POZ: giris 3.50%]
ASELS [SV] ... Prim:1.50% [30g] >>> GIRIS YAPILDI <<<
ASELS [SV] ... Prim:-0.10% [30g] >>> CIKIS YAPILDI <<<
```

**Build:** Basarili (ideal.dll, 0 error)

### Faz 12: Pine Script Arbitraj Backtest - TAMAMLANDI (2026-02-25)

TradingView'da Spot-VIOP ve Takvim Spread arbitraj stratejisinin gorsel backtesti.

**Dosya:** `arbitraj_gelismis_backtest.pine`

**TradingView VIOP sembol formati:** `BIST:ASELSH2026` (hisse + ay kodu + yil)
- Ay kodlari: F=Ocak, G=Subat, H=Mart, J=Nisan, K=Mayis, M=Haziran, N=Temmuz, Q=Agustos, U=Eylul, V=Ekim, X=Kasim, Z=Aralik

**Ozellikler:**
- Spot-VIOP ve Takvim Spread modu (toggle)
- `request.security()` ile VIOP fiyat verisi
- Faiz bazli adil deger + net prim hesaplamasi
- Manuel pozisyon tracking (giris/cikis/kar)
- Gorsel: spread vs adil spread fill, net prim kalin cizgi, giris/cikis label, pozisyon arka plan
- Dashboard: fiyatlar, spread, prim, P&L, ROI, islem istatistikleri

**Kar hesabi (Spot-VIOP):**
- BIST kar: `(cikisBIST - girisBIST) x bistLot`
- VIOP kar: `(girisVIOP - cikisVIOP) x viopLot x viopCarpan`
- Default: bistLot=100, viopLot=1, viopCarpan=100 (dengeli 100 hisse maruz kalim)
- Pozisyon buyuklugu: ~31K TL BIST + ~5K TL VIOP teminat = ~36K TL

**Ilk backtest sonucu (ASELS, Mart 2026 vadesi):**
- Faiz: %37 (MB politika faizi)
- Giris Marji: %2.0, Cikis Marji: %0.5
- 4 islem, 4 kazancli (%100 win rate)
- Toplam Kar: 9,245 TL, ROI: %9.25

**Devam edecek iyilestirmeler:**
- Sermaye yeterliligi / teminat kontrolu eklenebilir
- Birden fazla hisse icin karsilastirmali test
- Komisyon/slippage simulasyonu

### Faz 13: Temettu Duzeltmesi - TAMAMLANDI (2026-02-25)

Temettu odeme gunu (ex-date) BIST fiyati temettu kadar duserken VIOP fiyati degismez. Bu yapay spread genislemesini onlemek icin adil deger formulune temettu duzeltmesi eklendi.

**Formul:**
- `AdilSpread% = Faiz × (KalanGun/365) - (TemettuTutar/SpotFiyat × 100)`
- Duzeltme sadece ex-date ONCESI uygulanir — ex-date gectikten sonra temettu sifirlanir

**SQL Migration:** `arbitraj_gelismis_temettu_migration.sql`
- `ArbitrajGelismis`: +TemettuTutar (float, default 0), +TemettuTarihi (smalldatetime, null)
- `sel_arbitrajGelismis` SP guncellendi (+2 kolon)
- **Veritabaninda calistirildi:** Basarili

**Degisen dosyalar:**
| Dosya | Degisiklik |
|-------|-----------|
| `arbitraj_gelismis_temettu_migration.sql` | YENI - temettu kolonlari + SP guncelleme |
| `Denali/ArbitrajGelismisConfig.cs` | +TemettuTutar, +TemettuTarihi property |
| `Denali/ArbitrajStratejiGelismis.cs` | AdilSpreadHesapla +temettu parametreleri, ex-date kontrolu |
| `arbitraj_gelismis_backtest.pine` | +Temettu input grubu, temettuDuzeltme() fonksiyonu |

**Kullanim:**
```sql
-- Temettu bilgisi girilince otomatik duzeltme yapilir
UPDATE ArbitrajGelismis SET TemettuTutar = 5.50, TemettuTarihi = '2026-03-15'
WHERE HisseAdi = 'ASELS' AND ArbitrajTipi = 0
-- Ex-date gectikten sonra sifirlamak opsiyonel (duzeltme zaten devre disi kalir)
```

**Build:** Basarili (ideal.dll, 0 error)

### Faz 14: Core Trailing Stop Zarar Korumasi - TAMAMLANDI (2026-02-25)

Core pozisyonlarin trailing stop ile alis fiyatinin altinda satilmasi sorunu duzeltildi.

**Sorun:** GZNMI hissesinde 3 Core pozisyon (PozisyonTipi=1) alis fiyatinin altinda satildi (toplam -32 TL zarar). Sebep: `sel_hisseSatimKontrolCore` SP'deki trailing stop kosulu:
- `TepeNoktasi > AlisFiyati * 1.01` — sadece %1 uzerine cikmasi yeterli (cok dusuk esik)
- `@SatisFiyati >= AlisFiyati` kontrolu yok — trailing stop tetiklendigi anda alis altinda bile satar

**Duzeltme:** `sel_hisseSatimKontrolCore` SP guncellendi:
1. Aktivasyon esigi: `1.01` → `1.05` (trailing stop ancak fiyat alisin %5 uzerine cikinca aktif)
2. Zarar korumasi: `AND @SatisFiyati >= AlisFiyati` eklendi (asla alis fiyatinin altinda satilmaz)

**Degisen dosyalar:**
| Dosya | Degisiklik |
|-------|-----------|
| `grid_hibrit_migration.sql` | sel_hisseSatimKontrolCore SP trailing stop kosulu guncellendi |
| DB: `sel_hisseSatimKontrolCore` | Canli SP guncellendi |

### Faz 15: Arbitraj Global Butce Yonetimi - TAMAMLANDI (2026-02-25)

114 config (55 Spot-VIOP + 59 Takvim Spread) icin global butce havuzu kontrolu eklendi.

**Mekanizma:**
- `ArbitrajGelismis` tablosuna `Butce` float kolonu eklendi (default 0)
- `_GLOBAL` satirinda `Butce=250000` (global butce havuzu)
- Giris oncesi: acik pozisyon toplam tutari + yeni pozisyon tutari > global butce ise giris engellenir
- Butce hesaplama `Baslat()` basinda 1 kez yapilir (2 DB sorgusu), her config icin tekrarlanmaz
- Giris yapilinca `acikTutar` yerinde guncellenir (ayni dongude birden fazla giris kontrolu icin)
- Engelleme RiskDetay'a ve ArbitrajSpreadLog AtlanmaAciklamasi'na yazilir

**SQL Migration:** `arbitraj_butce_migration.sql`
- `ALTER TABLE ArbitrajGelismis ADD Butce float NOT NULL DEFAULT 0`
- `_GLOBAL` satiri: HisseAdi='_GLOBAL', ArbitrajTipi=-1, AktifMi=0, Butce=250000
- `sel_arbitrajGelismis` SP: +Butce kolonu

**Degisen dosyalar:**
| Dosya | Degisiklik |
|-------|-----------|
| `arbitraj_butce_migration.sql` | YENI — Butce kolonu + _GLOBAL satiri + SP guncelleme |
| `Denali/ArbitrajGelismisConfig.cs` | +Butce property |
| `Denali/DatabaseManager.cs` | +ArbitrajGelismisGlobalButceGetir(), +ArbitrajGelismisAcikTutar() |
| `Denali/ArbitrajStratejiGelismis.cs` | Baslat: butce hesapla, SpotViopIzle/TakvimSpreadIzle: +globalButce/acikTutar param, giris oncesi butce kontrol |
| `StockPortfolioReports/Class.cs` | ArbitrajGelismisConfig +Butce |
| `StockPortfolioReports/Pages/Arbitraj/Index.cshtml` + `.cs` | Butce ozet karti (kullanilan/kalan/yuzde + progress bar) |
| `StockPortfolioReports/Pages/Arbitraj/Ayarlar.cshtml` + `.cs` | Global butce karti + duzenleme formu |

**Build:** Basarili (ideal.dll 0 error, StockPortfolioReports 0 error)

### Faz 16: Momentum Overnight Strateji (YutanMum) - TAMAMLANDI (2026-02-25)

Mevcut YutanMum (Engulfing) altyapisina Momentum Overnight sinyal tipi eklendi. Aksam 17:55'te BIST100 tarama → momentum sinyali veren hisseleri batch olarak al → ertesi sabah sat.

**Backtest sonuclari (BIST30, 2024-01 ~ 2026-02):**
- Momentum: ROI %80.42, PF 4.39, Win %75.7
- VWAP: ROI %77.59, PF 4.97, Win %76.5
- Engulfing: ROI ~%38, PF ~4.79

**Momentum sinyal kosullari (hisse bazinda):**
1. Close zirveye yakin: `(C-L)/(H-L) >= 0.80` (CloseThreshold)
2. Gun ici momentum: `(C-O)/O*100 >= %1.0` (MinMomentum)
3. Hacim artisi: `V > V[1] * MinHacimCarpani`
4. Dunden yukari: `C > C[1]`
5. XU100 pozitif: `Bist100EndeksYuzde > 0`

**OvernightMod:** Batch ertesi gun acilista (10:01) otomatik satilir. Kapaliyken mevcut KAR_ALDI/MAX_GUN mantigi korunur.

**SQL Migration:** `yutan_mum_momentum_migration.sql`
- `YutanMumConfig`: +SinyalTipi(1), +CloseThreshold(0.80), +MinMomentum(1.0), +OvernightMod(1)
- `YutanMumHareket`: +BugunYuksek, +BugunDusuk, +MomentumYuzde
- SP guncelleme: `sel_yutanMumConfig` (SELECT *), `ins_yutanMumHareket` (+3 param)
- Config guncelleme: SinyalTipi=1, IslemSaati='17:55', OvernightMod=1

**Degisen dosyalar:**
| Dosya | Degisiklik |
|-------|-----------|
| `yutan_mum_momentum_migration.sql` | YENI — YutanMumConfig + YutanMumHareket ALTER + SP guncelleme |
| `Denali/YutanMumConfig.cs` | +SinyalTipi, +CloseThreshold, +MinMomentum, +OvernightMod |
| `Denali/YutanMumHareket.cs` | +BugunYuksek, +BugunDusuk, +MomentumYuzde |
| `Denali/YutanMumStrateji.cs` | XU100 endeks filtresi, SinyalTipi switch (0=Engulfing/1=Momentum), OvernightMod satis |
| `Denali/IdealManager.cs` | +MomentumKontrol() — closePos, momentum%, hacim, dunden yukari kontrolleri |
| `Denali/DatabaseManager.cs` | YutanMumHareketEkle +BugunYuksek, BugunDusuk, MomentumYuzde |

**Strateji akisi:**
1. `Baslat()`: Baglanti + saat kontrol → config oku → aktif batch'leri kontrol et
2. XU100 endeks filtresi: `Bist100EndeksYuzde <= 0` ise tarama yapma
3. `YeniBatchOlustur()`: BIST100 tara → `SinyalTipi==1` ise `MomentumKontrol`, `==0` ise `YutanMumKontrol`
4. `AktifBatchleriKontrolEt()`: `OvernightMod && batch.Date < bugun` → OVERNIGHT satis

**Build:** Basarili (ideal.dll, 0 error)

### Faz 17: Portfoy Raporlama PozisyonTipi Ayrimi - TAMAMLANDI (2026-02-25)

sel_portfoy ve sel_portfoy_cikanlar SP'lerine Grid/Core (PozisyonTipi) ayrimi eklendi.

**SQL Migration:** `portfoy_pozisyon_tipi_migration.sql`
- `vHisseHareket` view: +PozisyonTipi kolonu (HisseEmir icin default 0=Grid)
- `sel_portfoy` SP: +PozisyonTipi GROUP BY, +@pozisyonTipi filtre parametresi
- `sel_portfoy_cikanlar` SP: ayni degisiklikler

**Kullanim:**
```sql
EXEC sel_portfoy                    -- tum pozisyonlar (Grid+Core ayri satirlarda)
EXEC sel_portfoy @pozisyonTipi=0    -- sadece Grid
EXEC sel_portfoy @pozisyonTipi=1    -- sadece Core
```

**Web UI (madde 17):** GridCoreSummary.cshtml zaten Grid/Core ayrimi yapiyor (LINQ ile). Ek degisiklik gerekmedi.

### Faz 18: YutanMum Overnight Zarar Grid Devri - TAMAMLANDI (2026-02-26)

OVERNIGHT satisinda zararda olan pozisyonlar, hisse KademeStrateji'de aktifse satilmak yerine HisseHareket tablosuna Grid pozisyonu olarak devredilir.

**Mantik (BatchSat icinde):**
- `neden == "OVERNIGHT"` VE `satisFiyati < hareket.AlisFiyati` VE `DatabaseManager.HisseGetir(hisseAdi) != null` ise:
  - `IdealManager.Sat()` CAGIRILMAZ — fiziksel satis yok
  - `HisseHareketEkleGuncelle` ile Grid pozisyon (PozisyonTipi=0, RobotAdi="YutanMum") olusturulur
  - YutanMum kaydı `AlisFiyati` ile kapatilir (kar=0)
- Karda olan OVERNIGHT pozisyonlar normal satilir
- KademeStrateji'de olmayan hisseler normal satilir
- KAR_ALDI ve MAX_GUN nedenleri etkilenmez

**Degisen dosyalar:**
| Dosya | Degisiklik |
|-------|-----------|
| `Denali/YutanMumStrateji.cs` | BatchSat: OVERNIGHT zararda + aktif hisse → Grid devir |

**Build:** Basarili (ideal.dll, 0 error)

### Faz 19: Gunluk Rapor + Core Rapor Sayfalari - TAMAMLANDI (2026-02-26)

StockPortfolioReports'a 2 yeni rapor sayfasi eklendi.

**GunlukRapor** — Gunun tum alis/satis aktivitesi
- Tarih secici (varsayilan: bugun) + hisse dropdown filtresi
- 4 ozet kart: Net K/Z (yesil/kirmizi), Alis (adet+TL), Satis (adet+TL), Hacim
- Hisse bazli detay tablosu: alis/satis adet/lot/tutar, kar, zarar, net K/Z + TOPLAM satiri
- Chart.js bar chart: hisse bazli kar (yesil) / zarar (kirmizi)
- Veri kaynagi: `vHisseHareket` view (arsivlenmis kayitlar dahil)

**CoreRapor** — Aktif Core pozisyonlarin maliyet ve K/Z durumu
- Hisse dropdown filtresi
- 4 ozet kart: Aktif Core (adet/lot), Toplam Maliyet, Guncel Deger, Acik K/Z (TL+%)
- Hisse bazli tablo: adet, lot, ort maliyet, guncel fiyat, maliyet TL, guncel deger, acik K/Z, K/Z %, ort tutma gun, kapanan adet, gerceklesen kar
- Chart.js bar chart: acik K/Z (yesil/kirmizi) + gerceklesen kar (mavi)
- Veri kaynagi: `HisseHareket` tablo (PozisyonTipi=1) + `Hisse` tablo (PiyasaSatis guncel fiyat)

**Teknik:** `VHisseHareket` entity + `vHisseHareket` view DbContext'e eklendi (HasNoKey, AktifMi int? — view CASE ifadesi int doner)

**Degisen dosyalar:**
| Dosya | Degisiklik |
|-------|-----------|
| `StockPortfolioReports/Class.cs` | +VHisseHareket entity, +DbSet, +OnModelCreating view mapping |
| `StockPortfolioReports/Pages/GunlukRapor.cshtml.cs` | YENI — vHisseHareket view uzerinden LINQ sorgulari |
| `StockPortfolioReports/Pages/GunlukRapor.cshtml` | YENI — Razor view: kartlar, tablo, bar chart |
| `StockPortfolioReports/Pages/CoreRapor.cshtml.cs` | YENI — Core pozisyon LINQ sorgulari, guncel fiyat karsilastirma |
| `StockPortfolioReports/Pages/CoreRapor.cshtml` | YENI — Razor view: kartlar, tablo, bar chart |
| `StockPortfolioReports/Pages/Shared/_Layout.cshtml` | Raporlar dropdown'una "Gunluk Rapor" + "Core Rapor" linkleri eklendi |

**Build:** Basarili (0 error)

### Faz 20: Manuel Core Emir Sistemi - TAMAMLANDI (2026-03-02)

Web arayuzunden manuel Core alis emri girme sistemi. Emir `ManuelEmir` tablosuna yazilir, KademeStratejiGelismis calistiginda piyasa fiyati limit fiyata esit veya altindaysa IdealPro'ya emir gonderir ve HisseHareket'e Core (PozisyonTipi=1) olarak kaydeder.

**Tablo: ManuelEmir**
- `Id` bigint IDENTITY PK
- `HisseAdi` varchar(20), `Lot` int, `AlisFiyati` float (limit fiyat)
- `Durum` int: 0=Bekliyor, 1=Gerceklesti, 2=Iptal
- `OlusturmaTarihi`, `GerceklesmeTarihi`, `GercekFiyat`, `Aciklama`

**SP'ler:** `sel_manuelEmir` (bekleyen emirler, hisse filtreli), `upd_manuelEmir` (durum guncelle)

**Akis:**
1. Web'den emir girilir → ManuelEmir tablosuna Durum=0 kayit
2. KademeStratejiGelismis.Baslat() her dongude `ManuelEmirKontrol()` cagirir (satis ile alis arasinda)
3. `alisFiyati <= emir.AlisFiyati` ise → IdealManager.Al() + HisseHareket Core kayit + ManuelEmir Durum=1
4. Web'den bekleyen emirler gorulebilir ve iptal edilebilir (Durum=2)

**SQL Migration:** `manuel_emir_migration.sql` — **Veritabaninda calistirildi:** Basarili

**Degisen dosyalar:**
| Dosya | Degisiklik |
|-------|-----------|
| `manuel_emir_migration.sql` | YENI — ManuelEmir tablosu + sel/upd SP'leri |
| `Denali/ManuelEmir.cs` | YENI — Entity class |
| `Denali/Denali.csproj` | +ManuelEmir.cs Compile Include |
| `Denali/DatabaseManager.cs` | +ManuelEmirGetir(), +ManuelEmirGuncelle() |
| `Denali/KademeStratejiGelismis.cs` | +ManuelEmirKontrol() metodu, Baslat() icinde cagriliyor |
| `StockPortfolioReports/Class.cs` | +ManuelEmir entity + DbSet |
| `StockPortfolioReports/Pages/CoreEkle.cshtml.cs` | ManuelEmir INSERT + bekleyen emirler listesi + iptal handler |
| `StockPortfolioReports/Pages/CoreEkle.cshtml` | Limit fiyat aciklamasi, bekleyen emirler tablosu, iptal butonu |

**Build:** Basarili (ideal.dll 0 error, StockPortfolioReports 0 error)

### Faz 21: Grid Sat-Al Dongusu Onleme - TAMAMLANDI (2026-03-03)

Satis yapildiktan sonra ayni/yakin fiyattan tekrar alim yapilmasi sorunu duzeltildi.

**Sorun:** `sel_hisseAlimKontrol` SP sadece `AktifMi=1` pozisyonlari kontrol ediyordu. Satis yapilinca pozisyon `AktifMi=0` olur ve alim kontrolunden kaciyordu. Ornek: TAVHL 305.50'den satildi → 13 dk sonra 305.50'den tekrar alindi.

**Cozum:** `sel_hisseAlimKontrol` SP'sine bugun satilan pozisyonlarin `SatisFiyati` yakinlik kontrolu eklendi:
- `AktifMi=0 AND SatisTarihi >= CAST(GETDATE() AS DATE)` → bugun kapanan pozisyonlar
- `SatisFiyati` ±marj araliginda ise alim engellenir
- Ertesi gun blok kalkar — fiyat hala oradaysa yeni grid giris olarak kabul edilir

**SQL Migration:** `grid_satal_onleme_migration.sql` — **Veritabaninda calistirildi:** Basarili

**Degisen dosyalar:**
| Dosya | Degisiklik |
|-------|-----------|
| `grid_satal_onleme_migration.sql` | YENI — sel_hisseAlimKontrol SP guncelleme |

C# kodu degismiyor — `DatabaseManager.HisseAlimKontrol()` ayni parametrelerle ayni SP'yi cagiriyor.

### Bekleyen Isler / Sonraki Adimlar

1. ~~PINE SCRIPT: Pragmatik hibrit yaklasim~~ — TAMAMLANDI
2. ~~C# Implementasyonu: 3 pragmatik kural~~ — TAMAMLANDI (Faz 9)
3. ~~EMA Filtresi C#: IdealPro API ile~~ — TAMAMLANDI (Faz 9)
4. ~~SQL Migration Calistir: pragmatik_hibrit_migration.sql~~ — TAMAMLANDI
5. ~~Faiz Bazli Adil Deger: ArbitrajStratejiGelismis~~ — TAMAMLANDI (Faz 10)
6. ~~Pozisyon Yonetimi: ArbitrajStratejiGelismis~~ — TAMAMLANDI (Faz 11)
7. ~~Arbitraj Pine Script Backtest~~ — TAMAMLANDI (Faz 12)
8. ~~Temettu Duzeltmesi~~ — TAMAMLANDI (Faz 13)
9. ~~Core Trailing Stop Zarar Korumasi~~ — TAMAMLANDI (Faz 14)
10. ~~Arbitraj Global Butce Yonetimi~~ — TAMAMLANDI (Faz 15)
11. ~~Momentum Overnight Strateji (YutanMum)~~ — TAMAMLANDI (Faz 16)
12. ~~SQL Migration Calistir: yutan_mum_momentum_migration.sql~~ — TAMAMLANDI
13. **Momentum Canli Test:** Robot calistir → 17:55'te tarama, sinyal varsa batch olustur, ertesi sabah OVERNIGHT satis
14. **Arbitraj Backtest Gelistirme:** Sermaye/teminat kontrolu, komisyon, coklu hisse karsilastirma
15. ~~Canli Test: KademeStratejiGelismis canli test~~ — TAMAMLANDI (2026-02-25, 8 hisse, 58 poz, Grid/Core ayrimi sorunsuz)
16. ~~Portfoy Raporlama: sel_portfoy PozisyonTipi ayrimi~~ — TAMAMLANDI (2026-02-25, vHisseHareket+sel_portfoy+sel_portfoy_cikanlar guncellendi)
17. ~~StockPortfolioReports: Web arayuzune Grid/Core ayrimi~~ — TAMAMLANDI (GridCoreSummary.cshtml zaten mevcut, LINQ ile Grid/Core ayrimi yapiliyor)
18. **Denali.Test:** SistemMock ile KademeStratejiGelismis unit test yaz
19. **KademeStrateji Gecisi:** Canli testte basarili olursa mevcut hisseleri KademeStratejiGelismis'e tasi
20. ~~Arbitraj Canli Test: ArbitrajStratejiGelismis canli izleme~~ — TAMAMLANDI (2026-02-25, 42 SV + 4 TS hisse, 57K+ log, sinyal/prim hesaplama sorunsuz, emir comment-out)
21. **Arbitraj Emir Acma:** Canli izleme basarili olursa comment-out emirleri ac
22. **Faiz Guncelleme:** MB faiz degisikliklerinde `UPDATE ArbitrajGelismis SET YillikFaiz=XX WHERE ...`

### Onemli Notlar

- `KademeStrateji.cs` DOKUNULMADI - kararli versiyon olarak calismaya devam ediyor
- Tum degisiklikler geriye uyumlu: PozisyonTipi default 0, yeni Hisse kolonlari default degerli
- Mevcut 133 acik pozisyon otomatik olarak Grid (PozisyonTipi=0) olarak isleniyor
- Hisse bazinda farkli CoreOran/CoreMarj ayarlanabilir (Default satirindan fallback)
- EMA filtresi IdealPro API uzerinden calisiyor — DB'de XU030 verisi biriktirmeye gerek yok
- IdealPro `GrafikVerileriniOku("IMKBX'XU030", "G")` gunluk bar verisi saglıyor
- ArbitrajGelismis'te YillikFaiz satir bazinda tutulur — MB faizi degisirse sadece yeni vade satirlari guncellenir, eski satirlar kendi faiziyle kalir
- TradingView VIOP verisi sadece mevcut/yakin vadeler icin mevcut — uzun donem backtest sinirli
- MB politika faizi: %37 (2026-02-25 itibariyle)
- Arbitraj global butce `_GLOBAL` satirinda tutulur — `UPDATE ArbitrajGelismis SET Butce=300000 WHERE HisseAdi='_GLOBAL'` ile degistirilebilir
- Butce kontrolu muhafazakar: tum bacaklarin tam tutarini kullanir (VIOP teminat orani ~%12-15 degil)
