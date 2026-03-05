using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace StockPortfolioReports.Pages
{
    public class CoreRaporModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public CoreRaporModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? HisseFiltre { get; set; }

        // Ozet kartlar
        public int ToplamAdet { get; set; }
        public int ToplamLot { get; set; }
        public decimal ToplamMaliyet { get; set; }
        public decimal ToplamGuncelDeger { get; set; }
        public decimal ToplamKZ { get; set; }

        // Tablo
        public List<CoreHisseDto> HisseDetaylari { get; set; } = new();
        public List<string> HisseListesi { get; set; } = new();

        // Grafik
        public string CoreKZJson { get; set; } = "{}";

        public async Task OnGetAsync()
        {
            // Hisse listesi (dropdown)
            HisseListesi = await _context.HisseHareket
                .Where(h => h.PozisyonTipi == 1)
                .Select(h => h.HisseAdi)
                .Distinct()
                .OrderBy(h => h)
                .ToListAsync();

            // Aktif Core pozisyonlar
            var coreQuery = _context.HisseHareket
                .Where(h => h.PozisyonTipi == 1 && h.AktifMi == true);
            if (!string.IsNullOrEmpty(HisseFiltre))
                coreQuery = coreQuery.Where(h => h.HisseAdi == HisseFiltre);
            var corePozisyonlar = await coreQuery.ToListAsync();

            // Kapanmis Core pozisyonlar
            var kapananCoreQuery = _context.HisseHareket
                .Where(h => h.PozisyonTipi == 1 && h.AktifMi == false);
            if (!string.IsNullOrEmpty(HisseFiltre))
                kapananCoreQuery = kapananCoreQuery.Where(h => h.HisseAdi == HisseFiltre);
            var kapananCoreler = await kapananCoreQuery.ToListAsync();

            // Guncel fiyatlar
            var hisseFiyatlar = await _context.Hisse
                .Where(h => h.HisseAdi != "Default" && h.PiyasaSatis != null)
                .ToDictionaryAsync(h => h.HisseAdi, h => h.PiyasaSatis ?? 0);

            // Hisse bazli gruplama
            var tumHisseler = corePozisyonlar.Select(h => h.HisseAdi)
                .Union(kapananCoreler.Select(h => h.HisseAdi))
                .Distinct()
                .ToList();

            HisseDetaylari = tumHisseler.Select(hisse =>
            {
                var aktifler = corePozisyonlar.Where(h => h.HisseAdi == hisse).ToList();
                var kapananlar = kapananCoreler.Where(h => h.HisseAdi == hisse).ToList();
                var guncelFiyat = hisseFiyatlar.GetValueOrDefault(hisse);

                var aktifMaliyet = aktifler.Sum(h => h.AlisFiyati * h.Lot);
                var aktifGuncelDeger = aktifler.Sum(h => guncelFiyat * h.Lot);
                var aktifKZ = aktifGuncelDeger - aktifMaliyet;
                var gerceklesenKar = kapananlar.Sum(h => h.Kar ?? 0);

                return new CoreHisseDto
                {
                    HisseAdi = hisse,
                    AktifAdet = aktifler.Count,
                    AktifLot = aktifler.Sum(h => h.Lot),
                    OrtMaliyet = aktifler.Sum(h => h.Lot) > 0
                        ? Math.Round(aktifMaliyet / aktifler.Sum(h => h.Lot), 2) : 0,
                    GuncelFiyat = guncelFiyat,
                    Maliyet = aktifMaliyet,
                    GuncelDeger = aktifGuncelDeger,
                    AktifKZ = aktifKZ,
                    AktifKZYuzde = aktifMaliyet > 0
                        ? Math.Round((double)(aktifKZ / aktifMaliyet * 100), 2) : 0,
                    KapananAdet = kapananlar.Count,
                    GerceklesenKar = gerceklesenKar,
                    OrtTutmaGun = aktifler.Count > 0
                        ? Math.Round(aktifler.Average(h => (DateTime.Now - h.AlisTarihi).TotalDays), 1) : 0
                };
            })
            .OrderByDescending(x => x.AktifKZ)
            .ToList();

            // Ozet kartlar
            ToplamAdet = HisseDetaylari.Sum(h => h.AktifAdet);
            ToplamLot = HisseDetaylari.Sum(h => h.AktifLot);
            ToplamMaliyet = HisseDetaylari.Sum(h => h.Maliyet);
            ToplamGuncelDeger = HisseDetaylari.Sum(h => h.GuncelDeger);
            ToplamKZ = ToplamGuncelDeger - ToplamMaliyet;

            // Grafik JSON
            var grafikData = HisseDetaylari
                .Where(h => h.AktifLot > 0)
                .OrderByDescending(h => h.AktifKZ)
                .ToList();

            CoreKZJson = JsonSerializer.Serialize(new
            {
                labels = grafikData.Select(x => x.HisseAdi).ToArray(),
                kzData = grafikData.Select(x => Math.Round(x.AktifKZ, 2)).ToArray(),
                gerceklesenData = grafikData.Select(x => Math.Round(x.GerceklesenKar, 2)).ToArray()
            });
        }

        public class CoreHisseDto
        {
            public string HisseAdi { get; set; } = "";
            public int AktifAdet { get; set; }
            public int AktifLot { get; set; }
            public decimal OrtMaliyet { get; set; }
            public decimal GuncelFiyat { get; set; }
            public decimal Maliyet { get; set; }
            public decimal GuncelDeger { get; set; }
            public decimal AktifKZ { get; set; }
            public double AktifKZYuzde { get; set; }
            public int KapananAdet { get; set; }
            public decimal GerceklesenKar { get; set; }
            public double OrtTutmaGun { get; set; }
        }
    }
}
