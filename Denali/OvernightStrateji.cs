
public class OvernightSinyal
{
    public string HisseAdi;
    public string SinyalTipi; // "GK" veya "T2"
    public double Yukselis;
    public double CloseRange;
    public double HacimTL;
    public double AlisFiyati;
    public int Lot;
}

public class OvernightStrateji
{
    static string sonTaramaTarihi = "";

    // GK parametreleri
    static double gkMinYukselis = 3.0;
    static double gkMinCloseRange = 90.0;
    static int gkHacimPer = 20;
    static double gkHacimCarpan = 1.0;
    static bool gkHacimArtisi = true;

    // T2 parametreleri
    static double t2MinYukselis = 7.0;
    static double t2MaxYukselis = 10.0;
    static double t2MinCloseRange = 80.0;
    static int t2HacimPer = 30;
    static double t2HacimCarpan = 1.0;

    // Ortak
    static double minGunlukHacimTL = 5000000;
    static int maxGunlukPozisyon = 5;

    // Karaliste
    static System.Collections.Generic.HashSet<string> karaliste = KaralisteOlustur();

    private static System.Collections.Generic.HashSet<string> KaralisteOlustur()
    {
        var set = new System.Collections.Generic.HashSet<string>();
        set.Add("OBAMS"); set.Add("KGYO"); set.Add("LIDER"); set.Add("GESAN");
        set.Add("ESEN"); set.Add("PASEU"); set.Add("BLUME"); set.Add("ESCAR");
        set.Add("TRCAS"); set.Add("DERIM"); set.Add("YEOTK"); set.Add("YGGYO");
        set.Add("ASGYO"); set.Add("PENTA"); set.Add("ANHYT"); set.Add("GOLTS");
        set.Add("AKSA"); set.Add("FMIZP"); set.Add("DURKN"); set.Add("GSRAY");
        set.Add("CEOEM"); set.Add("AGHOL"); set.Add("EGGUB"); set.Add("AKBNK");
        set.Add("KRVGD"); set.Add("SKTAS"); set.Add("RYGYO"); set.Add("AKSGY");
        return set;
    }

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
        AktifBatchleriKontrolEt(Sistem, config);

        // Tarama: gun icinde 1 kez, belirlenen saatte
        string bugun = System.DateTime.Now.ToString("yyyy-MM-dd");
        if (sonTaramaTarihi == bugun)
            return;

        if (IdealManager.YutanMumSaatiKontrolEt(Sistem, config.IslemSaati))
            return;

        // Cuma gunu tarama yapma — haftasonu riski
        if (System.DateTime.Now.DayOfWeek == System.DayOfWeek.Friday)
        {
            sonTaramaTarihi = bugun;
            return;
        }

        // XU100 endeks filtresi — endeks negatifse sinyal uretme
        double xu100Yuzde = IdealManager.Bist100EndeksYuzde(Sistem);
        if (xu100Yuzde <= 0)
        {
            sonTaramaTarihi = bugun;
            return;
        }

        // Bugun zaten batch acildi mi?
        if (DatabaseManager.YutanMumBugunBatchVarMi())
        {
            sonTaramaTarihi = bugun;
            return;
        }

        // Max aktif batch kontrolu
        var aktifBatchler = DatabaseManager.YutanMumAktifBatchlerGetir();
        if (aktifBatchler.Count >= config.MaxAktifBatch)
        {
            sonTaramaTarihi = bugun;
            return;
        }

        // Aktif butce hesapla
        double kullanilanButce = 0;
        foreach (var batch in aktifBatchler)
        {
            kullanilanButce += batch.ToplamAlimTutari;
        }
        double kalanButce = config.ToplamButce - kullanilanButce;
        if (kalanButce < config.MinButcePerHisse)
        {
            sonTaramaTarihi = bugun;
            return;
        }

