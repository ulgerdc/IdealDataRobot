using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace StockPortfolioReports.Pages.Arbitraj
{
    public class AyarlarModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AyarlarModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<ArbitrajGelismisConfig> Configler { get; set; } = new();
        public double GlobalButce { get; set; }

        public async Task OnGetAsync()
        {
            var tumu = await _context.ArbitrajGelismis
                .OrderBy(c => c.HisseAdi)
                .ThenBy(c => c.ArbitrajTipi)
                .ToListAsync();

            var globalConfig = tumu.FirstOrDefault(c => c.HisseAdi == "_GLOBAL");
            GlobalButce = globalConfig?.Butce ?? 250000;

            Configler = tumu.Where(c => c.HisseAdi != "_GLOBAL").ToList();
        }

        public async Task<IActionResult> OnPostToggleAsync(long id)
        {
            var config = await _context.ArbitrajGelismis.FindAsync(id);
            if (config == null) return NotFound();

            config.AktifMi = !config.AktifMi;
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostButceAsync(double butce)
        {
            var globalConfig = await _context.ArbitrajGelismis
                .FirstOrDefaultAsync(c => c.HisseAdi == "_GLOBAL");

            if (globalConfig == null)
                return NotFound();

            globalConfig.Butce = butce;
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}
