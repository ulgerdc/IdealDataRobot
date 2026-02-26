using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace StockPortfolioReports.Pages.Arbitraj
{
    public class SpreadGecmisiModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SpreadGecmisiModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public long? ConfigId { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? Baslangic { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? Bitis { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Sayfa { get; set; } = 1;

        public List<ConfigSecenekDto> ConfigSecenekleri { get; set; } = new();
        public List<ArbitrajSpreadLog> SpreadLoglari { get; set; } = new();
        public int ToplamKayit { get; set; }
        public int SayfaSayisi { get; set; }
        public string GrafikJson { get; set; } = "{}";
        public double? SeciliGirisMarji { get; set; }
        public double? SeciliCikisMarji { get; set; }

        private const int SayfaBasinaKayit = 50;

        public async Task OnGetAsync()
        {
            // Config secenekleri
            var configs = await _context.ArbitrajGelismis.ToListAsync();
            ConfigSecenekleri = configs.Select(c => new ConfigSecenekDto
            {
                Id = c.Id,
                Label = $"{c.HisseAdi} ({(c.ArbitrajTipi == 0 ? "SV" : "TS")})"
            }).ToList();

            // Default tarih araligi: son 7 gun
            if (!Baslangic.HasValue) Baslangic = DateTime.Now.AddDays(-7);
            if (!Bitis.HasValue) Bitis = DateTime.Now;

            // Secili config marjlari
            if (ConfigId.HasValue)
            {
                var seciliConfig = configs.FirstOrDefault(c => c.Id == ConfigId.Value);
                if (seciliConfig != null)
                {
                    SeciliGirisMarji = seciliConfig.GirisMarji;
                    SeciliCikisMarji = seciliConfig.CikisMarji;
                }
            }

            // Query
            var query = _context.ArbitrajSpreadLog
                .Where(s => s.Tarih >= Baslangic.Value && s.Tarih <= Bitis.Value.AddDays(1));

            if (ConfigId.HasValue)
                query = query.Where(s => s.ArbitrajGelismisId == ConfigId.Value);

            ToplamKayit = await query.CountAsync();
            SayfaSayisi = (int)Math.Ceiling(ToplamKayit / (double)SayfaBasinaKayit);
            if (Sayfa < 1) Sayfa = 1;
            if (Sayfa > SayfaSayisi && SayfaSayisi > 0) Sayfa = SayfaSayisi;

            SpreadLoglari = await query
                .OrderByDescending(s => s.Tarih)
                .Skip((Sayfa - 1) * SayfaBasinaKayit)
                .Take(SayfaBasinaKayit)
                .ToListAsync();

            // Grafik verisi (tum tarih araligindaki loglar, max 2000)
            var grafikLogs = await query
                .OrderBy(s => s.Tarih)
                .Take(2000)
                .Select(s => new
                {
                    tarih = s.Tarih,
                    spread = s.SpreadYuzde,
                    adil = s.AdilSpreadYuzde,
                    prim = s.NetPrimYuzde,
                    giris = s.GirisSinyali,
                    cikis = s.CikisSinyali
                })
                .ToListAsync();

            GrafikJson = JsonSerializer.Serialize(new
            {
                labels = grafikLogs.Select(l => l.tarih.ToString("dd.MM HH:mm")),
                spread = grafikLogs.Select(l => l.spread),
                adil = grafikLogs.Select(l => l.adil),
                prim = grafikLogs.Select(l => l.prim),
                girisNoktalar = grafikLogs.Select((l, i) => l.giris ? new { x = i, y = l.prim } : null).Where(x => x != null),
                cikisNoktalar = grafikLogs.Select((l, i) => l.cikis ? new { x = i, y = l.prim } : null).Where(x => x != null),
                girisMarji = SeciliGirisMarji,
                cikisMarji = SeciliCikisMarji
            });
        }

        public class ConfigSecenekDto
        {
            public long Id { get; set; }
            public string Label { get; set; }
        }
    }
}
