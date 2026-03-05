using Microsoft.EntityFrameworkCore;

namespace StockPortfolioReports
{
    public class HisseHareket
    {
        public long Id { get; set; }
        public string HisseAdi { get; set; }
        public int Lot { get; set; }
        public decimal AlisFiyati { get; set; }
        public decimal? SatisFiyati { get; set; }
        public bool? AktifMi { get; set; }
        public decimal? Kar { get; set; }
        public DateTime AlisTarihi { get; set; }
        public DateTime? SatisTarihi { get; set; }
        public string? RobotAdi { get; set; }
        public DateTime? Tarih { get; set; }
        public int PozisyonTipi { get; set; } // 0=Grid, 1=Core
        public decimal? TepeNoktasi { get; set; }
    }

    public class Hisse
    {
        public long Id { get; set; }
        public string HisseAdi { get; set; }
        public decimal IlkFiyat { get; set; }
        public decimal Butce { get; set; }
        public decimal BaslangicKademe { get; set; }
        public decimal AlimTutari { get; set; }
        public decimal? SonAlimTutari { get; set; }
        public byte? MarjTipi { get; set; }
        public int? Marj { get; set; }
        public bool Aktif { get; set; }
        public bool? AlisAktif { get; set; }
        public bool? SatisAktif { get; set; }
        public decimal? PiyasaAlis { get; set; }
        public decimal? PiyasaSatis { get; set; }
        public DateTime? PortfoyTarihi { get; set; }
        public DateTime? SonIslemTarihi { get; set; }
        public int? CoreOran { get; set; }
        public decimal? CoreMarj { get; set; }
        public decimal? TrailingStopYuzde { get; set; }
        public decimal? ButceLimitYuzde { get; set; }
        public double? ButceAtrGecisYuzde { get; set; }
        public int? AtrPeriyot { get; set; }
        public decimal? AtrCarpan { get; set; }
        public string? AtrZamanDilimi { get; set; }
    }

    public class RiskDetay
    {
        public string? HisseAdi { get; set; }
        public string? Data { get; set; }
        public DateTime? Tarih { get; set; }
    }

    public class HisseGunlukVeri
    {
        public long Id { get; set; }
        public string HisseAdi { get; set; }
        public DateTime Tarih { get; set; }
        public decimal Yuksek { get; set; }
        public decimal Dusuk { get; set; }
        public decimal Kapanis { get; set; }
    }

    public class ArbitrajGelismisConfig
    {
        public long Id { get; set; }
        public string HisseAdi { get; set; }
        public int ArbitrajTipi { get; set; }
        public string? YakinVadeKodu { get; set; }
        public string? UzakVadeKodu { get; set; }
        public DateTime YakinVadeSonGun { get; set; }
        public DateTime? UzakVadeSonGun { get; set; }
        public int BistLot { get; set; }
        public int ViopLot { get; set; }
        public double GirisMarji { get; set; }
        public double CikisMarji { get; set; }
        public bool AktifMi { get; set; }
        public double YillikFaiz { get; set; }
        public double TemettuTutar { get; set; }
        public DateTime? TemettuTarihi { get; set; }
        public double Butce { get; set; }
        public DateTime? Tarih { get; set; }
    }

    public class ArbitrajGelismisHareket
    {
        public long Id { get; set; }
        public long ArbitrajGelismisId { get; set; }
        public string RobotAdi { get; set; }
        public string HisseAdi { get; set; }
        public int ArbitrajTipi { get; set; }
        public string Bacak1Sembol { get; set; }
        public string Bacak1Yon { get; set; }
        public decimal Bacak1GirisFiyat { get; set; }
        public decimal? Bacak1CikisFiyat { get; set; }
        public int Bacak1Lot { get; set; }
        public string Bacak2Sembol { get; set; }
        public string Bacak2Yon { get; set; }
        public decimal Bacak2GirisFiyat { get; set; }
        public decimal? Bacak2CikisFiyat { get; set; }
        public int Bacak2Lot { get; set; }
        public decimal GirisSpreadYuzde { get; set; }
        public decimal? CikisSpreadYuzde { get; set; }
        public decimal? Kar { get; set; }
        public bool AktifMi { get; set; }
        public DateTime PozisyonTarihi { get; set; }
        public DateTime? KapanisTarihi { get; set; }
    }

