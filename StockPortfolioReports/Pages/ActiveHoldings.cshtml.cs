using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace StockPortfolioReports.Pages
{
    public class ActiveHoldingsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ActiveHoldingsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ActiveHoldingDto> AktifHisseler { get; set; }
        public List<HisseOzetDto> HisseOzetleri { get; set; } = new();

        public async Task OnGetAsync()
        {
            AktifHisseler = await (from h in _context.HisseHareket
                                   join p in _context.Hisse on h.HisseAdi equals p.HisseAdi
                                   where h.AktifMi == true
                                   select new ActiveHoldingDto
                                   {
                                       HisseAdi = h.HisseAdi,
                                       Lot = h.Lot,
                                       AlisFiyati = h.AlisFiyati,
                                       PiyasaSatis = p.PiyasaSatis ?? 0,
                                       PotansiyelKar = ((p.PiyasaSatis ?? 0) - h.AlisFiyati) * h.Lot,
                                       AlisTarihi = h.AlisTarihi
                                   }).ToListAsync();

            HisseOzetleri = AktifHisseler
                .GroupBy(p => p.HisseAdi)
                .Select(g =>
                {
                    var toplamLot = g.Sum(p => p.Lot);
                    var toplamTutar = g.Sum(p => p.AlisFiyati * p.Lot);
                    var ortMaliyet = toplamLot > 0 ? Math.Round(toplamTutar / toplamLot, 2) : 0;
                    var piyasa = g.First().PiyasaSatis;
                    var karZarar = g.Sum(p => (p.PiyasaSatis - p.AlisFiyati) * p.Lot);
                    return new HisseOzetDto
                    {
                        HisseAdi = g.Key,
                        ToplamLot = toplamLot,
                        OrtalamaMaliyet = ortMaliyet,
                        PiyasaSatis = piyasa,
                        ToplamTutar = toplamTutar,
                        ToplamKarZarar = karZarar,
                        KarZararYuzde = toplamTutar > 0 ? Math.Round(karZarar / toplamTutar * 100, 2) : 0
                    };
                })
                .OrderBy(h => h.HisseAdi)
                .ToList();
        }

        public class ActiveHoldingDto
        {
            public string HisseAdi { get; set; }
            public int Lot { get; set; }
            public decimal AlisFiyati { get; set; }
            public decimal PiyasaSatis { get; set; }
            public decimal PotansiyelKar { get; set; }
            public DateTime AlisTarihi { get; set; }
        }

        public class HisseOzetDto
        {
            public string HisseAdi { get; set; }
            public int ToplamLot { get; set; }
            public decimal OrtalamaMaliyet { get; set; }
            public decimal PiyasaSatis { get; set; }
            public decimal ToplamTutar { get; set; }
            public decimal ToplamKarZarar { get; set; }
            public decimal KarZararYuzde { get; set; }
        }
    }
}
