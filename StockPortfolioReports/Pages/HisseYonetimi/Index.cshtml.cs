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

        [BindProperty(SupportsGet = true)]
        public string? HisseFiltre { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? AktifFiltre { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? AlisFiltre { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? SatisFiltre { get; set; }

        public List<Hisse> Hisseler { get; set; } = new();
        public List<string> HisseListesi { get; set; } = new();

        public async Task OnGetAsync()
        {
            HisseListesi = await _context.Hisse
                .Where(h => h.HisseAdi != "Default")
                .Select(h => h.HisseAdi)
                .Distinct()
                .OrderBy(h => h)
                .ToListAsync();

            var query = _context.Hisse.Where(h => h.HisseAdi != "Default");

            if (!string.IsNullOrEmpty(HisseFiltre))
                query = query.Where(h => h.HisseAdi == HisseFiltre);

            if (AktifFiltre == "aktif")
                query = query.Where(h => h.Aktif);
            else if (AktifFiltre == "pasif")
                query = query.Where(h => !h.Aktif);

            if (AlisFiltre == "aktif")
                query = query.Where(h => h.AlisAktif == true);
            else if (AlisFiltre == "pasif")
                query = query.Where(h => h.AlisAktif != true);

            if (SatisFiltre == "aktif")
                query = query.Where(h => h.SatisAktif == true);
            else if (SatisFiltre == "pasif")
                query = query.Where(h => h.SatisAktif != true);

            Hisseler = await query.OrderBy(h => h.HisseAdi).ToListAsync();
        }

        public async Task<IActionResult> OnPostToggleAsync(long id, string field, string? hisseFiltre, string? aktifFiltre, string? alisFiltre, string? satisFiltre)
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
            return RedirectToPage(new { HisseFiltre = hisseFiltre, AktifFiltre = aktifFiltre, AlisFiltre = alisFiltre, SatisFiltre = satisFiltre });
        }
    }
}
