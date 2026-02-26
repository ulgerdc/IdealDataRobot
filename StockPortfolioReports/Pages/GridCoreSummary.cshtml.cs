using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace StockPortfolioReports.Pages
{
    public class GridCoreSummaryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GridCoreSummaryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public decimal GridToplamTutar { get; set; }
        public int GridToplamLot { get; set; }
        public decimal GridToplamKar { get; set; }
        public decimal CoreToplamTutar { get; set; }
        public int CoreToplamLot { get; set; }
        public decimal CoreToplamKar { get; set; }

        public List<HisseGridCoreDto> HisseBazli { get; set; } = new();
        public string YasDagilimJson { get; set; } = "{}";

        public async Task OnGetAsync()
        {
            var aktifler = await (from h in _context.HisseHareket
                                  join p in _context.Hisse on h.HisseAdi equals p.HisseAdi
                                  where h.AktifMi == true
                                  select new
                                  {
                                      h.HisseAdi,
                                      h.Lot,
                                      h.AlisFiyati,
                                      h.PozisyonTipi,
                                      h.AlisTarihi,
                                      PiyasaSatis = p.PiyasaSatis ?? 0
                                  }).ToListAsync();

            GridToplamLot = aktifler.Where(a => a.PozisyonTipi == 0).Sum(a => a.Lot);
            GridToplamTutar = aktifler.Where(a => a.PozisyonTipi == 0).Sum(a => a.AlisFiyati * a.Lot);
            GridToplamKar = aktifler.Where(a => a.PozisyonTipi == 0).Sum(a => (a.PiyasaSatis - a.AlisFiyati) * a.Lot);
            CoreToplamLot = aktifler.Where(a => a.PozisyonTipi == 1).Sum(a => a.Lot);
            CoreToplamTutar = aktifler.Where(a => a.PozisyonTipi == 1).Sum(a => a.AlisFiyati * a.Lot);
            CoreToplamKar = aktifler.Where(a => a.PozisyonTipi == 1).Sum(a => (a.PiyasaSatis - a.AlisFiyati) * a.Lot);

            HisseBazli = aktifler
                .GroupBy(a => a.HisseAdi)
                .Select(g => new HisseGridCoreDto
                {
                    HisseAdi = g.Key,
                    GridLot = g.Where(x => x.PozisyonTipi == 0).Sum(x => x.Lot),
                    CoreLot = g.Where(x => x.PozisyonTipi == 1).Sum(x => x.Lot),
                    GridTutar = g.Where(x => x.PozisyonTipi == 0).Sum(x => x.AlisFiyati * x.Lot),
                    CoreTutar = g.Where(x => x.PozisyonTipi == 1).Sum(x => x.AlisFiyati * x.Lot),
                    GridKar = g.Where(x => x.PozisyonTipi == 0).Sum(x => (x.PiyasaSatis - x.AlisFiyati) * x.Lot),
                    CoreKar = g.Where(x => x.PozisyonTipi == 1).Sum(x => (x.PiyasaSatis - x.AlisFiyati) * x.Lot),
                    PiyasaSatis = g.First().PiyasaSatis
                })
                .OrderByDescending(x => x.GridTutar + x.CoreTutar)
                .ToList();

            // Yas dagilimi
            var now = DateTime.Now;
            int g07 = 0, g730 = 0, g3060 = 0, g60p = 0;
            int c07 = 0, c730 = 0, c3060 = 0, c60p = 0;
            foreach (var a in aktifler)
            {
                var days = (now - a.AlisTarihi).Days;
                if (a.PozisyonTipi == 0)
                {
                    if (days <= 7) g07++; else if (days <= 30) g730++; else if (days <= 60) g3060++; else g60p++;
                }
                else
                {
                    if (days <= 7) c07++; else if (days <= 30) c730++; else if (days <= 60) c3060++; else c60p++;
                }
            }
            YasDagilimJson = JsonSerializer.Serialize(new
            {
                labels = new[] { "0-7 gun", "7-30 gun", "30-60 gun", "60+ gun" },
                grid = new[] { g07, g730, g3060, g60p },
                core = new[] { c07, c730, c3060, c60p }
            });
        }

        public class HisseGridCoreDto
        {
            public string HisseAdi { get; set; }
            public int GridLot { get; set; }
            public int CoreLot { get; set; }
            public decimal GridTutar { get; set; }
            public decimal CoreTutar { get; set; }
            public decimal GridKar { get; set; }
            public decimal CoreKar { get; set; }
            public decimal PiyasaSatis { get; set; }
            public decimal GridOrtMaliyet => GridLot > 0 ? Math.Round(GridTutar / GridLot, 2) : 0;
            public decimal CoreOrtMaliyet => CoreLot > 0 ? Math.Round(CoreTutar / CoreLot, 2) : 0;
        }
    }
}
