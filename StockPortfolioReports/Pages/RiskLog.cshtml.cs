using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace StockPortfolioReports.Pages
{
    public class RiskLogModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public RiskLogModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? HisseFiltre { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? AramaMetni { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? BaslangicTarih { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? BitisTarih { get; set; }

        [BindProperty(SupportsGet = true)]
        public int Sayfa { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int RetentionGun { get; set; } = 7;

        public List<RiskDetay> Loglar { get; set; } = new();
        public List<string> Hisseler { get; set; } = new();
        public int ToplamSayfa { get; set; }
        public int ToplamKayit { get; set; }

        private const int SayfaBasi = 50;

        public async Task OnGetAsync()
        {
            if (Sayfa < 1) Sayfa = 1;
            if (RetentionGun < 1) RetentionGun = 7;

            Hisseler = await _context.Hisse
                .Where(h => h.HisseAdi != "Default")
                .Select(h => h.HisseAdi)
                .OrderBy(h => h)
                .ToListAsync();

            // Varsayilan: tarih filtresi yoksa retention gun kadar geriye bak
            var effectiveBaslangic = BaslangicTarih ?? DateTime.Today.AddDays(-RetentionGun);

            var query = _context.RiskDetay
                .Where(r => r.Tarih >= effectiveBaslangic);

            if (BitisTarih.HasValue)
                query = query.Where(r => r.Tarih <= BitisTarih.Value.AddDays(1));

            if (!string.IsNullOrEmpty(HisseFiltre))
                query = query.Where(r => r.HisseAdi == HisseFiltre);

            if (!string.IsNullOrEmpty(AramaMetni))
                query = query.Where(r => r.Data != null && r.Data.Contains(AramaMetni));

            ToplamKayit = await query.CountAsync();
            ToplamSayfa = Math.Max(1, (int)Math.Ceiling((double)ToplamKayit / SayfaBasi));

            Loglar = await query
                .OrderByDescending(r => r.Tarih)
                .Skip((Sayfa - 1) * SayfaBasi)
                .Take(SayfaBasi)
                .ToListAsync();
        }
    }
}
