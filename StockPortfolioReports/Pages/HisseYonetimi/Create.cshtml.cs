using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace StockPortfolioReports.Pages.HisseYonetimi
{
    public class CreateModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CreateModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Hisse Hisse { get; set; }

        public async Task OnGetAsync()
        {
            var defaults = await _context.Hisse.FirstOrDefaultAsync(h => h.HisseAdi == "Default");
            Hisse = new Hisse
            {
                Butce = defaults?.Butce ?? 100000,
                AlimTutari = defaults?.AlimTutari ?? 5000,
                SonAlimTutari = defaults?.SonAlimTutari ?? 1000m,
                BaslangicKademe = defaults?.BaslangicKademe ?? 0.05m,
                MarjTipi = defaults?.MarjTipi ?? 0,
                Marj = defaults?.Marj ?? 1,
                CoreOran = defaults?.CoreOran ?? 30,
                CoreMarj = defaults?.CoreMarj ?? 100m,
                TrailingStopYuzde = defaults?.TrailingStopYuzde ?? 5m,
                ButceLimitYuzde = defaults?.ButceLimitYuzde ?? 80m,
                ButceAtrGecisYuzde = defaults?.ButceAtrGecisYuzde ?? 60.0,
                AtrPeriyot = defaults?.AtrPeriyot ?? 14,
                AtrCarpan = defaults?.AtrCarpan ?? 0.5m,
                AtrZamanDilimi = defaults?.AtrZamanDilimi ?? "D",
                Aktif = true,
                AlisAktif = true,
                SatisAktif = true,
                PortfoyTarihi = DateTime.Today,
                SonIslemTarihi = DateTime.Today
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Hisse.Add(Hisse);
            await _context.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}
