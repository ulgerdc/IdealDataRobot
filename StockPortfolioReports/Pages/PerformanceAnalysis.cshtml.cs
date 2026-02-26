using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace StockPortfolioReports.Pages
{
    public class PerformanceAnalysisModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PerformanceAnalysisModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int ToplamIslem { get; set; }
        public double KazancOrani { get; set; }
        public decimal OrtalamaKar { get; set; }
        public double OrtalamaTutmaSuresi { get; set; }

        // Grid vs Core karsilastirma
        public int GridIslem { get; set; }
        public decimal GridToplam { get; set; }
        public double GridKazancOrani { get; set; }
        public int CoreIslem { get; set; }
        public decimal CoreToplam { get; set; }
        public double CoreKazancOrani { get; set; }

        public string KumulatifKarJson { get; set; } = "{}";
        public string AylikKarJson { get; set; } = "{}";

        public async Task OnGetAsync()
        {
            var kapananlar = await _context.HisseHareket
                .Where(h => h.AktifMi == false && h.Kar != null && h.SatisTarihi != null)
                .ToListAsync();

            ToplamIslem = kapananlar.Count;
            if (ToplamIslem > 0)
            {
                var kazancli = kapananlar.Count(k => k.Kar > 0);
                KazancOrani = Math.Round((double)kazancli / ToplamIslem * 100, 1);
                OrtalamaKar = Math.Round(kapananlar.Average(k => k.Kar!.Value), 2);
                OrtalamaTutmaSuresi = Math.Round(kapananlar.Average(k => (k.SatisTarihi!.Value - k.AlisTarihi).TotalDays), 1);
            }

            // Grid vs Core
            var gridKapanan = kapananlar.Where(k => k.PozisyonTipi == 0).ToList();
            var coreKapanan = kapananlar.Where(k => k.PozisyonTipi == 1).ToList();
            GridIslem = gridKapanan.Count;
            GridToplam = gridKapanan.Sum(k => k.Kar ?? 0);
            GridKazancOrani = GridIslem > 0 ? Math.Round((double)gridKapanan.Count(k => k.Kar > 0) / GridIslem * 100, 1) : 0;
            CoreIslem = coreKapanan.Count;
            CoreToplam = coreKapanan.Sum(k => k.Kar ?? 0);
            CoreKazancOrani = CoreIslem > 0 ? Math.Round((double)coreKapanan.Count(k => k.Kar > 0) / CoreIslem * 100, 1) : 0;

            // Kumulatif kar zaman serisi
            var sirali = kapananlar.OrderBy(k => k.SatisTarihi).ToList();
            var kum = 0m;
            var kumLabels = new List<string>();
            var kumData = new List<decimal>();
            foreach (var g in sirali.GroupBy(k => k.SatisTarihi!.Value.Date).OrderBy(g => g.Key))
            {
                kum += g.Sum(x => x.Kar!.Value);
                kumLabels.Add(g.Key.ToString("dd/MM/yy"));
                kumData.Add(Math.Round(kum, 0));
            }
            KumulatifKarJson = JsonSerializer.Serialize(new { labels = kumLabels, data = kumData });

            // Aylik kar
            var aylik = kapananlar
                .GroupBy(k => new { k.SatisTarihi!.Value.Year, k.SatisTarihi!.Value.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new
                {
                    Ay = $"{g.Key.Year}-{g.Key.Month:D2}",
                    Kar = g.Sum(x => x.Kar!.Value)
                })
                .ToList();

            AylikKarJson = JsonSerializer.Serialize(new
            {
                labels = aylik.Select(a => a.Ay).ToArray(),
                data = aylik.Select(a => Math.Round(a.Kar, 0)).ToArray()
            });
        }
    }
}
