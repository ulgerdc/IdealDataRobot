/// <summary>
/// KademeStrateji - Hibrit Grid + Core Versiyon
///
/// 4 KATMANLI SATIS MANTIGI:
/// 1. Grid Satis + Core Ayirimi: Normal marj satisi, lotun %30'u uzun vade core olarak tutulur
/// 2. Zaman Bazli Marj Indirimi: 30+ gunluk eski Grid pozisyonlar kademeli dusuk marjla satilir
/// 3. Core Pozisyon Cikisi: Yuksek hedef (%10) veya trailing stop (%5) ile core pozisyon kapanir
/// 4. Trailing Stop Tepe Guncelleme: Her dongude core pozisyonlarin tepe noktasi guncellenir
///
/// PRAGMATIK HIBRIT 3 KURAL:
/// Kural 1: XU030 EMA8 < EMA21 → yarim lot (endeks dususte alimi azalt)
/// Kural 2: Butce %60+ → ATR marjina gec (grid genisler, daha az pozisyon)
/// Kural 3: Core + TimeDecay her zaman aktif (mevcut)
///
/// BUTCE HARD LIMITI: RiskYoneticisi'nde %80 esiginde alim durur
/// TREND KORUMASI: Dusus trendinde alimi durdurma/azaltma
/// </summary>
public class KademeStratejiGelismis
{
    public static void Baslat(dynamic Sistem, string hisseAdi)
    {
        Sistem.AlgoIslem = "OK";

        if (Sistem.BaglantiVar == false)
        {
            return;
        }

        double alisFiyati = IdealManager.AlisFiyatiGetir(Sistem, hisseAdi);
        double satisFiyati = IdealManager.SatisFiyatiGetir(Sistem, hisseAdi);

        if (alisFiyati == 0 || satisFiyati == 0)
        {
            return; // Devre kesik
        }

        var hisse = DatabaseManager.HisseGetir(hisseAdi);
        if (hisse == null)
        {
            hisse = DatabaseManager.HisseGetir("Default");
            if (hisse == null)
            {
                return;
            }
            hisse.HisseAdi = hisseAdi;
        }

        hisse.PiyasaAlis = alisFiyati;
        hisse.PiyasaSatis = satisFiyati;

        // Gunluk yuksek/dusuk veriyi kaydet (ATR hesaplama icin)
        double yuksekGun = IdealManager.YuksekGunGetir(Sistem, hisseAdi);
        double dusukGun = IdealManager.DusukGunGetir(Sistem, hisseAdi);
        if (yuksekGun > 0 && dusukGun > 0)
        {
            DatabaseManager.GunlukVeriKaydet(hisseAdi, yuksekGun, dusukGun, satisFiyati);
        }

        double marj = HesaplaMarj(hisse, alisFiyati, satisFiyati);

        // ONCELIK 1: SATIS ISLEMLERI
        if (IdealManager.SatisSaatiKontrolEt(Sistem) == false && hisse.SatisAktif)
        {
            GelismisSatisYap(Sistem, hisseAdi, satisFiyati, marj, hisse);
        }

        // ONCELIK 1.5: MANUEL EMIR KONTROL
        if (IdealManager.AlisSaatiKontrolEt(Sistem) == false)
        {
            ManuelEmirKontrol(Sistem, hisseAdi, alisFiyati);
        }

        // ONCELIK 2: ALIS ISLEMLERI
        if (IdealManager.AlisSaatiKontrolEt(Sistem) == false && hisse.AlisAktif)
        {
            if (!TrendKorumasiKontrol(Sistem, hisse, alisFiyati))
            {
                DatabaseManager.HisseGuncelle(hisse);
                return;
            }

            var risk = RiskYoneticisi.RiskHesapla(Sistem, hisse, alisFiyati, marj);
            if (risk > 0)
            {
                marj = System.Math.Round(marj * risk, 2);

                var endeksBoleni = RiskYoneticisi.EndeksDegerlendir(Sistem, hisse);
                var alimTutari = IdealManager.DivideAndRoundToInt(hisse.AlimTutari, endeksBoleni);

                // KURAL 1: XU030 EMA8 < EMA21 → yarim lot
                double emaCarpani = IdealManager.EndeksEMAKontrol(Sistem);
                alimTutari = (int)(alimTutari * emaCarpani);

                int lot = IdealManager.DivideAndRoundToInt(alimTutari, alisFiyati);

                var hisseAlimKontrol = DatabaseManager.HisseAlimKontrol(hisseAdi, alisFiyati, marj);
                if (hisseAlimKontrol)
                {
                    IdealManager.Al(Sistem, hisse.HisseAdi, lot, alisFiyati);
                    var hisseAl = new HisseHareket();
                    hisseAl.AlisFiyati = alisFiyati;
                    hisseAl.HisseAdi = hisseAdi;
                    hisseAl.Lot = lot;
                    hisseAl.RobotAdi = Sistem.Name;
                    DatabaseManager.HisseHareketEkleGuncelle(hisseAl);
                }
            }
        }

        DatabaseManager.HisseGuncelle(hisse);
    }

