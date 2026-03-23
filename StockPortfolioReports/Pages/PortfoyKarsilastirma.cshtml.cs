using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace StockPortfolioReports.Pages
{
    public class PortfoyKarsilastirmaModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public PortfoyKarsilastirmaModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public IdealHesap? Hesap { get; set; }
        public DateTime? SonGuncelleme { get; set; }
        public List<KarsilastirmaDto> Satirlar { get; set; } = new();

        // Ozet
        public decimal DbToplamMaliyet { get; set; }
        public decimal DbToplamDeger { get; set; }
        public decimal IdealToplamMaliyet { get; set; }
        public decimal IdealToplamDeger { get; set; }
        public int EslesenSayisi { get; set; }
        public int FarkliSayisi { get; set; }
        public int SadeceDbSayisi { get; set; }
        public int SadeceIdealSayisi { get; set; }

        public async Task OnGetAsync()
        {
            Hesap = await _context.IdealHesap.FirstOrDefaultAsync();

            // DB aktif pozisyonlar (HisseHareket AktifMi=1, grup by HisseAdi)
            var dbPozlar = await _context.HisseHareket
                .Where(h => h.AktifMi == true)
                .GroupBy(h => h.HisseAdi)
                .Select(g => new
                {
                    HisseAdi = g.Key,
                    Lot = g.Sum(h => h.Lot),
                    ToplamMaliyet = g.Sum(h => h.Lot * h.AlisFiyati),
                    PozisyonSayisi = g.Count()
                })
                .ToListAsync();

            // Guncel fiyatlar (Hisse tablosundan)
            var fiyatlar = await _context.Hisse
                .Where(h => h.PiyasaSatis > 0)
                .ToDictionaryAsync(h => h.HisseAdi, h => h.PiyasaSatis ?? 0m);

            // IdealPro portfoy
            var idealPozlar = await _context.IdealPortfoy.ToListAsync();
            SonGuncelleme = idealPozlar.FirstOrDefault()?.GuncellemeTarihi;

            // Tum hisse adlarini topla
            var tumHisseler = new HashSet<string>();
            foreach (var p in dbPozlar) tumHisseler.Add(p.HisseAdi);
            foreach (var p in idealPozlar) tumHisseler.Add(p.Sembol);

            // Karsilastirma satirlari olustur
            foreach (var hisse in tumHisseler.OrderBy(h => h))
            {
                var db = dbPozlar.FirstOrDefault(p => p.HisseAdi == hisse);
                var ideal = idealPozlar.FirstOrDefault(p => p.Sembol == hisse);
                fiyatlar.TryGetValue(hisse, out var guncelFiyat);

                var satir = new KarsilastirmaDto();
                satir.HisseAdi = hisse;

                // DB taraf
                if (db != null)
                {
                    satir.DbLot = db.Lot;
                    satir.DbOrtMaliyet = db.Lot > 0 ? Math.Round(db.ToplamMaliyet / db.Lot, 2) : 0;
                    satir.DbMaliyetTutar = Math.Round(db.ToplamMaliyet, 0);
                    satir.DbPozSayisi = db.PozisyonSayisi;
                    if (guncelFiyat > 0)
                    {
                        satir.DbGuncelDeger = Math.Round(db.Lot * guncelFiyat, 0);
                        satir.DbKarZarar = satir.DbGuncelDeger - satir.DbMaliyetTutar;
                    }
                }

                // IdealPro taraf
                if (ideal != null)
                {
                    satir.IdealLot = ideal.Lot;
                    satir.IdealMaliyet = Math.Round((decimal)ideal.Maliyet, 2);
                    satir.IdealMaliyetTutar = Math.Round((decimal)(ideal.Lot * ideal.Maliyet), 0);
                    satir.IdealGuncelFiyat = Math.Round((decimal)ideal.GuncelFiyat, 2);
                    satir.IdealGuncelDeger = Math.Round((decimal)(ideal.Lot * ideal.GuncelFiyat), 0);
                    satir.IdealKarZarar = Math.Round((decimal)ideal.KarZarar, 2);
                }

                // Fark
                satir.LotFark = satir.IdealLot - satir.DbLot;
                satir.SadeceDb = db != null && ideal == null;
                satir.SadeceIdeal = db == null && ideal != null;
                satir.Eslesiyor = db != null && ideal != null && satir.LotFark == 0;

                Satirlar.Add(satir);
            }

            // Ozet
            DbToplamMaliyet = Satirlar.Sum(s => s.DbMaliyetTutar);
            DbToplamDeger = Satirlar.Sum(s => s.DbGuncelDeger);
            IdealToplamMaliyet = Satirlar.Sum(s => s.IdealMaliyetTutar);
            IdealToplamDeger = Satirlar.Sum(s => s.IdealGuncelDeger);
            EslesenSayisi = Satirlar.Count(s => s.Eslesiyor);
            FarkliSayisi = Satirlar.Count(s => !s.SadeceDb && !s.SadeceIdeal && !s.Eslesiyor);
            SadeceDbSayisi = Satirlar.Count(s => s.SadeceDb);
            SadeceIdealSayisi = Satirlar.Count(s => s.SadeceIdeal);
        }

        public class KarsilastirmaDto
        {
            public string HisseAdi { get; set; } = "";

            // DB
            public int DbLot { get; set; }
            public decimal DbOrtMaliyet { get; set; }
            public decimal DbMaliyetTutar { get; set; }
            public decimal DbGuncelDeger { get; set; }
            public decimal DbKarZarar { get; set; }
            public int DbPozSayisi { get; set; }

            // IdealPro
            public int IdealLot { get; set; }
            public decimal IdealMaliyet { get; set; }
            public decimal IdealMaliyetTutar { get; set; }
            public decimal IdealGuncelFiyat { get; set; }
            public decimal IdealGuncelDeger { get; set; }
            public decimal IdealKarZarar { get; set; }

            // Karsilastirma
            public int LotFark { get; set; }
            public bool SadeceDb { get; set; }
            public bool SadeceIdeal { get; set; }
            public bool Eslesiyor { get; set; }
        }
    }
}
