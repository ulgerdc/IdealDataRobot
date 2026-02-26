using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace StockPortfolioReports.Pages
{
    public class DailySummaryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public DailySummaryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<DailySummaryDto> GunlukOzet { get; set; }

        public async Task OnGetAsync()
        {
            GunlukOzet = await _context.HisseHareket
                .GroupBy(x => new { Tarih = x.AlisTarihi.Date, x.HisseAdi })
                .Select(g => new DailySummaryDto
                {
                    Tarih = g.Key.Tarih,
                    HisseAdi = g.Key.HisseAdi,
                    ToplamAlis = g.Sum(x => x.Lot),
                    Satilan = g.Where(x => x.AktifMi == false).Sum(x => x.Lot),
                    Satilmamis = g.Where(x => x.AktifMi == true).Sum(x => x.Lot),
                    GunlukKar = g.Where(x => x.AktifMi == false && x.Kar != null).Sum(x => x.Kar.Value)
                })
                .OrderByDescending(x => x.Tarih)
                .ThenBy(x => x.HisseAdi)
                .ToListAsync();
        }

        public class DailySummaryDto
        {
            public DateTime Tarih { get; set; }
            public string HisseAdi { get; set; }
            public int ToplamAlis { get; set; }
            public int Satilan { get; set; }
            public int Satilmamis { get; set; }
            public decimal GunlukKar { get; set; }
        }
    }
}
