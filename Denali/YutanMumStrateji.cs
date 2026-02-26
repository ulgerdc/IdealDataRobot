
public class YutanMumSinyal
{
    public string HisseAdi { get; set; }
    public double AlisFiyati { get; set; }
    public int Lot { get; set; }
    public double DunkuAcilis { get; set; }
    public double DunkuKapanis { get; set; }
    public double BugunAcilis { get; set; }
    public double BugunKapanis { get; set; }
    public long DunkuHacim { get; set; }
    public long BugunHacim { get; set; }
    public double BugunYuksek { get; set; }
    public double BugunDusuk { get; set; }
    public double MomentumYuzde { get; set; }
}

public class YutanMumStrateji
{
    static string sonTaramaTarihi = "";

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

        // Her dongude: aktif batch K/Z kontrol
        AktifBatchleriKontrolEt(Sistem, config);

        // Tarama: gun icinde 1 kez, belirlenen saatte
        string bugun = System.DateTime.Now.ToString("yyyy-MM-dd");
        if (sonTaramaTarihi == bugun)
            return;

        if (IdealManager.YutanMumSaatiKontrolEt(Sistem, config.IslemSaati))
            return;

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

        // Aktif batch'lerin kullandigi butceyi hesapla
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
        var hisseListesi = DatabaseManager.Bist100HisselerGetir();
        var sinyaller = new System.Collections.Generic.List<YutanMumSinyal>();

        foreach (var hisseAdi in hisseListesi)
        {
            try
            {
                // Pre-filter: yesil mi?
                if (!IdealManager.HisseYesilMi(Sistem, hisseAdi))
                    continue;

                double dunkuAcilis = 0, dunkuKapanis = 0, bugunAcilis = 0, bugunKapanis = 0;
                long dunkuHacim = 0, bugunHacim = 0;
                double bugunYuksek = 0, bugunDusuk = 0, momentumYuzde = 0;
                bool sinyal = false;

                if (config.SinyalTipi == 1) // Momentum
                {
                    sinyal = IdealManager.MomentumKontrol(Sistem, hisseAdi,
                        config.CloseThreshold, config.MinMomentum, config.MinHacimCarpani,
                        out bugunAcilis, out bugunYuksek, out bugunDusuk, out bugunKapanis,
                        out bugunHacim, out dunkuKapanis, out dunkuHacim, out momentumYuzde);
                }
                else // Engulfing (mevcut)
                {
                    sinyal = IdealManager.YutanMumKontrol(Sistem, hisseAdi, config.MinHacimCarpani,
                        out dunkuAcilis, out dunkuKapanis, out bugunAcilis, out bugunKapanis,
                        out dunkuHacim, out bugunHacim);
                }

                if (!sinyal)
                    continue;

                double alisFiyati = IdealManager.AlisFiyatiGetir(Sistem, hisseAdi);
                if (alisFiyati <= 0)
                    continue;

                sinyaller.Add(new YutanMumSinyal
                {
                    HisseAdi = hisseAdi,
                    AlisFiyati = alisFiyati,
                    DunkuAcilis = dunkuAcilis,
                    DunkuKapanis = dunkuKapanis,
                    BugunAcilis = bugunAcilis,
                    BugunKapanis = bugunKapanis,
                    DunkuHacim = dunkuHacim,
                    BugunHacim = bugunHacim,
                    BugunYuksek = bugunYuksek,
                    BugunDusuk = bugunDusuk,
                    MomentumYuzde = momentumYuzde
                });
            }
            catch
            {
                // Bir hisse hatasi digerlerini engellemesin
            }
        }

        if (sinyaller.Count == 0)
            return;

