
public class OvernightSemihStrateji
{
    static string sonTaramaTarihi = "";

    // Parametreler
    static double minYukselis = 1.5;
    static double maxYukselis = 9.5;
    static double butce = 100000;
    static double hisseBasiButce = 10000;
    static int maxPozisyon = 15;

    // Karaliste — overnight performansi kotu olan hisseler
    static System.Collections.Generic.HashSet<string> karaliste = KaralisteOlustur();

    private static System.Collections.Generic.HashSet<string> KaralisteOlustur()
    {
        var set = new System.Collections.Generic.HashSet<string>();
        // BIST30 blue chip — overnight AF% dusuk
        set.Add("KCHOL"); set.Add("TCELL"); set.Add("TUPRS"); set.Add("THYAO");
        set.Add("GARAN"); set.Add("EREGL"); set.Add("AKBNK"); set.Add("HALKB");
        set.Add("ISCTR"); set.Add("YKBNK"); set.Add("SAHOL"); set.Add("SISE");
        set.Add("TTKOM"); set.Add("ENKA"); set.Add("FROTO"); set.Add("KOZAL");
        set.Add("KOZAA"); set.Add("ARCLK"); set.Add("EKGYO"); set.Add("DOAS");
        set.Add("MGROS"); set.Add("PGSUS"); set.Add("TAVHL");
        // Sorunlu / dusuk performans
        set.Add("SASA"); set.Add("TMPOL"); set.Add("VERUS");
        // Yapisal sorunlu
        set.Add("OBAMS"); set.Add("KGYO"); set.Add("LIDER"); set.Add("GESAN");
        set.Add("PASEU"); set.Add("BLUME"); set.Add("ESCAR"); set.Add("TRCAS");
        set.Add("DERIM"); set.Add("YGGYO"); set.Add("ASGYO"); set.Add("PENTA");
        set.Add("ANHYT"); set.Add("GOLTS"); set.Add("FMIZP"); set.Add("DURKN");
        set.Add("GSRAY"); set.Add("CEOEM"); set.Add("AGHOL"); set.Add("KRVGD");
        set.Add("SKTAS"); set.Add("RYGYO"); set.Add("AKSGY");
        return set;
    }

    /// <summary>
    /// TEST MODU — sinyal taramasi yapar, emir gondermez.
    /// IdealPro'da OvernightSemihTestBaslat robotuna bagla.
    /// </summary>
    public static void Test(dynamic Sistem)
    {
        Sistem.AlgoIslem = "OK";
        if (Sistem.BaglantiVar == false)
            return;

        string log = "";
        try { log = string.Format("Saat:{0}", Sistem.Saat); }
        catch { log = "Saat:?"; }

        double xu100 = 0;
        try { xu100 = IdealManager.Bist100EndeksYuzde(Sistem); }
        catch { }
        log += string.Format(" XU100:{0:F2}%", xu100);

        var sinyaller = new System.Collections.Generic.List<OvernightSemihSinyal>();
        int listeCount = 0;
        int karalisteAtlanan = 0;

        try
        {
            var tumHisseler = Sistem.YuzeyselListeGetir("IMKBH'");
            if (tumHisseler == null || tumHisseler.Count == 0)
            {
                Sistem.Mesaj(log + " | Liste:BOS");
                return;
            }
            listeCount = tumHisseler.Count;

            for (int h = 0; h < tumHisseler.Count; h++)
            {
                try
                {
                    string fullName = tumHisseler[h].Symbol.ToString();
                    string hisse = fullName.Replace("IMKBH'", "").Trim();

                    if (karaliste.Contains(hisse))
                    {
                        karalisteAtlanan++;
                        continue;
                    }

                    double netPerDay = 0;
                    try
                    {
                        var veri = Sistem.YuzeyselVeriOku(fullName);
                        netPerDay = veri.NetPerDay;
                    }
                    catch { continue; }

                    if (netPerDay >= minYukselis && netPerDay < maxYukselis)
                    {
                        double alisFiyati = 0;
                        try { alisFiyati = IdealManager.AlisFiyatiGetir(Sistem, hisse); }
                        catch { }

                        var sinyal = new OvernightSemihSinyal();
                        sinyal.HisseAdi = hisse;
                        sinyal.Yukselis = netPerDay;
                        sinyal.AlisFiyati = alisFiyati;
                        sinyaller.Add(sinyal);
                    }
                }
                catch { }
            }
        }
        catch (System.Exception ex)
        {
            Sistem.Mesaj(log + " | HATA:" + ex.Message);
            return;
        }

        // Yukselis'e gore siralama (bubble sort)
        for (int i = 0; i < sinyaller.Count - 1; i++)
        {
            for (int j = i + 1; j < sinyaller.Count; j++)
            {
                if (sinyaller[j].Yukselis > sinyaller[i].Yukselis)
                {
                    var temp = sinyaller[i];
                    sinyaller[i] = sinyaller[j];
                    sinyaller[j] = temp;
                }
            }
        }

        // Sonuc mesaji
        string sinyalListesi = "";
        for (int i = 0; i < sinyaller.Count; i++)
        {
            if (sinyalListesi.Length > 0) sinyalListesi += ", ";
            sinyalListesi += string.Format("{0}({1:F1}%)", sinyaller[i].HisseAdi, sinyaller[i].Yukselis);
        }

        Sistem.Mesaj(string.Format("{0} | Liste:{1} KL:{2} Sinyal:{3} | {4}",
            log, listeCount, karalisteAtlanan, sinyaller.Count,
            sinyaller.Count > 0 ? sinyalListesi : "YOK"));
    }

