using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace StockPortfolioReports.Pages
{
    public class GunlukRaporModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public GunlukRaporModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? Tarih { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? HisseFiltre { get; set; }

        // Ozet kartlar
        public decimal NetKarZarar { get; set; }
        public int AlisAdet { get; set; }
        public decimal AlisToplam { get; set; }
        public int SatisAdet { get; set; }
        public decimal SatisToplam { get; set; }
        public decimal Hacim { get; set; }

        // Tablo
        public List<HisseGunlukDto> HisseDetaylari { get; set; } = new();
        public List<string> HisseListesi { get; set; } = new();

        // Grafik
        public string KarZararJson { get; set; } = "{}";

        public async Task OnGetAsync()
        {
            Tarih ??= DateTime.Today;
            var gun = Tarih.Value.Date;
            var gunSonu = gun.AddDays(1);

            // Hisse listesi (dropdown)
            HisseListesi = await _context.VHisseHareket
                .Select(h => h.HisseAdi)
                .Distinct()
                .OrderBy(h => h)
                .ToListAsync();

            // Gundeki alislar
            var alisQuery = _context.VHisseHareket
                .Where(h => h.AlisTarihi >= gun && h.AlisTarihi < gunSonu);
            if (!string.IsNullOrEmpty(HisseFiltre))
                alisQuery = alisQuery.Where(h => h.HisseAdi == HisseFiltre);
            var alislar = await alisQuery.ToListAsync();

            // Gundeki satislar (view uzerinden — arsivlenmis kayitlar da dahil)
            var satisQuery = _context.VHisseHareket
                .Where(h => h.AktifMi == 0 && h.SatisTarihi != null
                    && h.SatisTarihi >= gun && h.SatisTarihi < gunSonu);
            if (!string.IsNullOrEmpty(HisseFiltre))
                satisQuery = satisQuery.Where(h => h.HisseAdi == HisseFiltre);
            var satislar = await satisQuery.ToListAsync();

            // Ozet kartlar
            AlisAdet = alislar.Count;
            AlisToplam = alislar.Sum(h => h.AlisFiyati * h.Lot);
            SatisAdet = satislar.Count;
            SatisToplam = satislar.Sum(h => (h.SatisFiyati ?? 0) * h.Lot);
            NetKarZarar = satislar.Sum(h => h.Kar ?? 0);
            Hacim = AlisToplam + SatisToplam;

            // Hisse bazli gruplama — alis ve satisi olan tum hisseler
            var tumHisseler = alislar.Select(h => h.HisseAdi)
                .Union(satislar.Select(h => h.HisseAdi))
                .Distinct()
                .ToList();

            HisseDetaylari = tumHisseler.Select(hisse =>
            {
                var hisseAlis = alislar.Where(h => h.HisseAdi == hisse).ToList();
                var hisseSatis = satislar.Where(h => h.HisseAdi == hisse).ToList();
                var kar = hisseSatis.Where(h => (h.Kar ?? 0) > 0).Sum(h => h.Kar ?? 0);
                var zarar = hisseSatis.Where(h => (h.Kar ?? 0) < 0).Sum(h => h.Kar ?? 0);

                return new HisseGunlukDto
                {
                    HisseAdi = hisse,
                    AlisAdet = hisseAlis.Count,
                    AlisLot = hisseAlis.Sum(h => h.Lot),
                    AlisTutari = hisseAlis.Sum(h => h.AlisFiyati * h.Lot),
                    SatisAdet = hisseSatis.Count,
                    SatisLot = hisseSatis.Sum(h => h.Lot),
                    SatisTutari = hisseSatis.Sum(h => (h.SatisFiyati ?? 0) * h.Lot),
                    Kar = kar,
                    Zarar = zarar,
                    NetKZ = kar + zarar
                };
            })
            .OrderByDescending(x => x.NetKZ)
            .ToList();

            // Grafik JSON
            var grafikData = HisseDetaylari
                .Where(h => h.Kar != 0 || h.Zarar != 0)
                .OrderByDescending(h => h.NetKZ)
                .ToList();

            KarZararJson = JsonSerializer.Serialize(new
            {
                labels = grafikData.Select(x => x.HisseAdi).ToArray(),
                karData = grafikData.Select(x => Math.Round(x.Kar, 2)).ToArray(),
                zararData = grafikData.Select(x => Math.Round(x.Zarar, 2)).ToArray()
            });
        }

        public class HisseGunlukDto
        {
            public string HisseAdi { get; set; } = "";
            public int AlisAdet { get; set; }
            public int AlisLot { get; set; }
            public decimal AlisTutari { get; set; }
            public int SatisAdet { get; set; }
            public int SatisLot { get; set; }
            public decimal SatisTutari { get; set; }
            public decimal Kar { get; set; }
            public decimal Zarar { get; set; }
            public decimal NetKZ { get; set; }
        }
    }
}