        // Butce dagitimi
        double hisseBasiButce = aktifButce / sinyaller.Count;
        if (hisseBasiButce < config.MinButcePerHisse)
        {
            // Sinyal cok fazla, butce yetmiyor — en fazla kac hisseye yetiyorsa o kadar al
            int maxHisse = (int)(aktifButce / config.MinButcePerHisse);
            if (maxHisse <= 0) return;
            // Ilk N sinyal (tarama sirasina gore)
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
                hareket.DunkuAcilis = sinyal.DunkuAcilis;
                hareket.DunkuKapanis = sinyal.DunkuKapanis;
                hareket.BugunAcilis = sinyal.BugunAcilis;
                hareket.BugunKapanis = sinyal.BugunKapanis;
                hareket.DunkuHacim = sinyal.DunkuHacim;
                hareket.BugunHacim = sinyal.BugunHacim;
                hareket.BugunYuksek = sinyal.BugunYuksek;
                hareket.BugunDusuk = sinyal.BugunDusuk;
                hareket.MomentumYuzde = sinyal.MomentumYuzde;

                DatabaseManager.YutanMumHareketEkle(hareket);
                basarili++;

                Sistem.Mesaj(string.Format("[YM] ALIS: {0} Lot:{1} Fiyat:{2:F2}", sinyal.HisseAdi, sinyal.Lot, sinyal.AlisFiyati));
            }
            catch
            {
                // Bir hisse hatasi digerlerini engellemesin
            }
        }

        DatabaseManager.RiskDetayEkle("YutanMum",
            string.Format("Batch#{0} olusturuldu. Sinyal:{1} Basarili:{2} Tutar:{3:F0}",
                batchId, sinyaller.Count, basarili, toplamAlimTutari));
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
                    // Aktif hareket kalmamis, batch'i kapat
                    DatabaseManager.YutanMumBatchKapat(batch.Id, 0, "BOS");
                    continue;
                }

                // Toplam K/Z hesapla
                double toplamAlim = 0;
                double toplamGuncel = 0;
                foreach (var hareket in hareketler)
                {
                    toplamAlim += hareket.Lot * hareket.AlisFiyati;
                    double guncelFiyat = IdealManager.SatisFiyatiGetir(Sistem, hareket.HisseAdi);
                    if (guncelFiyat <= 0) guncelFiyat = hareket.AlisFiyati; // Fiyat alinamazsa alis fiyatini kullan
                    toplamGuncel += hareket.Lot * guncelFiyat;
                }

                double toplamKZ = toplamGuncel - toplamAlim;

                // Gun sayisi kontrolu
                int gunSayisi = (int)(System.DateTime.Now - batch.BatchTarihi).TotalDays;

                // Satis kosullari
                string satisNedeni = null;
                bool farkliGun = batch.BatchTarihi.Date < System.DateTime.Now.Date;
                if (config.OvernightMod && farkliGun)
                    satisNedeni = "OVERNIGHT";
                else if (!config.OvernightMod && toplamKZ > 0)
                    satisNedeni = "KAR_ALDI";
                else if (!config.OvernightMod && gunSayisi >= config.MaxGunSayisi)
                    satisNedeni = "MAX_GUN";

                if (satisNedeni != null)
                {
                    BatchSat(Sistem, batch, hareketler, satisNedeni);
                }
            }
            catch
            {
                // Bir batch hatasi digerlerini engellemesin
            }
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
                    continue; // Devre kesici vb. — sonraki dongude tekrar dene

                // OVERNIGHT zararda + KademeStrateji hissesiyse → Grid'e devret, satma
                if (neden == "OVERNIGHT" && satisFiyati < hareket.AlisFiyati)
                {
                    Hisse hisse = DatabaseManager.HisseGetir(hareket.HisseAdi);
                    if (hisse != null)
                    {
                        // Grid pozisyonu olarak kaydet
                        var gridPoz = new HisseHareket();
                        gridPoz.Id = 0; // INSERT
                        gridPoz.HisseAdi = hareket.HisseAdi;
                        gridPoz.Lot = hareket.Lot;
                        gridPoz.AlisFiyati = hareket.AlisFiyati;
                        gridPoz.SatisFiyati = 0;
                        gridPoz.RobotAdi = "YutanMum";
                        gridPoz.PozisyonTipi = 0; // Grid
                        DatabaseManager.HisseHareketEkleGuncelle(gridPoz);

                        // YutanMum kaydini kapat (kar=0)
                        DatabaseManager.YutanMumHareketSat(hareket.Id, hareket.AlisFiyati);
                        satilan++;

                        Sistem.Mesaj(string.Format("[YM] GRID DEVIR: {0} Lot:{1} Alis:{2:F2} Guncel:{3:F2} Zarar:{4:F2}",
                            hareket.HisseAdi, hareket.Lot, hareket.AlisFiyati, satisFiyati,
                            hareket.Lot * (satisFiyati - hareket.AlisFiyati)));
                        continue;
                    }
                }

                IdealManager.Sat(Sistem, hareket.HisseAdi, hareket.Lot, satisFiyati);
                DatabaseManager.YutanMumHareketSat(hareket.Id, satisFiyati);

                double kar = hareket.Lot * (satisFiyati - hareket.AlisFiyati);
                toplamKar += kar;
                satilan++;

                Sistem.Mesaj(string.Format("[YM] SATIS: {0} Lot:{1} Fiyat:{2:F2} Kar:{3:F2} Neden:{4}",
                    hareket.HisseAdi, hareket.Lot, satisFiyati, kar, neden));
            }
            catch
            {
                // Bir hisse hatasi digerlerini engellemesin
            }
        }

        // Tum pozisyonlar satildiysa batch'i kapat
        if (satilan >= hareketler.Count)
        {
            DatabaseManager.YutanMumBatchKapat(batch.Id, toplamKar, neden);
        }

        DatabaseManager.RiskDetayEkle("YutanMum",
            string.Format("Batch#{0} {1}. Satilan:{2}/{3} ToplamKar:{4:F2}",
                batch.Id, neden, satilan, hareketler.Count, toplamKar));
    }
}