    /// <summary>
    /// CANLI MOD — sinyal taramasi + emir gonderir.
    /// IdealPro'da OvernightSemihBaslat robotuna bagla.
    /// </summary>
    public static void Baslat(dynamic Sistem)
    {
        Sistem.AlgoIslem = "OK";
        if (Sistem.BaglantiVar == false)
            return;

        if (IdealManager.SaatiKontrolEt(Sistem))
            return;

        YutanMumConfig config = DatabaseManager.YutanMumConfigGetir();
        if (config == null || !config.AktifMi)
            return;

        // Her dongude: aktif batch satis kontrol
        AktifBatchleriKontrolEt(Sistem);

        // Tarama: gun icinde 1 kez
        string bugun = System.DateTime.Now.ToString("yyyy-MM-dd");
        if (sonTaramaTarihi == bugun)
            return;

        // Saat kontrolu — 17:55'te tarama
        if (IdealManager.YutanMumSaatiKontrolEt(Sistem, "17:55"))
            return;

        // Cuma gunu tarama yapma
        if (System.DateTime.Now.DayOfWeek == System.DayOfWeek.Friday)
        {
            sonTaramaTarihi = bugun;
            return;
        }

        // XU100 endeks filtresi
        double xu100 = IdealManager.Bist100EndeksYuzde(Sistem);
        if (xu100 <= 0)
        {
            sonTaramaTarihi = bugun;
            Sistem.Mesaj(string.Format("[SEMIH] XU100 negatif ({0:F2}%), tarama iptal", xu100));
            return;
        }

        // Max aktif batch kontrolu
        var aktifBatchler = DatabaseManager.YutanMumAktifBatchlerGetir();
        int semihBatchCount = 0;
        double kullanilanButce = 0;
        foreach (var batch in aktifBatchler)
        {
            if (batch.RobotAdi == "OvernightSemih")
            {
                semihBatchCount++;
                kullanilanButce += batch.ToplamAlimTutari;
            }
        }
        if (semihBatchCount >= 1) // max 1 aktif batch
        {
            sonTaramaTarihi = bugun;
            return;
        }

        double kalanButce = butce - kullanilanButce;
        if (kalanButce < hisseBasiButce)
        {
            sonTaramaTarihi = bugun;
            return;
        }

        YeniBatchOlustur(Sistem, kalanButce);
        sonTaramaTarihi = bugun;
    }

