using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace StockPortfolioReports.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public decimal GerceklesmisKar { get; set; }
        public decimal PotansiyelKar { get; set; }
        public decimal ToplamKar { get; set; }
        public decimal ToplamButce { get; set; }
        public decimal KullanilanButce { get; set; }
        public double ButceYuzde { get; set; }
        public int AcikPozisyonSayisi { get; set; }
        public int GridPozisyon { get; set; }
        public int CorePozisyon { get; set; }
        public decimal GridTutar { get; set; }
        public decimal CoreTutar { get; set; }

        public string KarTrendiJson { get; set; } = "{}";
        public string GridCoreDagilimJson { get; set; } = "{}";
        public string HisseKarJson { get; set; } = "{}";

        public List<SonIslemDto> SonIslemler { get; set; } = new();

        public async Task OnGetAsync()
        {
            // Gerceklesmis kar
            GerceklesmisKar = await _context.HisseHareket
                .Where(x => x.AktifMi == false && x.Kar != null)
                .SumAsync(x => (decimal?)x.Kar ?? 0);

            // Potansiyel kar
            PotansiyelKar = await (from h in _context.HisseHareket
                                   join p in _context.Hisse on h.HisseAdi equals p.HisseAdi
                                   where h.AktifMi == true
                                   select (decimal?)((p.PiyasaSatis - h.AlisFiyati) * h.Lot)).SumAsync() ?? 0;

            ToplamKar = GerceklesmisKar + PotansiyelKar;

            // Butce kullanimi
            ToplamButce = await _context.Hisse
                .Where(h => h.HisseAdi != "Default" && h.Aktif)
                .SumAsync(h => (decimal?)h.Butce) ?? 0;

            KullanilanButce = await _context.HisseHareket
                .Where(h => h.AktifMi == true)
                .SumAsync(h => (decimal?)(h.AlisFiyati * h.Lot)) ?? 0;

            ButceYuzde = ToplamButce > 0 ? (double)(KullanilanButce / ToplamButce * 100) : 0;

            // Acik pozisyonlar
            var acikPozisyonlar = await _context.HisseHareket
                .Where(h => h.AktifMi == true)
                .ToListAsync();

            AcikPozisyonSayisi = acikPozisyonlar.Count;
            GridPozisyon = acikPozisyonlar.Count(p => p.PozisyonTipi == 0);
            CorePozisyon = acikPozisyonlar.Count(p => p.PozisyonTipi == 1);
            GridTutar = acikPozisyonlar.Where(p => p.PozisyonTipi == 0).Sum(p => p.AlisFiyati * p.Lot);
            CoreTutar = acikPozisyonlar.Where(p => p.PozisyonTipi == 1).Sum(p => p.AlisFiyati * p.Lot);

            // Son 90 gun kar trendi
            var son90Gun = DateTime.Today.AddDays(-90);
            var gunlukKar = await _context.HisseHareket
                .Where(h => h.AktifMi == false && h.SatisTarihi != null && h.SatisTarihi >= son90Gun && h.Kar != null)
                .GroupBy(h => h.SatisTarihi!.Value.Date)
                .Select(g => new { Tarih = g.Key, Kar = g.Sum(x => x.Kar!.Value) })
                .OrderBy(x => x.Tarih)
                .ToListAsync();

            var kumulatif = 0m;
            var trendLabels = new List<string>();
            var trendData = new List<decimal>();
            foreach (var g in gunlukKar)
            {
                kumulatif += g.Kar;
                trendLabels.Add(g.Tarih.ToString("dd/MM"));
                trendData.Add(Math.Round(kumulatif, 2));
            }
            KarTrendiJson = JsonSerializer.Serialize(new { labels = trendLabels, data = trendData });

            // Grid vs Core dagilim
            GridCoreDagilimJson = JsonSerializer.Serialize(new
            {
                labels = new[] { "Grid", "Core" },
                data = new[] { Math.Round(GridTutar, 0), Math.Round(CoreTutar, 0) }
            });

            // Hisse bazli acik kar/zarar
            var hisseKar = await (from h in _context.HisseHareket
                                  join p in _context.Hisse on h.HisseAdi equals p.HisseAdi
                                  where h.AktifMi == true
                                  group new { h, p } by h.HisseAdi into g
                                  select new
                                  {
                                      Hisse = g.Key,
                                      Kar = g.Sum(x => ((x.p.PiyasaSatis ?? 0) - x.h.AlisFiyati) * x.h.Lot)
                                  })
                                 .OrderByDescending(x => x.Kar)
                                 .ToListAsync();

            HisseKarJson = JsonSerializer.Serialize(new
            {
                labels = hisseKar.Select(x => x.Hisse).ToArray(),
                data = hisseKar.Select(x => Math.Round(x.Kar, 0)).ToArray()
            });

            // Son 10 kapanan islem
            SonIslemler = await _context.HisseHareket
                .Where(h => h.AktifMi == false && h.SatisTarihi != null)
                .OrderByDescending(h => h.SatisTarihi)
                .Take(10)
                .Select(h => new SonIslemDto
                {
                    HisseAdi = h.HisseAdi,
                    Lot = h.Lot,
                    AlisFiyati = h.AlisFiyati,
                    SatisFiyati = h.SatisFiyati ?? 0,
                    Kar = h.Kar ?? 0,
                    SatisTarihi = h.SatisTarihi!.Value
                })
                .ToListAsync();
        }

        public class SonIslemDto
        {
            public string HisseAdi { get; set; }
            public int Lot { get; set; }
            public decimal AlisFiyati { get; set; }
            public decimal SatisFiyati { get; set; }
            public decimal Kar { get; set; }
            public DateTime SatisTarihi { get; set; }
        }
    }
}
