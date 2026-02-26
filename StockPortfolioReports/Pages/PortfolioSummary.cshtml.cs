using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace StockPortfolioReports.Pages
{
    public class PortfolioSummaryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PortfolioSummaryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public decimal GerceklesmisKar { get; set; }
        public decimal PotansiyelKar { get; set; }
        public decimal ToplamKar { get; set; }

        public async Task OnGetAsync()
        {
            GerceklesmisKar = await _context.HisseHareket
                .Where(x => x.AktifMi == false && x.Kar != null)
                .SumAsync(x => x.Kar.Value);

            PotansiyelKar = await (from h in _context.HisseHareket
                                   join p in _context.Hisse on h.HisseAdi equals p.HisseAdi
                                   where h.AktifMi == true
                                   select (decimal?)((p.PiyasaSatis ?? 0) - h.AlisFiyati) * h.Lot).SumAsync() ?? 0;

            ToplamKar = GerceklesmisKar + PotansiyelKar;
        }
    }
}