    private static void YeniBatchOlustur(dynamic Sistem, double aktifButce)
    {
        var sinyaller = new System.Collections.Generic.List<OvernightSemihSinyal>();

        try
        {
            var tumHisseler = Sistem.YuzeyselListeGetir("IMKBH'");
            if (tumHisseler == null || tumHisseler.Count == 0)
                return;

            for (int h = 0; h < tumHisseler.Count; h++)
            {
                try
                {
                    string fullName = tumHisseler[h].Symbol.ToString();
                    string hisse = fullName.Replace("IMKBH'", "").Trim();

                    if (karaliste.Contains(hisse))
                        continue;

                    double netPerDay = 0;
                    try
                    {
                        var veri = Sistem.YuzeyselVeriOku(fullName);
                        netPerDay = veri.NetPerDay;
                        if (netPerDay < minYukselis || netPerDay >= maxYukselis)
                            continue;
                    }
                    catch { continue; }

                    double alisFiyati = IdealManager.AlisFiyatiGetir(Sistem, hisse);
                    if (alisFiyati <= 0)
                        continue;

                    var sinyal = new OvernightSemihSinyal();
                    sinyal.HisseAdi = hisse;
                    sinyal.Yukselis = netPerDay;
                    sinyal.AlisFiyati = alisFiyati;
                    sinyaller.Add(sinyal);
                }
                catch { }
            }
        }
        catch { return; }

        if (sinyaller.Count == 0)
            return;

        // Yukselis'e gore sirala (bubble sort)
        for (int i = 0; i < sinyaller.Count - 1; i++)
        {
            for (int j = i + 1; j < sinyaller.Count; j++)
            {
                if (sinyaller[j].Yukselis > sinyaller[i].Yukselis)
                {
                    var temp = sinyaller[i];
                    sinyaller[i] = sinyaller[j];
                    sinyaller[j] = temp;
                }
            }
        }

        // Max pozisyon limiti
        if (sinyaller.Count > maxPozisyon)
            sinyaller = sinyaller.GetRange(0, maxPozisyon);

        // Butce dagitimi
        double perHisse = aktifButce / sinyaller.Count;
        if (perHisse < hisseBasiButce)
        {
            int maxH = (int)(aktifButce / hisseBasiButce);
            if (maxH <= 0) return;
            if (sinyaller.Count > maxH)
                sinyaller = sinyaller.GetRange(0, maxH);
            perHisse = aktifButce / sinyaller.Count;
        }

        // Lot hesapla
        double toplamAlimTutari = 0;
        foreach (var sinyal in sinyaller)
        {
            sinyal.Lot = (int)(perHisse / sinyal.AlisFiyati);
            if (sinyal.Lot <= 0) sinyal.Lot = 1;
            toplamAlimTutari += sinyal.Lot * sinyal.AlisFiyati;
        }

        // Batch olustur
        long batchId = DatabaseManager.YutanMumBatchOlustur("OvernightSemih", sinyaller.Count, toplamAlimTutari);
        if (batchId <= 0)
            return;

        int basarili = 0;
        string ozet = "";
        foreach (var sinyal in sinyaller)
        {
            try
            {
                // *** EMIR GONDERME — test doneminde comment-out ***
                // IdealManager.Al(Sistem, sinyal.HisseAdi, sinyal.Lot, sinyal.AlisFiyati);

                var hareket = new YutanMumHareket();
                hareket.BatchId = batchId;
                hareket.HisseAdi = sinyal.HisseAdi;
                hareket.Lot = sinyal.Lot;
                hareket.AlisFiyati = sinyal.AlisFiyati;
                hareket.MomentumYuzde = sinyal.Yukselis;
                DatabaseManager.YutanMumHareketEkle(hareket);
                basarili++;

                if (ozet.Length > 0) ozet += ", ";
                ozet += string.Format("{0}({1:F1}%)", sinyal.HisseAdi, sinyal.Yukselis);

                Sistem.Mesaj(string.Format("[SEMIH] ALIS: {0} Lot:{1} Fiyat:{2:F2} Yuk:{3:F1}%",
                    sinyal.HisseAdi, sinyal.Lot, sinyal.AlisFiyati, sinyal.Yukselis));
            }
            catch { }
        }

        DatabaseManager.RiskDetayEkle("OvernightSemih",
            string.Format("Batch#{0} {1}/{2} basarili. Tutar:{3:F0} | {4}",
                batchId, basarili, sinyaller.Count, toplamAlimTutari, ozet));
    }

