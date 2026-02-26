
public class ArbitrajStratejiGelismis
{
    static System.Collections.Generic.Dictionary<long, System.Tuple<double, System.DateTime>> spreadCache
        = new System.Collections.Generic.Dictionary<long, System.Tuple<double, System.DateTime>>();

    public static void Baslat(dynamic Sistem)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        Sistem.AlgoIslem = "OK";
        if (Sistem.BaglantiVar == false)
            return;

        if (IdealManager.ArbitrajGelismisSaatiKontrolEt(Sistem) == true)
            return;

        double bist100Yuzde = 0;
        double bist30Yuzde = 0;
        double viop30Yuzde = 0;

        try { bist100Yuzde = IdealManager.Bist100EndeksYuzde(Sistem); } catch { }
        try { bist30Yuzde = IdealManager.Bist30EndeksYuzde(Sistem); } catch { }
        try { viop30Yuzde = IdealManager.Viop30EndeksYuzde(Sistem); } catch { }

        var configList = DatabaseManager.ArbitrajGelismisGetir();

        double globalButce = DatabaseManager.ArbitrajGelismisGlobalButceGetir();
        double acikTutar = DatabaseManager.ArbitrajGelismisAcikTutar();

        foreach (var config in configList)
        {
            try
            {
                if (config.ArbitrajTipi == 0)
                    SpotViopIzle(Sistem, config, sb, bist100Yuzde, bist30Yuzde, viop30Yuzde, globalButce, ref acikTutar);
                else if (config.ArbitrajTipi == 1)
                    TakvimSpreadIzle(Sistem, config, sb, bist100Yuzde, bist30Yuzde, viop30Yuzde, globalButce, ref acikTutar);
            }
            catch (System.Exception ex)
            {
                sb.AppendLine(config.HisseAdi + " HATA: " + ex.Message);
            }
        }

