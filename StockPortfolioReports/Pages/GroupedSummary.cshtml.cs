using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace StockPortfolioReports.Pages
{
    public class GroupedSummaryModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GroupedSummaryModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string ViewMode { get; set; } = "gunluk"; // gunluk, haftalik, aylik

        public List<GroupedSummaryDto> Ozets { get; set; }

        public async Task OnGetAsync()
        {
            var veriler = await _context.HisseHareket.ToListAsync();

            if (ViewMode == "haftalik")
            {
                Ozets = veriler
                    .GroupBy(x => new {
                        x.HisseAdi,
                        Yil = x.AlisTarihi.Year,
                        Hafta = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
                            x.AlisTarihi, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday)
                    })
                    .Select(g => new GroupedSummaryDto
                    {
                        GrupTarihi = $"{g.Key.Yil}-Hafta {g.Key.Hafta:D2}",
                        HisseAdi = g.Key.HisseAdi,
                        ToplamAlis = g.Sum(x => x.Lot),
                        Satilan = g.Where(x => x.AktifMi == false).Sum(x => x.Lot),
                        Satilmamis = g.Where(x => x.AktifMi == true).Sum(x => x.Lot),
                        ToplamKar = g.Where(x => x.AktifMi == false && x.Kar != null).Sum(x => x.Kar!.Value)
                    })
                    .OrderByDescending(x => x.GrupTarihi).ThenBy(x => x.HisseAdi)
                    .ToList();
            }
            else if (ViewMode == "aylik")
            {
                Ozets = veriler
                    .GroupBy(x => new { x.HisseAdi, Yil = x.AlisTarihi.Year, Ay = x.AlisTarihi.Month })
                    .Select(g => new GroupedSummaryDto
                    {
                        GrupTarihi = $"{g.Key.Yil}-{g.Key.Ay:D2}",
                        HisseAdi = g.Key.HisseAdi,
                        ToplamAlis = g.Sum(x => x.Lot),
                        Satilan = g.Where(x => x.AktifMi == false).Sum(x => x.Lot),
                        Satilmamis = g.Where(x => x.AktifMi == true).Sum(x => x.Lot),
                        ToplamKar = g.Where(x => x.AktifMi == false && x.Kar != null).Sum(x => x.Kar!.Value)
                    })
                    .OrderByDescending(x => x.GrupTarihi).ThenBy(x => x.HisseAdi)
                    .ToList();
            }
            else // gunluk
            {
                Ozets = veriler
                    .GroupBy(x => new { Tarih = x.AlisTarihi.Date, x.HisseAdi })
                    .Select(g => new GroupedSummaryDto
                    {
                        GrupTarihi = g.Key.Tarih.ToString("yyyy-MM-dd"),
                        HisseAdi = g.Key.HisseAdi,
                        ToplamAlis = g.Sum(x => x.Lot),
                        Satilan = g.Where(x => x.AktifMi == false).Sum(x => x.Lot),
                        Satilmamis = g.Where(x => x.AktifMi == true).Sum(x => x.Lot),
                        ToplamKar = g.Where(x => x.AktifMi == false && x.Kar != null).Sum(x => x.Kar!.Value)
                    })
                    .OrderByDescending(x => x.GrupTarihi).ThenBy(x => x.HisseAdi)
                    .ToList();
            }
        }

        public class GroupedSummaryDto
        {
            public string GrupTarihi { get; set; }
            public string HisseAdi { get; set; }
            public int ToplamAlis { get; set; }
            public int Satilan { get; set; }
            public int Satilmamis { get; set; }
            public decimal ToplamKar { get; set; }
        }
    }
}
