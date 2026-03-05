using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace StockPortfolioReports.Pages
{
    public class YutanMumRaporModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public YutanMumRaporModel(ApplicationDbContext context)
        {
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public DateTime? BaslangicTarih { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? BitisTarih { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? HisseFiltre { get; set; }

        // Ozet kartlar
        public int ToplamBatch { get; set; }
        public int KapananBatch { get; set; }
        public decimal ToplamKar { get; set; }
        public decimal ToplamAlimTutari { get; set; }
        public int ToplamIslem { get; set; }
        public int KazancliIslem { get; set; }
        public int KayipliIslem { get; set; }

        // Tablolar
        public List<BatchDto> Batchler { get; set; } = new();
        public List<HisseOzetDto> HisseOzetleri { get; set; } = new();
        public List<string> HisseListesi { get; set; } = new();

        // Grafik
        public string BatchKarJson { get; set; } = "{}";
        public string HisseKarJson { get; set; } = "{}";

        public async Task OnGetAsync()
        {
            BaslangicTarih ??= DateTime.Today.AddDays(-30);
            BitisTarih ??= DateTime.Today;
            var baslangic = BaslangicTarih.Value.Date;
            var bitis = BitisTarih.Value.Date.AddDays(1);

            // Hisse listesi
            HisseListesi = await _context.YutanMumHareket
                .Select(h => h.HisseAdi)
                .Distinct()
                .OrderBy(h => h)
                .ToListAsync();

            // Batch'ler
            var batchQuery = _context.YutanMumBatch
                .Where(b => b.BatchTarihi >= baslangic && b.BatchTarihi < bitis);
            var batchler = await batchQuery.OrderByDescending(b => b.BatchTarihi).ToListAsync();

            // Hareketler
            var batchIds = batchler.Select(b => b.Id).ToList();
            var hareketQuery = _context.YutanMumHareket
                .Where(h => batchIds.Contains(h.BatchId));
            if (!string.IsNullOrEmpty(HisseFiltre))
                hareketQuery = hareketQuery.Where(h => h.HisseAdi == HisseFiltre);
            var hareketler = await hareketQuery.ToListAsync();

            // Batch tablosu
            Batchler = batchler.Select(b =>
            {
                var bHareketler = hareketler.Where(h => h.BatchId == b.Id).ToList();
                var kapananlar = bHareketler.Where(h => !h.AktifMi).ToList();
                var kar = kapananlar.Sum(h => h.Kar ?? 0);

                return new BatchDto
                {
                    Id = b.Id,
                    Tarih = b.BatchTarihi,
                    HisseSayisi = bHareketler.Count,
                    AlimTutari = Math.Round(bHareketler.Sum(h => h.Lot * h.AlisFiyati), 0),
                    Kar = Math.Round(kar, 2),
                    AktifMi = b.AktifMi,
                    KapanisNedeni = b.KapanisNedeni ?? "",
                    Hisseler = string.Join(", ", bHareketler.Select(h => h.HisseAdi).Distinct()),
                    KazancliAdet = kapananlar.Count(h => (h.Kar ?? 0) > 0),
                    KayipliAdet = kapananlar.Count(h => (h.Kar ?? 0) < 0)
                };
            }).ToList();

            // Hisse bazli ozet
            var kapananHareketler = hareketler.Where(h => !h.AktifMi).ToList();
            var tumHisseler = hareketler.Select(h => h.HisseAdi).Distinct().ToList();

            HisseOzetleri = tumHisseler.Select(hisse =>
            {
                var hisseH = hareketler.Where(h => h.HisseAdi == hisse).ToList();
                var hisseKapanan = kapananHareketler.Where(h => h.HisseAdi == hisse).ToList();
                var toplamKar = hisseKapanan.Sum(h => h.Kar ?? 0);

                return new HisseOzetDto
                {
                    HisseAdi = hisse,
                    IslemSayisi = hisseH.Count,
                    ToplamLot = hisseH.Sum(h => h.Lot),
                    ToplamAlim = Math.Round(hisseH.Sum(h => h.Lot * h.AlisFiyati), 0),
                    KazancliAdet = hisseKapanan.Count(h => (h.Kar ?? 0) > 0),
                    KayipliAdet = hisseKapanan.Count(h => (h.Kar ?? 0) < 0),
                    ToplamKar = Math.Round(toplamKar, 2),
                    OrtMomentum = Math.Round(hisseH.Average(h => h.MomentumYuzde ?? 0), 2)
                };
            })
            .OrderByDescending(x => x.ToplamKar)
            .ToList();

            // Ozet kartlar
            ToplamBatch = Batchler.Count;
            KapananBatch = Batchler.Count(b => !b.AktifMi);
            ToplamKar = kapananHareketler.Sum(h => h.Kar ?? 0);
            ToplamAlimTutari = hareketler.Sum(h => h.Lot * h.AlisFiyati);
            ToplamIslem = kapananHareketler.Count;
            KazancliIslem = kapananHareketler.Count(h => (h.Kar ?? 0) > 0);
            KayipliIslem = kapananHareketler.Count(h => (h.Kar ?? 0) < 0);

            // Batch K/Z grafik (son 20)
            var grafikBatchler = Batchler
                .Where(b => !b.AktifMi)
                .OrderBy(b => b.Tarih)
                .TakeLast(20)
                .ToList();

            BatchKarJson = JsonSerializer.Serialize(new
            {
                labels = grafikBatchler.Select(b => b.Tarih.ToString("dd.MM")).ToArray(),
                karData = grafikBatchler.Select(b => Math.Round(b.Kar, 2)).ToArray()
            });

            // Hisse K/Z grafik
            var grafikHisseler = HisseOzetleri
                .Where(h => h.ToplamKar != 0)
                .OrderByDescending(h => h.ToplamKar)
                .ToList();

            HisseKarJson = JsonSerializer.Serialize(new
            {
                labels = grafikHisseler.Select(x => x.HisseAdi).ToArray(),
                karData = grafikHisseler.Select(x => Math.Round(x.ToplamKar, 2)).ToArray()
            });
        }

        public class BatchDto
        {
            public long Id { get; set; }
            public DateTime Tarih { get; set; }
            public int HisseSayisi { get; set; }
            public decimal AlimTutari { get; set; }
            public decimal Kar { get; set; }
            public bool AktifMi { get; set; }
            public string KapanisNedeni { get; set; } = "";
            public string Hisseler { get; set; } = "";
            public int KazancliAdet { get; set; }
            public int KayipliAdet { get; set; }
        }

        public class HisseOzetDto
        {
            public string HisseAdi { get; set; } = "";
            public int IslemSayisi { get; set; }
            public int ToplamLot { get; set; }
            public decimal ToplamAlim { get; set; }
            public int KazancliAdet { get; set; }
            public int KayipliAdet { get; set; }
            public decimal ToplamKar { get; set; }
            public double OrtMomentum { get; set; }
        }
    }
}