    public class ArbitrajSpreadLog
    {
        public long Id { get; set; }
        public long ArbitrajGelismisId { get; set; }
        public string HisseAdi { get; set; }
        public int ArbitrajTipi { get; set; }
        public decimal Bacak1Fiyat { get; set; }
        public decimal Bacak2Fiyat { get; set; }
        public decimal SpreadYuzde { get; set; }
        public decimal SpreadTutar { get; set; }
        public bool GirisSinyali { get; set; }
        public bool CikisSinyali { get; set; }
        public string? AtlanmaAciklamasi { get; set; }
        public decimal? Bist100Yuzde { get; set; }
        public decimal? Bist30Yuzde { get; set; }
        public decimal? Viop30Yuzde { get; set; }
        public decimal? AdilSpreadYuzde { get; set; }
        public decimal? NetPrimYuzde { get; set; }
        public int? KalanGun { get; set; }
        public DateTime Tarih { get; set; }
    }

    public class YutanMumBatch
    {
        public long Id { get; set; }
        public string RobotAdi { get; set; }
        public DateTime BatchTarihi { get; set; }
        public int HisseSayisi { get; set; }
        public double ToplamAlimTutari { get; set; } // SQL float
        public double ToplamKar { get; set; } // SQL float
        public bool AktifMi { get; set; }
        public DateTime? KapanisTarihi { get; set; }
        public string? KapanisNedeni { get; set; }
    }

    public class YutanMumHareket
    {
        public long Id { get; set; }
        public long BatchId { get; set; }
        public string HisseAdi { get; set; }
        public int Lot { get; set; }
        public decimal AlisFiyati { get; set; } // SQL decimal(18,2)
        public decimal? SatisFiyati { get; set; }
        public decimal? Kar { get; set; }
        public DateTime AlisTarihi { get; set; }
        public DateTime? SatisTarihi { get; set; }
        public bool AktifMi { get; set; }
        public decimal? DunkuAcilis { get; set; }
        public decimal? DunkuKapanis { get; set; }
        public decimal? BugunAcilis { get; set; }
        public decimal? BugunKapanis { get; set; }
        public long? DunkuHacim { get; set; }
        public long? BugunHacim { get; set; }
        public double? BugunYuksek { get; set; } // SQL float
        public double? BugunDusuk { get; set; } // SQL float
        public double? MomentumYuzde { get; set; } // SQL float
    }

    public class ManuelEmir
    {
        public long Id { get; set; }
        public string HisseAdi { get; set; }
        public int Lot { get; set; }
        public double AlisFiyati { get; set; }
        public int Durum { get; set; }          // 0=Bekliyor, 1=Gerceklesti, 2=Iptal
        public DateTime OlusturmaTarihi { get; set; }
        public DateTime? GerceklesmeTarihi { get; set; }
        public double? GercekFiyat { get; set; }
        public string? Aciklama { get; set; }
    }

    // vHisseHareket view entity (HisseHareket + HisseHareketOcak24 + HisseEmir)
    public class VHisseHareket
    {
        public long Id { get; set; }
        public string HisseAdi { get; set; }
        public int Lot { get; set; }
        public decimal AlisFiyati { get; set; }
        public decimal? SatisFiyati { get; set; }
        public decimal? Kar { get; set; }
        public DateTime? SatisTarihi { get; set; }
        public DateTime AlisTarihi { get; set; }
        public int? AktifMi { get; set; }
        public int PozisyonTipi { get; set; }
    }

    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<HisseHareket> HisseHareket { get; set; }
        public DbSet<VHisseHareket> VHisseHareket { get; set; }
        public DbSet<Hisse> Hisse { get; set; }
        public DbSet<RiskDetay> RiskDetay { get; set; }
        public DbSet<HisseGunlukVeri> HisseGunlukVeri { get; set; }
        public DbSet<ArbitrajGelismisConfig> ArbitrajGelismis { get; set; }
        public DbSet<ArbitrajGelismisHareket> ArbitrajGelismisHareket { get; set; }
        public DbSet<ArbitrajSpreadLog> ArbitrajSpreadLog { get; set; }
        public DbSet<YutanMumBatch> YutanMumBatch { get; set; }
        public DbSet<YutanMumHareket> YutanMumHareket { get; set; }
        public DbSet<ManuelEmir> ManuelEmir { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RiskDetay>().HasNoKey().ToTable("RiskDetay");
            modelBuilder.Entity<HisseGunlukVeri>().ToTable("HisseGunlukVeri");
            modelBuilder.Entity<ArbitrajGelismisConfig>().ToTable("ArbitrajGelismis");
            modelBuilder.Entity<VHisseHareket>().HasNoKey().ToView("vHisseHareket");
        }
    }
}