    /// <summary>
    /// 4 Katmanli Satis Mekanizmasi
    /// </summary>
    private static void GelismisSatisYap(dynamic Sistem, string hisseAdi, double satisFiyati, double marj, Hisse hisse)
    {
        // KATMAN 1: Grid Satis + Core Ayirimi
        var gridSatislar = DatabaseManager.HisseSatimKontrol(hisseAdi, satisFiyati, marj, 0);
        if (gridSatislar.Item1 > 0)
        {
            int coreOran = DinamikCoreOranHesapla(Sistem, hisse, satisFiyati);
            int toplamSatisLot = 0;

            foreach (var item in gridSatislar.Item2)
            {
                int coreLot = (int)System.Math.Floor(item.Lot * coreOran / 100.0);
                int gridLot = item.Lot - coreLot;

                if (gridLot <= 0)
                {
                    gridLot = item.Lot;
                    coreLot = 0;
                }

                if (coreLot > 0)
                {
                    // Lotu azalt (grid parcasi)
                    DatabaseManager.HisseHareketLotGuncelle(item.Id, gridLot);

                    // Grid kaydini kapat
                    item.Lot = gridLot;
                    item.SatisFiyati = satisFiyati;
                    item.AktifMi = false;
                    DatabaseManager.HisseHareketEkleGuncelle(item);

                    // Core pozisyon ac
                    var coreKayit = new HisseHareket();
                    coreKayit.HisseAdi = hisseAdi;
                    coreKayit.Lot = coreLot;
                    coreKayit.AlisFiyati = item.AlisFiyati;
                    coreKayit.RobotAdi = Sistem.Name;
                    coreKayit.PozisyonTipi = 1;
                    DatabaseManager.HisseHareketEkleGuncelle(coreKayit);

                    toplamSatisLot += gridLot;
                }
                else
                {
                    // Core ayirimi yok, tamamen sat
                    item.SatisFiyati = satisFiyati;
                    item.AktifMi = false;
                    DatabaseManager.HisseHareketEkleGuncelle(item);
                    toplamSatisLot += item.Lot;
                }
            }

            if (toplamSatisLot > 0)
            {
                IdealManager.Sat(Sistem, hisse.HisseAdi, toplamSatisLot, satisFiyati);
                DatabaseManager.RiskDetayEkle(hisseAdi,
                    string.Format("Grid satis: {0} lot, fiyat: {1}, core ayrildi", toplamSatisLot, satisFiyati));
            }
        }

        // KATMAN 2: Zaman Bazli Marj Indirimi (30+ gunluk Grid pozisyonlar)
        var zamanliSatislar = DatabaseManager.HisseSatimKontrolZamanli(hisseAdi, satisFiyati, marj);
        if (zamanliSatislar.Item1 > 0)
        {
            IdealManager.Sat(Sistem, hisse.HisseAdi, zamanliSatislar.Item1, satisFiyati);
            foreach (var item in zamanliSatislar.Item2)
            {
                item.SatisFiyati = satisFiyati;
                item.AktifMi = false;
                DatabaseManager.HisseHareketEkleGuncelle(item);
            }
            DatabaseManager.RiskDetayEkle(hisseAdi,
                string.Format("Zaman bazli satis: {0} lot, fiyat: {1}", zamanliSatislar.Item1, satisFiyati));
        }

        // KATMAN 3: Core Pozisyon Cikisi (hedef veya trailing stop)
        double coreMarj = hisse.CoreMarj > 0 ? hisse.CoreMarj : 100;
        double trailingStop = hisse.TrailingStopYuzde > 0 ? hisse.TrailingStopYuzde : 5.0;

        var coreSatislar = DatabaseManager.CoreSatimKontrol(hisseAdi, satisFiyati, coreMarj, trailingStop);
        if (coreSatislar.Item1 > 0)
        {
            IdealManager.Sat(Sistem, hisse.HisseAdi, coreSatislar.Item1, satisFiyati);
            foreach (var item in coreSatislar.Item2)
            {
                item.SatisFiyati = satisFiyati;
                item.AktifMi = false;
                DatabaseManager.HisseHareketEkleGuncelle(item);
            }
            DatabaseManager.RiskDetayEkle(hisseAdi,
                string.Format("Core satis: {0} lot, fiyat: {1}", coreSatislar.Item1, satisFiyati));
        }

        // KATMAN 4: Trailing Stop Tepe Guncelleme
        DatabaseManager.CoreTepeNoktasiGuncelle(hisseAdi, satisFiyati);
    }