    private static void AktifBatchleriKontrolEt(dynamic Sistem)
    {
        var aktifBatchler = DatabaseManager.YutanMumAktifBatchlerGetir();
        foreach (var batch in aktifBatchler)
        {
            try
            {
                if (batch.RobotAdi != "OvernightSemih")
                    continue;

                var hareketler = DatabaseManager.YutanMumBatchHareketlerGetir(batch.Id);
                if (hareketler.Count == 0)
                {
                    DatabaseManager.YutanMumBatchKapat(batch.Id, 0, "BOS");
                    continue;
                }

                bool farkliGun = batch.BatchTarihi.Date < System.DateTime.Now.Date;
                if (farkliGun)
                {
                    BatchSat(Sistem, batch, hareketler, "OVERNIGHT");
                }
            }
            catch { }
        }
    }

    private static void BatchSat(dynamic Sistem, YutanMumBatch batch,
        System.Collections.Generic.List<YutanMumHareket> hareketler, string neden)
    {
        double toplamKar = 0;
        int satilan = 0;

        foreach (var hareket in hareketler)
        {
            try
            {
                double satisFiyati = IdealManager.SatisFiyatiGetir(Sistem, hareket.HisseAdi);
                if (satisFiyati <= 0)
                    continue;

                // Zararda + KademeStrateji aktif ise → Grid'e devret
                if (neden == "OVERNIGHT" && satisFiyati < hareket.AlisFiyati)
                {
                    Hisse hisse = DatabaseManager.HisseGetir(hareket.HisseAdi);
                    if (hisse != null && hisse.AlisAktif && hisse.SatisAktif)
                    {
                        var gridPoz = new HisseHareket();
                        gridPoz.Id = 0;
                        gridPoz.HisseAdi = hareket.HisseAdi;
                        gridPoz.Lot = hareket.Lot;
                        gridPoz.AlisFiyati = hareket.AlisFiyati;
                        gridPoz.SatisFiyati = 0;
                        gridPoz.RobotAdi = "OvernightSemih";
                        gridPoz.PozisyonTipi = 0;
                        DatabaseManager.HisseHareketEkleGuncelle(gridPoz);

                        DatabaseManager.YutanMumHareketSat(hareket.Id, hareket.AlisFiyati);
                        satilan++;

                        Sistem.Mesaj(string.Format("[SEMIH] GRID DEVIR: {0} Lot:{1} Alis:{2:F2} Guncel:{3:F2}",
                            hareket.HisseAdi, hareket.Lot, hareket.AlisFiyati, satisFiyati));
                        continue;
                    }
                }

                // *** EMIR GONDERME — test doneminde comment-out ***
                // IdealManager.Sat(Sistem, hareket.HisseAdi, hareket.Lot, satisFiyati);
                DatabaseManager.YutanMumHareketSat(hareket.Id, satisFiyati);

                double kar = hareket.Lot * (satisFiyati - hareket.AlisFiyati);
                toplamKar += kar;
                satilan++;

                Sistem.Mesaj(string.Format("[SEMIH] SATIS: {0} Lot:{1} Fiyat:{2:F2} Kar:{3:F2}",
                    hareket.HisseAdi, hareket.Lot, satisFiyati, kar));
            }
            catch { }
        }

        if (satilan >= hareketler.Count)
        {
            DatabaseManager.YutanMumBatchKapat(batch.Id, toplamKar, neden);
        }

        DatabaseManager.RiskDetayEkle("OvernightSemih",
            string.Format("Batch#{0} {1}. Satilan:{2}/{3} Kar:{4:F2}",
                batch.Id, neden, satilan, hareketler.Count, toplamKar));
    }
}

public class OvernightSemihSinyal
{
    public string HisseAdi;
    public double Yukselis;
    public double AlisFiyati;
    public int Lot;
}
