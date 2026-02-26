using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace StockPortfolioReports.Pages
{
    public class SoldReportsModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public SoldReportsModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<HisseHareket> Satilanlar { get; set; }

        public async Task OnGetAsync()
        {
            Satilanlar = await _context.HisseHareket
                .Where(x => x.AktifMi == false && x.SatisFiyati != null)
                .ToListAsync();
        }
    }

}
