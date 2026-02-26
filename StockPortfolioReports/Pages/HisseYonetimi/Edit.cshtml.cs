using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace StockPortfolioReports.Pages.HisseYonetimi
{
    public class EditModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public EditModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public Hisse Hisse { get; set; }

        public async Task<IActionResult> OnGetAsync(long id)
        {
            Hisse = await _context.Hisse.FindAsync(id);
            if (Hisse == null) return NotFound();
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) return Page();

            _context.Attach(Hisse).State = EntityState.Modified;

            // PiyasaAlis/PiyasaSatis salt okunur - degistirme
            _context.Entry(Hisse).Property(x => x.PiyasaAlis).IsModified = false;
            _context.Entry(Hisse).Property(x => x.PiyasaSatis).IsModified = false;

            await _context.SaveChangesAsync();
            return RedirectToPage("Index");
        }
    }
}