        YeniBatchOlustur(Sistem, config, kalanButce);
        sonTaramaTarihi = bugun;
    }

    private static void YeniBatchOlustur(dynamic Sistem, YutanMumConfig config, double aktifButce)
    {
        var sinyaller = new System.Collections.Generic.List<OvernightSinyal>();

        try
        {
            // Tum Y+A pazar hisselerini tara
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

                    // Varant filtresi (6+ karakter = varant/sertifika)
                    if (hisse.Length > 5)
                        continue;

                    // Pre-filter: en az %3 yukselis (canli veri)
                    double netPerDay = 0;
                    try
                    {
                        var veri = Sistem.YuzeyselVeriOku(fullName);
                        netPerDay = veri.NetPerDay;
                        if (netPerDay < 3.0)
                            continue;
                    }
                    catch { continue; }

                    // Canli fiyatlarla sinyal kontrolu
                    // GrafikFiyatOku gun kapanmadan bugunku bar'i vermez
                    // Bu yuzden canli fiyatlarla kontrol ediyoruz
                    double alisFiyati = IdealManager.AlisFiyatiGetir(Sistem, hisse);
                    if (alisFiyati <= 0) continue;

                    double satisFiyati = IdealManager.SatisFiyatiGetir(Sistem, hisse);
                    double bugunKapanis = satisFiyati > 0 ? satisFiyati : alisFiyati;
                    double bugunYuksek = IdealManager.YuksekGunGetir(Sistem, hisse);
                    double bugunDusuk = IdealManager.DusukGunGetir(Sistem, hisse);
                    // Bugunku acilis fiyati: GrafikVerileriniOku bugunun tamamlanmamis barini doner
                    // GrafikFiyatOku("G") ise sadece kapanmis barlari doner (bugun yok!)
                    double bugunAcilis = 0;
                    try
                    {
                        var sonBar = Sistem.GrafikVerileriniOku("IMKBH'" + hisse, "G");
                        if (sonBar != null)
                            bugunAcilis = sonBar.Open;
                    }
                    catch { }
                    // Fallback: acilis alinamadiysa, CR+yukselis filtreleri zaten yeterli
                    if (bugunAcilis <= 0)
                        bugunAcilis = bugunKapanis - 1; // yesil mum kontrolunu gecir

                    if (bugunKapanis <= 0 || bugunAcilis <= 0) continue;

                    // Yesil mum kontrolu
                    if (bugunKapanis <= bugunAcilis) continue;

                    // Close Range
                    double range = bugunYuksek - bugunDusuk;
                    double closeRange = 0;
                    if (range > 0) closeRange = (bugunKapanis - bugunDusuk) / range * 100;

                    // NOT: GrafikFiyatOku("G","Hacim") tum sifir donuyor — hacim verisi yok
                    // Hacim filtresi devre disi

                    double yukselis = netPerDay; // canli NetPerDay kullan

                    // T2 sinyal (7-10%)
                    if (yukselis >= t2MinYukselis && yukselis <= t2MaxYukselis
                        && closeRange >= t2MinCloseRange)
                    {
                        var t2sinyal = new OvernightSinyal();
                        t2sinyal.HisseAdi = hisse;
                        t2sinyal.SinyalTipi = "T2";
                        t2sinyal.Yukselis = yukselis;
                        t2sinyal.CloseRange = closeRange;
                        t2sinyal.HacimTL = 0;
                        t2sinyal.AlisFiyati = alisFiyati;
                        sinyaller.Add(t2sinyal);
                        continue;
                    }

                    // GK sinyal (>=3%)
                    if (yukselis >= gkMinYukselis
                        && closeRange >= gkMinCloseRange)
                    {
                        var gksinyal = new OvernightSinyal();
                        gksinyal.HisseAdi = hisse;
                        gksinyal.SinyalTipi = "GK";
                        gksinyal.Yukselis = yukselis;
                        gksinyal.CloseRange = closeRange;
                        gksinyal.HacimTL = 0;
                        gksinyal.AlisFiyati = alisFiyati;
                        sinyaller.Add(gksinyal);
                    }
                }
                catch { }
            }
        }
        catch { return; }

        if (sinyaller.Count == 0)
            return;

        // Yukselis'e gore siralama (bubble sort — IdealPro LINQ desteklemiyor)
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

        // Pozisyon limiti
        if (sinyaller.Count > maxGunlukPozisyon)
            sinyaller = sinyaller.GetRange(0, maxGunlukPozisyon);

        // Butce dagitimi
        double hisseBasiButce = aktifButce / sinyaller.Count;
        if (hisseBasiButce < config.MinButcePerHisse)
        {
            int maxHisse = (int)(aktifButce / config.MinButcePerHisse);
            if (maxHisse <= 0) return;
            if (sinyaller.Count > maxHisse)
                sinyaller = sinyaller.GetRange(0, maxHisse);
            hisseBasiButce = aktifButce / sinyaller.Count;
        }

        // Lot hesapla
        double toplamAlimTutari = 0;
        foreach (var sinyal in sinyaller)
        {
            sinyal.Lot = (int)(hisseBasiButce / sinyal.AlisFiyati);
            if (sinyal.Lot <= 0) sinyal.Lot = 1;
            toplamAlimTutari += sinyal.Lot * sinyal.AlisFiyati;
        }

        // Batch olustur
        long batchId = DatabaseManager.YutanMumBatchOlustur(Sistem.Name, sinyaller.Count, toplamAlimTutari);
        if (batchId <= 0)
            return;

        int basarili = 0;
        string sinyalOzet = "";
        foreach (var sinyal in sinyaller)
        {
            try
            {
                IdealManager.Al(Sistem, sinyal.HisseAdi, sinyal.Lot, sinyal.AlisFiyati);

                var hareket = new YutanMumHareket();
                hareket.BatchId = batchId;
                hareket.HisseAdi = sinyal.HisseAdi;
                hareket.Lot = sinyal.Lot;
                hareket.AlisFiyati = sinyal.AlisFiyati;
                hareket.MomentumYuzde = sinyal.Yukselis;
                hareket.BugunYuksek = sinyal.CloseRange;
                hareket.BugunDusuk = sinyal.HacimTL;

                DatabaseManager.YutanMumHareketEkle(hareket);
                basarili++;

                if (sinyalOzet.Length > 0) sinyalOzet += ", ";
                sinyalOzet += string.Format("{0}[{1}]", sinyal.HisseAdi, sinyal.SinyalTipi);

                Sistem.Mesaj(string.Format("[ON] ALIS: {0} [{1}] Lot:{2} Fiyat:{3:F2} Yukselis:{4:F1}% CR:{5:F0}%",
                    sinyal.HisseAdi, sinyal.SinyalTipi, sinyal.Lot, sinyal.AlisFiyati,
                    sinyal.Yukselis, sinyal.CloseRange));
            }
            catch { }
        }

        DatabaseManager.RiskDetayEkle("Overnight",
            string.Format("Batch#{0} olusturuldu. {1} Basarili:{2}/{3} Tutar:{4:F0}",
                batchId, sinyalOzet, basarili, sinyaller.Count, toplamAlimTutari));
    }

    public static void Test(dynamic Sistem)
    {
        Sistem.AlgoIslem = "OK";
        string log = "";

        try { log += string.Format("Bag:{0} Saat:{1}", Sistem.BaglantiVar, Sistem.Saat); }
        catch { log += "Bag:? Saat:?"; }

        double xu100Yuzde = 0;
        try { xu100Yuzde = IdealManager.Bist100EndeksYuzde(Sistem); }
        catch { }
        log += string.Format(" XU100:{0:F2}%", xu100Yuzde);

        int gkSinyal = 0, t2Sinyal = 0, toplamTaranan = 0;
        int listeCount = 0;
        string sinyalDetay = "";
        string ornekVeri = "";

        try
        {
            var tumHisseler = Sistem.YuzeyselListeGetir("IMKBH'");
            if (tumHisseler == null || tumHisseler.Count == 0)
            {
                Sistem.Mesaj(log + " | Liste:BOS");
                return;
            }

            listeCount = tumHisseler.Count;
            int ornekSayac = 0;

            for (int h = 0; h < tumHisseler.Count; h++)
            {
                try
                {
                    string fullName = tumHisseler[h].Symbol.ToString();
                    string hisse = fullName.Replace("IMKBH'", "").Trim();

                    if (karaliste.Contains(hisse))
                        continue;

                    // Varant filtresi (6+ karakter = varant/sertifika)
                    if (hisse.Length > 5)
                        continue;

                    double netPerDay = 0;
                    try
                    {
                        var veri = Sistem.YuzeyselVeriOku(fullName);
                        netPerDay = veri.NetPerDay;
                        if (ornekSayac < 5)
                        {
                            ornekVeri += string.Format(" {0}:{1:F1}", hisse, netPerDay);
                            ornekSayac++;
                        }
                        if (netPerDay < 3.0)
                            continue;
                    }
                    catch { continue; }

                    toplamTaranan++;
                    double yukselis = netPerDay;

                    // Canli fiyatlarla kontrol
                    double satisFiyati = IdealManager.SatisFiyatiGetir(Sistem, hisse);
                    double bugunYuksek = IdealManager.YuksekGunGetir(Sistem, hisse);
                    double bugunDusuk = IdealManager.DusukGunGetir(Sistem, hisse);
                    double bugunKapanis = satisFiyati > 0 ? satisFiyati : 0;
                    // Bugunku acilis fiyati: GrafikVerileriniOku bugunun tamamlanmamis barini doner
                    // GrafikFiyatOku("G") ise sadece kapanmis barlari doner (bugun yok!)
                    double bugunAcilis = 0;
                    try
                    {
                        var sonBar = Sistem.GrafikVerileriniOku("IMKBH'" + hisse, "G");
                        if (sonBar != null)
                            bugunAcilis = sonBar.Open;
                    }
                    catch { }
                    // Fallback: acilis alinamadiysa, CR+yukselis filtreleri zaten yeterli
                    if (bugunAcilis <= 0)
                        bugunAcilis = bugunKapanis - 1; // yesil mum kontrolunu gecir
                    if (bugunKapanis <= 0 || bugunAcilis <= 0) continue;
                    if (bugunKapanis <= bugunAcilis) continue; // yesil mum

                    double range = bugunYuksek - bugunDusuk;
                    double closeRange = range > 0 ? (bugunKapanis - bugunDusuk) / range * 100 : 0;
                    double hacimTL = 0;

                    // T2 sinyal
                    if (yukselis >= t2MinYukselis && yukselis <= t2MaxYukselis
                        && closeRange >= t2MinCloseRange)
                    {
                        t2Sinyal++;
                        sinyalDetay += string.Format(" T2:{0}({1:F1}%)", hisse, yukselis);
                        continue;
                    }

                    // GK sinyal
                    if (yukselis >= gkMinYukselis && closeRange >= gkMinCloseRange)
                    {
                        gkSinyal++;
                        sinyalDetay += string.Format(" GK:{0}({1:F1}% CR:{2:F0}%)", hisse, yukselis, closeRange);
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

        Sistem.Mesaj(string.Format("{0} | Liste:{1} Taran:{2} GK:{3} T2:{4}{5} |{6}",
            log, listeCount, toplamTaranan, gkSinyal, t2Sinyal, sinyalDetay, ornekVeri));
    }

    private static void AktifBatchleriKontrolEt(dynamic Sistem, YutanMumConfig config)
    {
        var aktifBatchler = DatabaseManager.YutanMumAktifBatchlerGetir();
        if (aktifBatchler.Count == 0)
            return;

        foreach (var batch in aktifBatchler)
        {
            try
            {
                var hareketler = DatabaseManager.YutanMumBatchHareketlerGetir(batch.Id);
                if (hareketler.Count == 0)
                {
                    DatabaseManager.YutanMumBatchKapat(batch.Id, 0, "BOS");
                    continue;
                }

                // Farkli gun ise ertesi sabah satisi
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

                // OVERNIGHT zararda + KademeStrateji aktif hissesiyse → Grid'e devret
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
                        gridPoz.RobotAdi = "Overnight";
                        gridPoz.PozisyonTipi = 0;
                        DatabaseManager.HisseHareketEkleGuncelle(gridPoz);

                        DatabaseManager.YutanMumHareketSat(hareket.Id, hareket.AlisFiyati);
                        satilan++;

                        Sistem.Mesaj(string.Format("[ON] GRID DEVIR: {0} Lot:{1} Alis:{2:F2} Guncel:{3:F2}",
                            hareket.HisseAdi, hareket.Lot, hareket.AlisFiyati, satisFiyati));
                        continue;
                    }
                }

                IdealManager.Sat(Sistem, hareket.HisseAdi, hareket.Lot, satisFiyati);
                DatabaseManager.YutanMumHareketSat(hareket.Id, satisFiyati);

                double kar = hareket.Lot * (satisFiyati - hareket.AlisFiyati);
                toplamKar += kar;
                satilan++;

                Sistem.Mesaj(string.Format("[ON] SATIS: {0} Lot:{1} Fiyat:{2:F2} Kar:{3:F2} Neden:{4}",
                    hareket.HisseAdi, hareket.Lot, satisFiyati, kar, neden));
            }
            catch { }
        }

        if (satilan >= hareketler.Count)
        {
            DatabaseManager.YutanMumBatchKapat(batch.Id, toplamKar, neden);
        }

        DatabaseManager.RiskDetayEkle("Overnight",
            string.Format("Batch#{0} {1}. Satilan:{2}/{3} ToplamKar:{4:F2}",
                batch.Id, neden, satilan, hareketler.Count, toplamKar));
    }
}
