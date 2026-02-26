using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace StockPortfolioReports.Pages.Arbitraj
{
    public class PozisyonlarModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PozisyonlarModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int AcikPozisyonSayisi { get; set; }
        public decimal AcikGirisTutari { get; set; }
        public int KapaliPozisyonSayisi { get; set; }
        public decimal ToplamKar { get; set; }
        public double KazancOrani { get; set; }

        public List<ArbitrajGelismisHareket> Pozisyonlar { get; set; } = new();

        public async Task OnGetAsync()
        {
            Pozisyonlar = await _context.ArbitrajGelismisHareket
                .OrderByDescending(h => h.AktifMi)
                .ThenByDescending(h => h.PozisyonTarihi)
                .ToListAsync();

            var aciklar = Pozisyonlar.Where(p => p.AktifMi).ToList();
            var kapalilar = Pozisyonlar.Where(p => !p.AktifMi).ToList();

            AcikPozisyonSayisi = aciklar.Count;
            AcikGirisTutari = aciklar.Sum(p => p.Bacak1GirisFiyat * p.Bacak1Lot + p.Bacak2GirisFiyat * p.Bacak2Lot);

            KapaliPozisyonSayisi = kapalilar.Count;
            ToplamKar = kapalilar.Where(p => p.Kar.HasValue).Sum(p => p.Kar ?? 0);

            if (KapaliPozisyonSayisi > 0)
                KazancOrani = (double)kapalilar.Count(p => p.Kar > 0) / KapaliPozisyonSayisi * 100;
        }
    }
}
