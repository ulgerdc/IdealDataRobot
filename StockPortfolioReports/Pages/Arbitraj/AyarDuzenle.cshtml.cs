using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace StockPortfolioReports.Pages.Arbitraj
{
    public class AyarDuzenleModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public AyarDuzenleModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public ArbitrajGelismisConfig Config { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Config = await _context.ArbitrajGelismis.FindAsync(id);
            if (Config == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Attach(Config).State = EntityState.Modified;
            await _context.SaveChangesAsync();
            return RedirectToPage("Ayarlar");
        }
    }
}
