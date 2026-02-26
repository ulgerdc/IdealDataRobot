using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace StockPortfolioReports.Pages.Arbitraj
{
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public int AktifConfigSayisi { get; set; }
        public int AcikPozisyonSayisi { get; set; }
        public decimal ToplamKar { get; set; }
        public double GlobalButce { get; set; }
        public decimal KullanilanButce { get; set; }
        public double ButceYuzde { get; set; }

        public List<ConfigDurumDto> ConfigDurumlari { get; set; } = new();
        public string SpreadTrendiJson { get; set; } = "{}";

        public async Task OnGetAsync()
        {
            var configs = await _context.ArbitrajGelismis
                .Where(c => c.AktifMi)
                .ToListAsync();

            AktifConfigSayisi = configs.Count;

            AcikPozisyonSayisi = await _context.ArbitrajGelismisHareket
                .CountAsync(h => h.AktifMi);

            var kapaliKarlar = await _context.ArbitrajGelismisHareket
                .Where(h => !h.AktifMi && h.Kar != null)
                .Select(h => h.Kar ?? 0)
                .ToListAsync();
            ToplamKar = kapaliKarlar.Sum();

            // Global butce
            var globalConfig = await _context.ArbitrajGelismis
                .Where(c => c.HisseAdi == "_GLOBAL")
                .FirstOrDefaultAsync();
            GlobalButce = globalConfig?.Butce ?? 250000;

            var acikHareketler = await _context.ArbitrajGelismisHareket
                .Where(h => h.AktifMi)
                .ToListAsync();
            KullanilanButce = acikHareketler.Sum(h => h.Bacak1GirisFiyat * h.Bacak1Lot + h.Bacak2GirisFiyat * h.Bacak2Lot);
            ButceYuzde = GlobalButce > 0 ? (double)KullanilanButce / GlobalButce * 100 : 0;

            // Son spread durumu her config icin
            foreach (var config in configs)
            {
                var sonLog = await _context.ArbitrajSpreadLog
                    .Where(s => s.ArbitrajGelismisId == config.Id)
                    .OrderByDescending(s => s.Tarih)
                    .FirstOrDefaultAsync();

                var aktifPoz = await _context.ArbitrajGelismisHareket
                    .Where(h => h.ArbitrajGelismisId == config.Id && h.AktifMi)
                    .FirstOrDefaultAsync();

                ConfigDurumlari.Add(new ConfigDurumDto
                {
                    ConfigId = config.Id,
                    HisseAdi = config.HisseAdi,
                    ArbitrajTipi = config.ArbitrajTipi,
                    GirisMarji = config.GirisMarji,
                    CikisMarji = config.CikisMarji,
                    YillikFaiz = config.YillikFaiz,
                    SonBacak1Fiyat = sonLog?.Bacak1Fiyat,
                    SonBacak2Fiyat = sonLog?.Bacak2Fiyat,
                    SonSpreadYuzde = sonLog?.SpreadYuzde,
                    SonAdilSpreadYuzde = sonLog?.AdilSpreadYuzde,
                    SonNetPrimYuzde = sonLog?.NetPrimYuzde,
                    SonKalanGun = sonLog?.KalanGun,
                    SonLogTarih = sonLog?.Tarih,
                    AktifPozisyonVar = aktifPoz != null,
                    AktifPozGirisSpread = aktifPoz?.GirisSpreadYuzde
                });
            }

            // Son 24 saat spread trendi
            var son24Saat = DateTime.Now.AddHours(-24);
            var spreadLogs = await _context.ArbitrajSpreadLog
                .Where(s => s.Tarih >= son24Saat && configs.Select(c => c.Id).Contains(s.ArbitrajGelismisId))
                .OrderBy(s => s.Tarih)
                .ToListAsync();

            var datasets = new List<object>();
            var colors = new[] { "#0d6efd", "#198754", "#dc3545", "#ffc107" };
            int colorIdx = 0;
            foreach (var config in configs)
            {
                var logs = spreadLogs.Where(s => s.ArbitrajGelismisId == config.Id).ToList();
                if (logs.Any())
                {
                    var tipLabel = config.ArbitrajTipi == 0 ? "SV" : "TS";
                    datasets.Add(new
                    {
                        label = $"{config.HisseAdi} [{tipLabel}]",
                        data = logs.Select(l => new { x = l.Tarih.ToString("HH:mm"), y = l.NetPrimYuzde ?? 0 }),
                        borderColor = colors[colorIdx % colors.Length],
                        fill = false,
                        tension = 0.3
                    });
                    colorIdx++;
                }
            }

            var girisMarjRef = configs.Any() ? configs.Max(c => c.GirisMarji) : 0;
            var cikisMarjRef = configs.Any() ? configs.Min(c => c.CikisMarji) : 0;

            SpreadTrendiJson = JsonSerializer.Serialize(new
            {
                datasets,
                girisMarji = girisMarjRef,
                cikisMarji = cikisMarjRef
            });
        }

        public class ConfigDurumDto
        {
            public long ConfigId { get; set; }
            public string HisseAdi { get; set; }
            public int ArbitrajTipi { get; set; }
            public double GirisMarji { get; set; }
            public double CikisMarji { get; set; }
            public double YillikFaiz { get; set; }
            public decimal? SonBacak1Fiyat { get; set; }
            public decimal? SonBacak2Fiyat { get; set; }
            public decimal? SonSpreadYuzde { get; set; }
            public decimal? SonAdilSpreadYuzde { get; set; }
            public decimal? SonNetPrimYuzde { get; set; }
            public int? SonKalanGun { get; set; }
            public DateTime? SonLogTarih { get; set; }
            public bool AktifPozisyonVar { get; set; }
            public decimal? AktifPozGirisSpread { get; set; }
        }
    }
}