    private static int DinamikCoreOranHesapla(dynamic Sistem, Hisse hisse, double satisFiyati)
    {
        int baseCore = hisse.CoreOran > 0 ? hisse.CoreOran : 30;

        // 1. 5 gunluk momentum
        double momentum = DatabaseManager.MomentumHesapla(hisse.HisseAdi, 5);

        int coreOran;
        if (momentum >= 10.0) coreOran = 60;
        else if (momentum >= 5.0) coreOran = 50;
        else if (momentum >= 2.0) coreOran = 40;
        else if (momentum >= 0.0) coreOran = baseCore;
        else if (momentum >= -5.0) coreOran = 15;
        else coreOran = 5;

        // 2. Gunluk boost
        double dailyPct = IdealManager.HisseYuzde(Sistem, hisse.HisseAdi);
        if (dailyPct >= 3.0) coreOran = System.Math.Min(coreOran + 10, 70);

        // 3. Endeks filtresi
        double emaCarpani = IdealManager.EndeksEMAKontrol(Sistem);
        if (emaCarpani < 1.0)
            coreOran = (int)(coreOran * 0.5);

        // Min 0, max 70
        coreOran = System.Math.Max(0, System.Math.Min(coreOran, 70));

        DatabaseManager.RiskDetayEkle(hisse.HisseAdi,
            string.Format("DinamikCore: {0}% (mom5={1:F1}%, daily={2:F1}%, ema={3})",
            coreOran, momentum, dailyPct, emaCarpani));

        return coreOran;
    }

    private static double HesaplaMarj(Hisse hisse, double alisFiyati, double satisFiyati)
    {
        double marj = 0;

        // KURAL 2: Butce yuzdesini kontrol et, esik asildiysa ATR marjina zorla
        int effectiveMarjTipi = hisse.MarjTipi;

        if (hisse.ButceAtrGecisYuzde > 0 && effectiveMarjTipi != 2)
        {
            var poz = DatabaseManager.AcikHissePozisyonlariGetir(hisse.HisseAdi);
            if (poz != null && hisse.Butce > 0)
            {
                double yuzde = IdealManager.CalculatePercentage(poz.AcikPozisyonAlimTutari, hisse.Butce);
                if (yuzde >= hisse.ButceAtrGecisYuzde)
                {
                    effectiveMarjTipi = 2; // ATR'ye zorla
                    DatabaseManager.RiskDetayEkle(hisse.HisseAdi,
                        string.Format("Butce %{0} >= %{1} - ATR marjina gecildi",
                        System.Math.Round(yuzde, 1), hisse.ButceAtrGecisYuzde));
                }
            }
        }

        if (effectiveMarjTipi == 0) // Kademe
        {
            var kademeFiyati = System.Math.Round((alisFiyati - satisFiyati), 2);
            marj = hisse.Marj * kademeFiyati;
        }
        else if (effectiveMarjTipi == 1) // Binde
        {
            marj = IdealManager.DivideAndNoRound(hisse.Marj, 1000) * alisFiyati;
        }
        else if (effectiveMarjTipi == 2) // ATR
        {
            int periyot = hisse.AtrPeriyot > 0 ? hisse.AtrPeriyot : 14;
            double carpan = hisse.AtrCarpan > 0 ? hisse.AtrCarpan : 0.5;
            string zamanDilimi = string.IsNullOrEmpty(hisse.AtrZamanDilimi) ? "D" : hisse.AtrZamanDilimi;

            double atr = DatabaseManager.ATRHesapla(hisse.HisseAdi, periyot, zamanDilimi);

            if (atr > 0)
            {
                marj = System.Math.Round(atr * carpan, 2);
            }
            else
            {
                // ATR verisi henuz yoksa binde 7 fallback
                marj = IdealManager.DivideAndNoRound(7, 1000) * alisFiyati;
            }
        }

        return marj;
    }

