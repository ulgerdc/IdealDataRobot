using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace StockPortfolioReports.Pages
{
    public class CoreEkleModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CoreEkleModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<string> HisseListesi { get; set; } = new();

        [BindProperty]
        [Required(ErrorMessage = "Hisse secimi zorunludur")]
        public string HisseAdi { get; set; } = "";

        [BindProperty]
        [Required(ErrorMessage = "Lot zorunludur")]
        [Range(1, int.MaxValue, ErrorMessage = "Lot en az 1 olmalidir")]
        public int Lot { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Alis fiyati zorunludur")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Fiyat 0'dan buyuk olmalidir")]
        public decimal AlisFiyati { get; set; }

        [BindProperty]
        public string? Aciklama { get; set; }

        public string? Mesaj { get; set; }
        public string? MesajTipi { get; set; } // success, danger

        public List<ManuelEmir> BekleyenEmirler { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadHisseListesi();
            await LoadBekleyenEmirler();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await LoadHisseListesi();
                await LoadBekleyenEmirler();
                return Page();
            }

            await _context.Database.ExecuteSqlRawAsync(
                "INSERT INTO ManuelEmir (HisseAdi, Lot, AlisFiyati, Durum, Aciklama) VALUES ({0}, {1}, {2}, 0, {3})",
                HisseAdi, Lot, (double)AlisFiyati, Aciklama ?? "");

            Mesaj = $"{HisseAdi} icin {Lot} lot, {AlisFiyati:N2} TL limit fiyatla emir olusturuldu. Bot calistiginda gerceklesecek.";
            MesajTipi = "success";

            await LoadHisseListesi();
            await LoadBekleyenEmirler();
            return Page();
        }

        public async Task<IActionResult> OnPostIptalAsync(long id)
        {
            await _context.Database.ExecuteSqlRawAsync(
                "EXEC upd_manuelEmir @Id={0}, @Durum=2", id);

            Mesaj = "Emir iptal edildi.";
            MesajTipi = "success";

            await LoadHisseListesi();
            await LoadBekleyenEmirler();
            return Page();
        }

        private async Task LoadBekleyenEmirler()
        {
            BekleyenEmirler = await _context.ManuelEmir
                .Where(e => e.Durum == 0)
                .OrderByDescending(e => e.OlusturmaTarihi)
                .ToListAsync();
        }

        private async Task LoadHisseListesi()
        {
            HisseListesi = await _context.Hisse
                .Where(h => h.HisseAdi != "Default" && h.Aktif)
                .Select(h => h.HisseAdi)
                .Distinct()
                .OrderBy(h => h)
                .ToListAsync();
        }
    }
}
