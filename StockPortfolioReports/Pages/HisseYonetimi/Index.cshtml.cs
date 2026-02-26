using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace StockPortfolioReports.Pages.HisseYonetimi
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Hisse> Hisseler { get; set; } = new();

        public async Task OnGetAsync()
        {
            Hisseler = await _context.Hisse
                .Where(h => h.HisseAdi != "Default")
                .OrderBy(h => h.HisseAdi)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostToggleAsync(long id, string field)
        {
            var hisse = await _context.Hisse.FindAsync(id);
            if (hisse == null) return NotFound();

            switch (field)
            {
                case "Aktif": hisse.Aktif = !hisse.Aktif; break;
                case "AlisAktif": hisse.AlisAktif = !(hisse.AlisAktif ?? false); break;
                case "SatisAktif": hisse.SatisAktif = !(hisse.SatisAktif ?? false); break;
            }

            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}