    private static bool TrendKorumasiKontrol(dynamic Sistem, Hisse hisse, double alisFiyati)
    {
        double bist100Yuzde = IdealManager.Bist100EndeksYuzde(Sistem);
        double bist30Yuzde = IdealManager.Bist30EndeksYuzde(Sistem);

        if (bist100Yuzde < -5.0 && bist30Yuzde < -5.0)
        {
            DatabaseManager.RiskDetayEkle(hisse.HisseAdi, "Trend korumasi - alim yapilmadi");
            return false;
        }

        double dusukGun = IdealManager.DusukGunGetir(Sistem, hisse.HisseAdi);
        double yuksekGun = IdealManager.YuksekGunGetir(Sistem, hisse.HisseAdi);

        if (yuksekGun > 0 && dusukGun > 0)
        {
            double gunIciDusus = ((yuksekGun - alisFiyati) / yuksekGun) * 100;

            if (gunIciDusus > 8.0)
            {
                DatabaseManager.RiskDetayEkle(hisse.HisseAdi,
                    string.Format("Gun ici %{0} dusus - alim yapilmadi",
                    System.Math.Round(gunIciDusus, 2)));
                return false;
            }
        }

        if (hisse.IlkFiyat > 0)
        {
            double ilkFiyatDusus = ((hisse.IlkFiyat - alisFiyati) / hisse.IlkFiyat) * 100;

            if (ilkFiyatDusus > 30.0)
            {
                DatabaseManager.RiskDetayEkle(hisse.HisseAdi,
                    string.Format("Ilk fiyattan %{0} dusus - dikkatli alim",
                    System.Math.Round(ilkFiyatDusus, 2)));
            }
        }

        return true;
    }

    public static void ManuelEmirKontrolTumu(dynamic Sistem)
    {
        if (Sistem.BaglantiVar == false) return;
        if (IdealManager.AlisSaatiKontrolEt(Sistem) != false) return;

        var emirler = DatabaseManager.ManuelEmirGetir(null);
        foreach (var emir in emirler)
        {
            double alisFiyati = IdealManager.AlisFiyatiGetir(Sistem, emir.HisseAdi);
            if (alisFiyati <= 0) continue;

            if (alisFiyati <= emir.AlisFiyati)
            {
                IdealManager.Al(Sistem, emir.HisseAdi, emir.Lot, alisFiyati);

                var hareket = new HisseHareket();
                hareket.HisseAdi = emir.HisseAdi;
                hareket.Lot = emir.Lot;
                hareket.AlisFiyati = alisFiyati;
                hareket.RobotAdi = "ManuelCore";
                hareket.PozisyonTipi = 1; // Core
                DatabaseManager.HisseHareketEkleGuncelle(hareket);

                DatabaseManager.ManuelEmirGuncelle(emir.Id, 1, alisFiyati);

                DatabaseManager.RiskDetayEkle(emir.HisseAdi,
                    string.Format("Manuel Core emir gerceklesti: {0} lot, limit: {1}, gercek: {2}",
                    emir.Lot, emir.AlisFiyati, alisFiyati));
            }
        }
    }

    public static void ManuelEmirKontrol(dynamic Sistem, string hisseAdi, double alisFiyati)
    {
        var emirler = DatabaseManager.ManuelEmirGetir(hisseAdi);
        foreach (var emir in emirler)
        {
            if (alisFiyati <= emir.AlisFiyati)
            {
                IdealManager.Al(Sistem, hisseAdi, emir.Lot, alisFiyati);

                var hareket = new HisseHareket();
                hareket.HisseAdi = hisseAdi;
                hareket.Lot = emir.Lot;
                hareket.AlisFiyati = alisFiyati;
                hareket.RobotAdi = "ManuelCore";
                hareket.PozisyonTipi = 1; // Core
                DatabaseManager.HisseHareketEkleGuncelle(hareket);

                DatabaseManager.ManuelEmirGuncelle(emir.Id, 1, alisFiyati);

                DatabaseManager.RiskDetayEkle(hisseAdi,
                    string.Format("Manuel Core emir gerceklesti: {0} lot, limit: {1}, gercek: {2}",
                    emir.Lot, emir.AlisFiyati, alisFiyati));
            }
        }
    }
}
