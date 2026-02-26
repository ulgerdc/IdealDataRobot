using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace StockPortfolioReports.Pages
{
    public class HisseDetayModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public HisseDetayModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string Hisse { get; set; }

        public Hisse? HisseBilgi { get; set; }
        public decimal GerceklesmisKar { get; set; }
        public decimal PotansiyelKar { get; set; }
        public decimal KullanilanButce { get; set; }
        public double ButceYuzde { get; set; }
        public decimal OrtalamaMaliyet { get; set; }
        public decimal MaliyetFarkYuzde { get; set; }

        public List<HisseHareket> AcikPozisyonlar { get; set; } = new();
        public List<RiskDetay> SonRiskLoglar { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            if (string.IsNullOrEmpty(Hisse)) return RedirectToPage("/Index");

            HisseBilgi = await _context.Hisse.FirstOrDefaultAsync(h => h.HisseAdi == Hisse);
            if (HisseBilgi == null) return NotFound();

            // Gerceklesmis kar
            GerceklesmisKar = await _context.HisseHareket
                .Where(h => h.HisseAdi == Hisse && h.AktifMi == false && h.Kar != null)
                .SumAsync(h => (decimal?)h.Kar ?? 0);

            // Acik pozisyonlar
            AcikPozisyonlar = await _context.HisseHareket
                .Where(h => h.HisseAdi == Hisse && h.AktifMi == true)
                .OrderBy(h => h.AlisTarihi)
                .ToListAsync();

            // Potansiyel kar
            PotansiyelKar = AcikPozisyonlar.Sum(p => ((HisseBilgi.PiyasaSatis ?? 0) - p.AlisFiyati) * p.Lot);

            // Butce kullanimi
            KullanilanButce = AcikPozisyonlar.Sum(p => p.AlisFiyati * p.Lot);
            ButceYuzde = HisseBilgi.Butce > 0 ? (double)(KullanilanButce / HisseBilgi.Butce * 100) : 0;

            // Ortalama maliyet
            var toplamLot = AcikPozisyonlar.Sum(p => p.Lot);
            OrtalamaMaliyet = toplamLot > 0 ? Math.Round(KullanilanButce / toplamLot, 2) : 0;
            var piyasa = HisseBilgi.PiyasaSatis ?? 0;
            MaliyetFarkYuzde = OrtalamaMaliyet > 0 && piyasa > 0
                ? Math.Round((piyasa - OrtalamaMaliyet) / OrtalamaMaliyet * 100, 2) : 0;

            // Son 20 risk log
            SonRiskLoglar = await _context.RiskDetay
                .Where(r => r.HisseAdi == Hisse)
                .OrderByDescending(r => r.Tarih)
                .Take(20)
                .ToListAsync();

            return Page();
        }
    }
}