        if (sb.Length > 0)
            Sistem.Mesaj(sb.ToString());
    }

    static int KalanGunHesapla(System.DateTime vadeSonGun)
    {
        if (vadeSonGun == System.DateTime.MinValue)
            return 30;
        int gun = (vadeSonGun.Date - System.DateTime.Now.Date).Days;
        return System.Math.Max(0, gun);
    }

    static double AdilSpreadHesapla(double yillikFaiz, int kalanGun, double temettuTutar, System.DateTime temettuTarihi, double spotFiyat)
    {
        double adilSpread = yillikFaiz * (kalanGun / 365.0);

        // Temettu duzeltmesi: ex-date henuz gecmediyse temettu yuzdesini cikart
        if (temettuTutar > 0 && temettuTarihi > System.DateTime.Now.Date && spotFiyat > 0)
        {
            double temettuYuzde = (temettuTutar / spotFiyat) * 100;
            adilSpread -= temettuYuzde;
        }

        return adilSpread;
    }

    static void SpotViopIzle(dynamic Sistem, ArbitrajGelismisConfig config, System.Text.StringBuilder sb,
        double bist100Yuzde, double bist30Yuzde, double viop30Yuzde,
        double globalButce, ref double acikTutar)
    {
        double bistFiyat = IdealManager.AlisFiyatiGetir(Sistem, config.HisseAdi);
        double viopFiyat = IdealManager.ViopSatisFiyatiGetirVade(Sistem, config.HisseAdi, config.YakinVadeKodu);

        if (bistFiyat <= 0 || viopFiyat <= 0)
            return;

        double spreadTutar = viopFiyat - bistFiyat;
        double spreadYuzde = (spreadTutar / bistFiyat) * 100;

        int kalanGun = KalanGunHesapla(config.YakinVadeSonGun);
        double adilSpread = AdilSpreadHesapla(config.YillikFaiz, kalanGun, config.TemettuTutar, config.TemettuTarihi, bistFiyat);
        double netPrim = spreadYuzde - adilSpread;

        // Aktif pozisyon kontrol
        var aktifPozisyon = DatabaseManager.ArbitrajGelismisKontrol(config.HisseAdi, 0);

        string durum = "";
        string atlanmaAciklamasi = null;

        if (aktifPozisyon != null)
        {
            // --- CIKIS KONTROL ---
            bool cikisSinyali = netPrim <= config.CikisMarji;

            if (cikisSinyali)
            {
                // EMIR: VIOP alis (short kapama) + BIST satis
                // IdealManager.ViopAlVade(Sistem, config.HisseAdi, config.YakinVadeKodu, config.ViopLot, viopFiyat);
                // IdealManager.Sat(Sistem, config.HisseAdi, config.BistLot, bistFiyat);

                aktifPozisyon.Bacak1CikisFiyat = bistFiyat;
                aktifPozisyon.Bacak2CikisFiyat = viopFiyat;
                aktifPozisyon.CikisSpreadYuzde = spreadYuzde;
                DatabaseManager.ArbitrajGelismisHareketGuncelle(aktifPozisyon);

                durum = " >>> CIKIS YAPILDI <<<";
                DatabaseManager.RiskDetayEkle(config.HisseAdi,
                    "ARB Spot-VIOP CIKIS YAPILDI: spread %" + spreadYuzde.ToString("F2")
                    + ", adil %" + adilSpread.ToString("F2")
                    + ", prim %" + netPrim.ToString("F2")
                    + ", BIST:" + bistFiyat.ToString("F2")
                    + ", VIOP:" + viopFiyat.ToString("F2")
                    + ", girisSpread %" + aktifPozisyon.GirisSpreadYuzde.ToString("F2"));
            }
            else
            {
                durum = " [POZ: giris " + aktifPozisyon.GirisSpreadYuzde.ToString("F2") + "%]";
            }
        }
        else
        {
            // --- GIRIS KONTROL ---
            bool girisSinyali = netPrim >= config.GirisMarji;

            if (girisSinyali)
            {
                double yeniTutar = bistFiyat * config.BistLot + viopFiyat * config.ViopLot;
                if (acikTutar + yeniTutar > globalButce)
                {
                    girisSinyali = false;
                    atlanmaAciklamasi = "BUTCE YETERSIZ: acik=" + acikTutar.ToString("F0")
                        + " + yeni=" + yeniTutar.ToString("F0")
                        + " > limit=" + globalButce.ToString("F0");
                    DatabaseManager.RiskDetayEkle(config.HisseAdi, "ARB Spot-VIOP " + atlanmaAciklamasi);
                }
            }

            if (girisSinyali)
            {
                // EMIR: BIST alis + VIOP satis (short)
                // IdealManager.Al(Sistem, config.HisseAdi, config.BistLot, bistFiyat);
                // IdealManager.ViopSatVade(Sistem, config.HisseAdi, config.YakinVadeKodu, config.ViopLot, viopFiyat);

                var yeniHareket = new ArbitrajGelismisHareket
                {
                    Id = 0,
                    ArbitrajGelismisId = config.Id,
                    RobotAdi = Sistem.Name,
                    HisseAdi = config.HisseAdi,
                    ArbitrajTipi = 0,
                    Bacak1Sembol = "IMKBH'" + config.HisseAdi,
                    Bacak1Yon = "ALIS",
                    Bacak1GirisFiyat = bistFiyat,
                    Bacak1Lot = config.BistLot,
                    Bacak2Sembol = IdealManager.ViopSembolOlustur(config.HisseAdi, config.YakinVadeKodu),
                    Bacak2Yon = "SATIS",
                    Bacak2GirisFiyat = viopFiyat,
                    Bacak2Lot = config.ViopLot,
                    GirisSpreadYuzde = spreadYuzde
                };
                DatabaseManager.ArbitrajGelismisHareketGuncelle(yeniHareket);

                acikTutar += bistFiyat * config.BistLot + viopFiyat * config.ViopLot;

                durum = " >>> GIRIS YAPILDI <<<";
                DatabaseManager.RiskDetayEkle(config.HisseAdi,
                    "ARB Spot-VIOP GIRIS YAPILDI: spread %" + spreadYuzde.ToString("F2")
                    + ", adil %" + adilSpread.ToString("F2")
                    + ", prim %" + netPrim.ToString("F2")
                    + ", BIST:" + bistFiyat.ToString("F2")
                    + ", VIOP:" + viopFiyat.ToString("F2")
                    + ", vade:" + config.YakinVadeKodu
                    + ", kalanGun:" + kalanGun);
            }
        }

        if (durum.Contains("YAPILDI"))
        {
            sb.AppendLine(config.HisseAdi + " [SV] BIST:" + bistFiyat.ToString("F2")
                + " VIOP(" + config.YakinVadeKodu + "):" + viopFiyat.ToString("F2")
                + " Spread:" + spreadYuzde.ToString("F2") + "%"
                + " Prim:" + netPrim.ToString("F2") + "%"
                + " [" + kalanGun + "g]" + durum);
        }

        if (SpreadDegistiMi(config.Id, spreadYuzde))
        {
            bool logGiris = aktifPozisyon == null && netPrim >= config.GirisMarji;
            bool logCikis = aktifPozisyon != null && netPrim <= config.CikisMarji;
            DatabaseManager.ArbitrajSpreadLogKaydet(config.Id, config.HisseAdi, config.ArbitrajTipi,
                bistFiyat, viopFiyat, spreadYuzde, spreadTutar,
                logGiris, logCikis, atlanmaAciklamasi,
                bist100Yuzde, bist30Yuzde, viop30Yuzde,
                adilSpread, netPrim, kalanGun);
        }
    }

    static void TakvimSpreadIzle(dynamic Sistem, ArbitrajGelismisConfig config, System.Text.StringBuilder sb,
        double bist100Yuzde, double bist30Yuzde, double viop30Yuzde,
        double globalButce, ref double acikTutar)
    {
        if (string.IsNullOrEmpty(config.UzakVadeKodu))
            return;

        double yakinFiyat = IdealManager.ViopSatisFiyatiGetirVade(Sistem, config.HisseAdi, config.YakinVadeKodu);
        double uzakFiyat = IdealManager.ViopAlisFiyatiGetirVade(Sistem, config.HisseAdi, config.UzakVadeKodu);

        if (yakinFiyat <= 0 || uzakFiyat <= 0)
            return;

        double spreadTutar = uzakFiyat - yakinFiyat;
        double spreadYuzde = (spreadTutar / yakinFiyat) * 100;
        string yon = spreadYuzde > 0 ? "Contango" : "Backwardation";
        bool contango = spreadYuzde > 0;

        int kalanGunYakin = KalanGunHesapla(config.YakinVadeSonGun);
        int kalanGunUzak = KalanGunHesapla(config.UzakVadeSonGun);
        int vadeFarki = kalanGunUzak - kalanGunYakin;
        double adilSpread = AdilSpreadHesapla(config.YillikFaiz, vadeFarki, config.TemettuTutar, config.TemettuTarihi, yakinFiyat);
        double netPrim = System.Math.Abs(spreadYuzde) - adilSpread;

        // Aktif pozisyon kontrol
        var aktifPozisyon = DatabaseManager.ArbitrajGelismisKontrol(config.HisseAdi, 1);

        string durum = "";
        string atlanmaAciklamasi = null;

        if (aktifPozisyon != null)
        {
            // --- CIKIS KONTROL ---
            bool cikisSinyali = netPrim <= config.CikisMarji;

            if (cikisSinyali)
            {
                // Cikis: ters islem
                // Giris ALIS idi → cikis SATIS, giris SATIS idi → cikis ALIS
                // IdealManager.ViopSatVade(Sistem, config.HisseAdi, aktifPozisyon.Bacak1Yon == "ALIS" ? config.YakinVadeKodu : config.UzakVadeKodu, config.ViopLot, aktifPozisyon.Bacak1Yon == "ALIS" ? yakinFiyat : uzakFiyat);
                // IdealManager.ViopAlVade(Sistem, config.HisseAdi, aktifPozisyon.Bacak2Yon == "SATIS" ? config.UzakVadeKodu : config.YakinVadeKodu, config.ViopLot, aktifPozisyon.Bacak2Yon == "SATIS" ? uzakFiyat : yakinFiyat);

                // Cikis fiyatlari: bacak1 yakin vade, bacak2 uzak vade
                if (aktifPozisyon.Bacak1Yon == "ALIS")
                {
                    aktifPozisyon.Bacak1CikisFiyat = yakinFiyat;
                    aktifPozisyon.Bacak2CikisFiyat = uzakFiyat;
                }
                else
                {
                    aktifPozisyon.Bacak1CikisFiyat = yakinFiyat;
                    aktifPozisyon.Bacak2CikisFiyat = uzakFiyat;
                }
                aktifPozisyon.CikisSpreadYuzde = spreadYuzde;
                DatabaseManager.ArbitrajGelismisHareketGuncelle(aktifPozisyon);

                durum = " >>> CIKIS YAPILDI <<<";
                DatabaseManager.RiskDetayEkle(config.HisseAdi,
                    "ARB Takvim CIKIS YAPILDI: spread %" + spreadYuzde.ToString("F2")
                    + " (" + yon + ")"
                    + ", adil %" + adilSpread.ToString("F2")
                    + ", prim %" + netPrim.ToString("F2")
                    + ", girisSpread %" + aktifPozisyon.GirisSpreadYuzde.ToString("F2"));
            }
            else
            {
                durum = " [POZ: giris " + aktifPozisyon.GirisSpreadYuzde.ToString("F2") + "%]";
            }
        }
        else
        {
            // --- GIRIS KONTROL ---
            bool girisSinyali = netPrim >= config.GirisMarji;

            if (girisSinyali)
            {
                double yeniTutar = yakinFiyat * config.ViopLot + uzakFiyat * config.ViopLot;
                if (acikTutar + yeniTutar > globalButce)
                {
                    girisSinyali = false;
                    atlanmaAciklamasi = "BUTCE YETERSIZ: acik=" + acikTutar.ToString("F0")
                        + " + yeni=" + yeniTutar.ToString("F0")
                        + " > limit=" + globalButce.ToString("F0");
                    DatabaseManager.RiskDetayEkle(config.HisseAdi, "ARB Takvim " + atlanmaAciklamasi);
                }
            }

            if (girisSinyali)
            {
                // Contango: yakin vade ALIS + uzak vade SATIS
                // Backwardation: yakin vade SATIS + uzak vade ALIS
                string bacak1Yon = contango ? "ALIS" : "SATIS";
                string bacak2Yon = contango ? "SATIS" : "ALIS";

                // IdealManager.ViopAlVade(Sistem, config.HisseAdi, contango ? config.YakinVadeKodu : config.UzakVadeKodu, config.ViopLot, contango ? yakinFiyat : uzakFiyat);
                // IdealManager.ViopSatVade(Sistem, config.HisseAdi, contango ? config.UzakVadeKodu : config.YakinVadeKodu, config.ViopLot, contango ? uzakFiyat : yakinFiyat);

                var yeniHareket = new ArbitrajGelismisHareket
                {
                    Id = 0,
                    ArbitrajGelismisId = config.Id,
                    RobotAdi = Sistem.Name,
                    HisseAdi = config.HisseAdi,
                    ArbitrajTipi = 1,
                    Bacak1Sembol = IdealManager.ViopSembolOlustur(config.HisseAdi, config.YakinVadeKodu),
                    Bacak1Yon = bacak1Yon,
                    Bacak1GirisFiyat = yakinFiyat,
                    Bacak1Lot = config.ViopLot,
                    Bacak2Sembol = IdealManager.ViopSembolOlustur(config.HisseAdi, config.UzakVadeKodu),
                    Bacak2Yon = bacak2Yon,
                    Bacak2GirisFiyat = uzakFiyat,
                    Bacak2Lot = config.ViopLot,
                    GirisSpreadYuzde = spreadYuzde
                };
                DatabaseManager.ArbitrajGelismisHareketGuncelle(yeniHareket);

                acikTutar += yakinFiyat * config.ViopLot + uzakFiyat * config.ViopLot;

                durum = " >>> GIRIS YAPILDI <<<";
                DatabaseManager.RiskDetayEkle(config.HisseAdi,
                    "ARB Takvim GIRIS YAPILDI: spread %" + spreadYuzde.ToString("F2")
                    + " (" + yon + ")"
                    + ", adil %" + adilSpread.ToString("F2")
                    + ", prim %" + netPrim.ToString("F2")
                    + ", Yakin(" + config.YakinVadeKodu + "):" + yakinFiyat.ToString("F2")
                    + ", Uzak(" + config.UzakVadeKodu + "):" + uzakFiyat.ToString("F2")
                    + ", kalanGun Y:" + kalanGunYakin + " U:" + kalanGunUzak);
            }
        }

        if (durum.Contains("YAPILDI"))
        {
            sb.AppendLine(config.HisseAdi + " [TS] Y:" + yakinFiyat.ToString("F2") + "(" + config.YakinVadeKodu + ")"
                + " U:" + uzakFiyat.ToString("F2") + "(" + config.UzakVadeKodu + ")"
                + " Spread:" + spreadYuzde.ToString("F2") + "% " + yon
                + " Prim:" + netPrim.ToString("F2") + "%"
                + " [Y:" + kalanGunYakin + "g U:" + kalanGunUzak + "g]" + durum);
        }

        if (SpreadDegistiMi(config.Id, spreadYuzde))
        {
            bool logGiris = aktifPozisyon == null && netPrim >= config.GirisMarji;
            bool logCikis = aktifPozisyon != null && netPrim <= config.CikisMarji;
            DatabaseManager.ArbitrajSpreadLogKaydet(config.Id, config.HisseAdi, config.ArbitrajTipi,
                yakinFiyat, uzakFiyat, spreadYuzde, spreadTutar,
                logGiris, logCikis, atlanmaAciklamasi,
                bist100Yuzde, bist30Yuzde, viop30Yuzde,
                adilSpread, netPrim, kalanGunYakin);
        }
    }

    static bool SpreadDegistiMi(long configId, double guncelSpread)
    {
        System.Tuple<double, System.DateTime> sonKayit;
        if (spreadCache.TryGetValue(configId, out sonKayit))
        {
            double fark = System.Math.Abs(guncelSpread - sonKayit.Item1);
            double gecenDakika = (System.DateTime.Now - sonKayit.Item2).TotalMinutes;

            if (fark < 0.05 && gecenDakika < 5)
                return false;
        }

        spreadCache[configId] = new System.Tuple<double, System.DateTime>(guncelSpread, System.DateTime.Now);
        return true;
    }
}
