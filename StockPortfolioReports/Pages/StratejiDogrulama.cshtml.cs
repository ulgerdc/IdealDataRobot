using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace StockPortfolioReports.Pages
{
    public class StratejiDogrulamaModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public StratejiDogrulamaModel(ApplicationDbContext context)
        {
            _context = context;
        }

        // Filtreler
        [BindProperty(SupportsGet = true)]
        public string? HisseFiltre { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? BaslangicTarih { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? BitisTarih { get; set; }

        // Ozet kartlar - Grid
        public decimal GridKar { get; set; }
        public int GridIslem { get; set; }
        public double GridKazancOrani { get; set; }
        public double GridOrtTutma { get; set; }

        // Ozet kartlar - Core
        public decimal CoreKar { get; set; }
        public int CoreIslem { get; set; }
        public double CoreKazancOrani { get; set; }
        public double CoreOrtTutma { get; set; }

        // Ozet kartlar - Butce Korumasi
        public int ButceEngel { get; set; }
        public DateTime? ButceEngelSonTarih { get; set; }

        // Ozet kartlar - Aktif Pozisyonlar
        public int AktifGrid { get; set; }
        public int AktifCore { get; set; }
        public decimal AktifGridTutar { get; set; }
        public decimal AktifCoreTutar { get; set; }
        public double OrtYas { get; set; }

        // Kural sayaclari
        public int GridSatisSayisi { get; set; }
        public int ZamanIndirimiSayisi { get; set; }
        public int CoreCikisSayisi { get; set; }
        public int ButceLimitiSayisi { get; set; }
        public int AtrGecisSayisi { get; set; }
        public int TrendKorumaSayisi { get; set; }

        // Grafikler
        public string GridCoreKarJson { get; set; } = "{}";
        public string OlayDagilimJson { get; set; } = "{}";

        // Tablolar
        public List<YasDagilimiDto> YasDagilimi { get; set; } = new();
        public List<HisseDetayDto> HisseDetaylari { get; set; } = new();
        public List<StratejiOlayDto> SonOlaylar { get; set; } = new();
        public List<string> HisseListesi { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Varsayilan tarihler
            BaslangicTarih ??= DateTime.Today.AddDays(-30);
            BitisTarih ??= DateTime.Today;
            var baslangic = BaslangicTarih.Value;
            var bitis = BitisTarih.Value.AddDays(1); // inclusive

            // Hisse listesi (dropdown icin)
            HisseListesi = await _context.HisseHareket
                .Select(h => h.HisseAdi)
                .Distinct()
                .OrderBy(h => h)
                .ToListAsync();

            // Kapanan islemler (tarih filtreli)
            var kapananQuery = _context.HisseHareket
                .Where(h => h.AktifMi == false && h.SatisTarihi != null
                    && h.SatisTarihi >= baslangic && h.SatisTarihi < bitis);

            if (!string.IsNullOrEmpty(HisseFiltre))
                kapananQuery = kapananQuery.Where(h => h.HisseAdi == HisseFiltre);

            var kapananlar = await kapananQuery.ToListAsync();

            // Grid performans
            var gridKapanan = kapananlar.Where(h => h.PozisyonTipi == 0).ToList();
            GridIslem = gridKapanan.Count;
            GridKar = gridKapanan.Sum(h => h.Kar ?? 0);
            var gridKarli = gridKapanan.Count(h => (h.Kar ?? 0) > 0);
            GridKazancOrani = GridIslem > 0 ? Math.Round((double)gridKarli / GridIslem * 100, 1) : 0;
            GridOrtTutma = gridKapanan.Count > 0
                ? Math.Round(gridKapanan.Average(h => (h.SatisTarihi!.Value - h.AlisTarihi).TotalDays), 1)
                : 0;

            // Core performans
            var coreKapanan = kapananlar.Where(h => h.PozisyonTipi == 1).ToList();
            CoreIslem = coreKapanan.Count;
            CoreKar = coreKapanan.Sum(h => h.Kar ?? 0);
            var coreKarli = coreKapanan.Count(h => (h.Kar ?? 0) > 0);
            CoreKazancOrani = CoreIslem > 0 ? Math.Round((double)coreKarli / CoreIslem * 100, 1) : 0;
            CoreOrtTutma = coreKapanan.Count > 0
                ? Math.Round(coreKapanan.Average(h => (h.SatisTarihi!.Value - h.AlisTarihi).TotalDays), 1)
                : 0;

            // Aktif pozisyonlar
            var aktifQuery = _context.HisseHareket.Where(h => h.AktifMi == true);
            if (!string.IsNullOrEmpty(HisseFiltre))
                aktifQuery = aktifQuery.Where(h => h.HisseAdi == HisseFiltre);

            var aktifler = await aktifQuery.ToListAsync();
            AktifGrid = aktifler.Count(h => h.PozisyonTipi == 0);
            AktifCore = aktifler.Count(h => h.PozisyonTipi == 1);
            AktifGridTutar = aktifler.Where(h => h.PozisyonTipi == 0).Sum(h => h.AlisFiyati * h.Lot);
            AktifCoreTutar = aktifler.Where(h => h.PozisyonTipi == 1).Sum(h => h.AlisFiyati * h.Lot);
            OrtYas = aktifler.Count > 0
                ? Math.Round(aktifler.Average(h => (DateTime.Now - h.AlisTarihi).TotalDays), 1)
                : 0;

            // RiskDetay sorgusu
            var riskQuery = _context.RiskDetay
                .Where(r => r.Tarih >= baslangic && r.Tarih < bitis);
            if (!string.IsNullOrEmpty(HisseFiltre))
                riskQuery = riskQuery.Where(r => r.HisseAdi == HisseFiltre);

            var riskDetaylar = await riskQuery.ToListAsync();

            // Butce korumasi
            var butceRisks = riskDetaylar.Where(r => r.Data != null && r.Data.Contains("Butce hard limiti")).ToList();
            ButceEngel = butceRisks.Count;
            ButceEngelSonTarih = butceRisks.OrderByDescending(r => r.Tarih).FirstOrDefault()?.Tarih;

            // Kural sayaclari
            var kategoriler = riskDetaylar.Select(r => KategorizEt(r.Data)).ToList();
            GridSatisSayisi = kategoriler.Count(k => k == "GridSatis");
            ZamanIndirimiSayisi = kategoriler.Count(k => k == "ZamanIndirimi");
            CoreCikisSayisi = kategoriler.Count(k => k == "CoreCikis");
            ButceLimitiSayisi = kategoriler.Count(k => k == "ButceLimiti");
            AtrGecisSayisi = kategoriler.Count(k => k == "AtrGecis");
            TrendKorumaSayisi = kategoriler.Count(k => k == "TrendKorumasi");

            // Grafik 1: Hisse bazli Grid vs Core kar (bar chart)
            var hisseGruplu = kapananlar
                .GroupBy(h => h.HisseAdi)
                .Select(g => new
                {
                    Hisse = g.Key,
                    GridKar = Math.Round(g.Where(h => h.PozisyonTipi == 0).Sum(h => h.Kar ?? 0), 2),
                    CoreKar = Math.Round(g.Where(h => h.PozisyonTipi == 1).Sum(h => h.Kar ?? 0), 2)
                })
                .OrderByDescending(x => x.GridKar + x.CoreKar)
                .ToList();

            GridCoreKarJson = JsonSerializer.Serialize(new
            {
                labels = hisseGruplu.Select(x => x.Hisse).ToArray(),
                gridData = hisseGruplu.Select(x => x.GridKar).ToArray(),
                coreData = hisseGruplu.Select(x => x.CoreKar).ToArray()
            });

            // Grafik 2: Olay dagilimi (doughnut)
            OlayDagilimJson = JsonSerializer.Serialize(new
            {
                labels = new[] { "Grid Satis", "Zaman Indirimi", "Core Cikis", "Butce Limiti", "ATR Gecis", "Trend Korumasi" },
                data = new[] { GridSatisSayisi, ZamanIndirimiSayisi, CoreCikisSayisi, ButceLimitiSayisi, AtrGecisSayisi, TrendKorumaSayisi }
            });

            // Yas dagilimi tablosu
            var yasGruplari = new[] { (0, 7), (7, 30), (30, 60), (60, 90), (90, int.MaxValue) };
            var yasLabels = new[] { "0-7 gun", "7-30 gun", "30-60 gun", "60-90 gun", "90+ gun" };

            for (int i = 0; i < yasGruplari.Length; i++)
            {
                var (min, max) = yasGruplari[i];
                var gridGrup = gridKapanan.Where(h =>
                {
                    var yas = (h.SatisTarihi!.Value - h.AlisTarihi).TotalDays;
                    return yas >= min && yas < max;
                }).ToList();
                var coreGrup = coreKapanan.Where(h =>
                {
                    var yas = (h.SatisTarihi!.Value - h.AlisTarihi).TotalDays;
                    return yas >= min && yas < max;
                }).ToList();

                YasDagilimi.Add(new YasDagilimiDto
                {
                    YasGrubu = yasLabels[i],
                    GridAdet = gridGrup.Count,
                    GridKar = gridGrup.Sum(h => h.Kar ?? 0),
                    CoreAdet = coreGrup.Count,
                    CoreKar = coreGrup.Sum(h => h.Kar ?? 0)
                });
            }

            // Hisse bazli detay tablosu
            var hisseConfig = await _context.Hisse
                .Where(h => h.HisseAdi != "Default")
                .ToDictionaryAsync(h => h.HisseAdi);

            var hisseGrupDetay = kapananlar
                .GroupBy(h => h.HisseAdi)
                .Select(g =>
                {
                    var gridItems = g.Where(h => h.PozisyonTipi == 0).ToList();
                    var coreItems = g.Where(h => h.PozisyonTipi == 1).ToList();
                    var allItems = g.ToList();
                    var config = hisseConfig.GetValueOrDefault(g.Key);

                    var hisseAktifler = aktifler.Where(a => a.HisseAdi == g.Key).ToList();
                    var aktifToplamLot = hisseAktifler.Sum(a => a.Lot);
                    var aktifToplamTutar = hisseAktifler.Sum(a => a.AlisFiyati * a.Lot);

                    return new HisseDetayDto
                    {
                        HisseAdi = g.Key,
                        GridIslem = gridItems.Count,
                        CoreIslem = coreItems.Count,
                        GridKar = gridItems.Sum(h => h.Kar ?? 0),
                        CoreKar = coreItems.Sum(h => h.Kar ?? 0),
                        OrtTutma = allItems.Count > 0
                            ? Math.Round(allItems.Average(h => (h.SatisTarihi!.Value - h.AlisTarihi).TotalDays), 1)
                            : 0,
                        ButceYuzde = config != null && config.Butce > 0
                            ? Math.Round((double)(aktifToplamTutar / config.Butce * 100), 1)
                            : 0,
                        OrtalamaMaliyet = aktifToplamLot > 0 ? Math.Round(aktifToplamTutar / aktifToplamLot, 2) : 0,
                        CoreOran = config?.CoreOran,
                        CoreMarj = config?.CoreMarj,
                        TrailingStopYuzde = config?.TrailingStopYuzde
                    };
                })
                .OrderByDescending(x => x.GridKar + x.CoreKar)
                .ToList();

            HisseDetaylari = hisseGrupDetay;

            // Son strateji olaylari (son 20)
            var sonOlayQuery = _context.RiskDetay
                .Where(r => r.Tarih >= baslangic && r.Tarih < bitis && r.Data != null);
            if (!string.IsNullOrEmpty(HisseFiltre))
                sonOlayQuery = sonOlayQuery.Where(r => r.HisseAdi == HisseFiltre);

            // Sadece strateji ile ilgili olaylari filtrele
            var sonOlaylarRaw = await sonOlayQuery
                .OrderByDescending(r => r.Tarih)
                .Take(200) // daha fazla cek, filtreleyecegiz
                .ToListAsync();

            SonOlaylar = sonOlaylarRaw
                .Select(r => new StratejiOlayDto
                {
                    Tarih = r.Tarih,
                    HisseAdi = r.HisseAdi ?? "",
                    OlayTipi = KategorizEt(r.Data),
                    Detay = r.Data ?? ""
                })
                .Where(o => o.OlayTipi != "Diger")
                .Take(20)
                .ToList();
        }

        private string KategorizEt(string? data)
        {
            if (data == null) return "Diger";
            if (data.Contains("Grid satis")) return "GridSatis";
            if (data.Contains("Zaman bazli")) return "ZamanIndirimi";
            if (data.Contains("Core satis")) return "CoreCikis";
            if (data.Contains("Butce hard limiti")) return "ButceLimiti";
            if (data.Contains("ATR marjina gecildi")) return "AtrGecis";
            if (data.Contains("Trend korumasi") || data.Contains("Gun ici") || data.Contains("dusus")) return "TrendKorumasi";
            return "Diger";
        }

        public static string OlayBadgeClass(string olayTipi) => olayTipi switch
        {
            "GridSatis" => "bg-primary",
            "ZamanIndirimi" => "bg-warning text-dark",
            "CoreCikis" => "bg-info text-dark",
            "ButceLimiti" => "bg-danger",
            "AtrGecis" => "bg-secondary",
            "TrendKorumasi" => "bg-dark",
            _ => "bg-light text-dark"
        };

        public static string OlayLabel(string olayTipi) => olayTipi switch
        {
            "GridSatis" => "Grid Satis",
            "ZamanIndirimi" => "Zaman Indirimi",
            "CoreCikis" => "Core Cikis",
            "ButceLimiti" => "Butce Limiti",
            "AtrGecis" => "ATR Gecis",
            "TrendKorumasi" => "Trend Korumasi",
            _ => "Diger"
        };

        public class YasDagilimiDto
        {
            public string YasGrubu { get; set; } = "";
            public int GridAdet { get; set; }
            public decimal GridKar { get; set; }
            public int CoreAdet { get; set; }
            public decimal CoreKar { get; set; }
        }

        public class HisseDetayDto
        {
            public string HisseAdi { get; set; } = "";
            public int GridIslem { get; set; }
            public int CoreIslem { get; set; }
            public decimal GridKar { get; set; }
            public decimal CoreKar { get; set; }
            public double OrtTutma { get; set; }
            public double ButceYuzde { get; set; }
            public decimal OrtalamaMaliyet { get; set; }
            public int? CoreOran { get; set; }
            public decimal? CoreMarj { get; set; }
            public decimal? TrailingStopYuzde { get; set; }
        }

        public class StratejiOlayDto
        {
            public DateTime? Tarih { get; set; }
            public string HisseAdi { get; set; } = "";
            public string OlayTipi { get; set; } = "";
            public string Detay { get; set; } = "";
        }
    }
}
