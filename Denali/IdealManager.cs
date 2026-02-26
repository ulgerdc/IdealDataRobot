
public class IdealManager
{
    static string hisseOrtam = "IMKBH'";
    static string viopOrtam = "VIP'";
    static string bist100 = "IMKBX'XU100";
    static string bist30 = "IMKBX'XU030";
    static string viop30 = "VIP'VIP-X030";

    public static bool ArbitrajSaatiKontrolEt(dynamic Sistem)
    {
        return (Sistem.Saat.CompareTo("10:05:00") <= 0 || Sistem.Saat.CompareTo("17:59:59") >= 0);
    }
    public static bool SaatiKontrolEt(dynamic Sistem)
    {
        return (Sistem.Saat.CompareTo("10:00:00") <= 0 || Sistem.Saat.CompareTo("17:59:59") >= 0);
    }
    public static bool AlisSaatiKontrolEt(dynamic Sistem)
    {
        return (Sistem.Saat.CompareTo("11:00:15") <= 0 || Sistem.Saat.CompareTo("17:59:59") >= 0);
    }

    public static bool SatisSaatiKontrolEt(dynamic Sistem)
    {
        return (Sistem.Saat.CompareTo("10:00:00") <= 0 || Sistem.Saat.CompareTo("17:59:59") >= 0);
    }

    public static bool SabahCoskusuAlimSaatiKontrolEt(dynamic Sistem)
    {
        return (Sistem.Saat.CompareTo("17:58:00") <= 0 || Sistem.Saat.CompareTo("17:59:59") >= 0);
    }

    public static void HesapOku(dynamic Sistem)
    {
        var BistHesap = Sistem.BistHesapOku();
        var Limit = BistHesap.IslemLimit;
        var Bakiye = BistHesap.Bakiye;
        //Pozisyonlar PozList = BistHesap.Pozisyonlar;
        //Emirler[] BekleyenList = BistHesap.BekleyenEmirler;
        //Emirler[] GerceklesenList = BistHesap.GerceklesenEmirler;


    }

    //piyasa satis fiyati bizim alis fiyatimiz
    public static double AlisFiyatiGetir(dynamic Sistem, string hisse)
    {
        if (Sistem == null)
            return 0.05D;

        return System.Math.Round(Sistem.SatisFiyat(hisseOrtam + hisse), 2);

    }

    //piyasa alis fiyati bizim alis fiyatimiz
    public static double SatisFiyatiGetir(dynamic Sistem, string hisse)
    {
        if (Sistem == null)
            return 0.05D;

        return System.Math.Round(Sistem.AlisFiyat(hisseOrtam + hisse), 2);

    }

    //piyasa satis fiyati bizim alis fiyatimiz
    public static double ViopAlisFiyatiGetir(dynamic Sistem, string hisse)
    {
        if (Sistem == null)
            return 0.05D;

        //VIP'F_ASELS0224

        string viopHisseAdi = ViopHisseAdiGetir(hisse);

        Sistem.Debug(viopHisseAdi);

        return System.Math.Round(Sistem.SatisFiyat(viopHisseAdi), 2);

    }

    private static string ViopHisseAdiGetir(string hisse)
    {
        return viopOrtam + "F_" + hisse + System.DateTime.Now.Month.ToString("d2") + System.DateTime.Now.ToString("yy");
    }

    //piyasa alis fiyati bizim alis fiyatimiz
    public static double ViopSatisFiyatiGetir(dynamic Sistem, string hisse)
    {
        if (Sistem == null)
            return 0.05D;

        string viopHisseAdi = ViopHisseAdiGetir(hisse);
        return System.Math.Round(Sistem.AlisFiyat(viopHisseAdi), 2);

    }

    public static double YuksekGunGetir(dynamic Sistem, string hisse)
    {
        return Sistem.YuksekGun(hisseOrtam + hisse);
    }

    public static double DusukGunGetir(dynamic Sistem, string hisse)
    {

        return Sistem.DusukGun(hisseOrtam + hisse);
    }

    public static double DusukGunGetirBist100(dynamic Sistem)
    {

        return Sistem.DusukGun(bist100);
    }

    public static double YuksekGunGetirBist100(dynamic Sistem)
    {

        return Sistem.YuksekGun(bist100);
    }

    public static double DusukGunGetirBist30(dynamic Sistem)
    {

        return Sistem.DusukGun(bist30);
    }

    public static double YuksekGunGetirBist30(dynamic Sistem)
    {

        return Sistem.YuksekGun(bist30);
    }

    public static double DusukGunGetirViop30(dynamic Sistem)
    {

        return Sistem.DusukGun(viop30);
    }

    public static double YuksekGunGetirViop30(dynamic Sistem)
    {

        return Sistem.YuksekGun(viop30);
    }

    public static double Bist100EndeksYuzde(dynamic Sistem)
    {
        var Endeks = Sistem.YuzeyselVeriOku(bist100);
        return Endeks.NetPerDay;
    }

    public static double Bist30EndeksYuzde(dynamic Sistem)
    {
        var Endeks = Sistem.YuzeyselVeriOku(bist30);
        return Endeks.NetPerDay;
    }

    public static double Viop30EndeksYuzde(dynamic Sistem)
    {
        var Endeks = Sistem.YuzeyselVeriOku(viop30);
        return Endeks.NetPerDay;
    }

    public static double HisseYuzde(dynamic Sistem,string hisse)
    {
        var Endeks = Sistem.YuzeyselVeriOku(hisseOrtam + hisse);
        return Endeks.NetPerDay;
    }
    public static void Al(dynamic Sistem, string hisseAdi, int lot, double fiyat)
    {

        Sistem.EmirSembol = "IMKBH'" + hisseAdi;
        Sistem.EmirIslem = "ALIS";
        Sistem.EmirSuresi = "GUN"; // SEANS, GUN
        Sistem.EmirTipi = "Limit"; // NORMAL, KIE, KPY, AFE/KAFE
        Sistem.EmirSatisTipi = "NORMAL"; // imkb (NORMAL, ACIGA, VIRMANDAN)
        Sistem.EmirMiktari = lot;
        Sistem.EmirFiyati = fiyat; //veya Piyasa

        Sistem.EmirGonder();

        Sistem.PozisyonKontrolGuncelle(Sistem.EmirSembol, lot);
        //Sistem.PozisyonKontrolOku(Sistem.EmirSembol);


    }

    public static void ViopAl(dynamic Sistem, string hisseAdi, int lot, double fiyat)
    {

        Sistem.EmirSembol = ViopHisseAdiGetir(hisseAdi);
        Sistem.EmirIslem = "ALIS";
        Sistem.EmirSuresi = "GUN"; // SEANS, GUN
        Sistem.EmirTipi = "Limitli"; // NORMAL, KIE, KPY, AFE/KAFE
        Sistem.EmirSatisTipi = "NORMAL"; // imkb (NORMAL, ACIGA, VIRMANDAN)
        Sistem.EmirMiktari = lot;
        Sistem.EmirFiyati = fiyat; //veya Piyasa

        Sistem.EmirGonder();

        Sistem.PozisyonKontrolGuncelle(Sistem.EmirSembol, lot);
        //Sistem.PozisyonKontrolOku(Sistem.EmirSembol);


    }

    public static void Sat(dynamic Sistem, string hisseAdi, int lot, double fiyat)
    {
        Sistem.EmirSembol = "IMKBH'" + hisseAdi;
        Sistem.EmirIslem = "SATIS";
        Sistem.EmirSuresi = "GUN";
        Sistem.EmirTipi = "Limit";
        Sistem.EmirSatisTipi = "NORMAL";
        Sistem.EmirMiktari = lot;
        Sistem.EmirFiyati = fiyat;
        Sistem.EmirGonder();

        Sistem.PozisyonKontrolGuncelle(Sistem.EmirSembol, lot);
    }

    public static void ViopSat(dynamic Sistem, string hisseAdi, int lot, double fiyat)
    {
        Sistem.EmirSembol = ViopHisseAdiGetir(hisseAdi);
        Sistem.EmirIslem = "SATIS";
        Sistem.EmirSuresi = "GUN";
        Sistem.EmirTipi = "Limitli";
        Sistem.EmirSatisTipi = "NORMAL";
        Sistem.EmirMiktari = lot;
        Sistem.EmirFiyati = fiyat;
        Sistem.EmirGonder();

        Sistem.PozisyonKontrolGuncelle(Sistem.EmirSembol, lot);
    }

    public static double TabanFiyat(dynamic Sistem, Hisse hisse)
    {
        double tabanFiyat = Sistem.Taban("IMKBH'" + hisse.HisseAdi);
        return tabanFiyat;
    }

    public static double TavanFiyat(dynamic Sistem, Hisse hisse)
    {
        double tavanFiyat = Sistem.Tavan("IMKBH'" + hisse.HisseAdi);
        return tavanFiyat;
    }

    public static double KademeFiyatiGetir(dynamic Sistem, string hisse)
    {
        double tavanFiyat = Sistem.Tavan("IMKBH'" + hisse);
        return tavanFiyat;
    }
    public static double SonFiyatGetir(double min, double max)
    {
        return GenerateRandomDouble(min, max);
    }

    static double GenerateRandomDouble(double minValue, double maxValue)
    {
        // .NET'te Random sınıfı kullanarak rastgele sayı üretme
        System.Random random = new System.Random();

        // Belirtilen aralıkta rastgele bir double sayı üretme
        double randomNumber = minValue + (maxValue - minValue) * random.NextDouble();

        // Noktadan sonraki ondalık basamak sayısını kontrol et
        int decimalPlaces = System.BitConverter.GetBytes(decimal.GetBits((decimal)randomNumber)[3])[2];
        int maxDecimalPlaces = 2;

        if (decimalPlaces > maxDecimalPlaces)
        {
            // Daha fazla ondalık basamak varsa sadece belirtilen kadarını kullan
            double power = System.Math.Pow(10, maxDecimalPlaces);
            randomNumber = System.Math.Truncate(randomNumber * power) / power;
        }

        return randomNumber;
    }

    public static double MakeTwoDigit(System.Double randomNumber)
    {
        int decimalPlaces = System.BitConverter.GetBytes(decimal.GetBits((decimal)randomNumber)[3])[2];
        int maxDecimalPlaces = 2;

        if (decimalPlaces > maxDecimalPlaces)
        {
            // Daha fazla ondalık basamak varsa sadece belirtilen kadarını kullan
            double power = System.Math.Pow(10, maxDecimalPlaces);
            randomNumber = System.Math.Truncate(randomNumber * power) / power;
        }

        return randomNumber;
    }

    public static int DivideAndRoundToInt(double numerator, double denominator)
    {
        if (denominator == 0)
        {
            // Sıfıra bölme hatası kontrolü
            throw new System.DivideByZeroException("Denominator cannot be zero.");
        }

        // Bölme işlemi ve sonucu tam sayıya yuvarlama
        double result = numerator / denominator;
        int roundedResult = System.Convert.ToInt32(result);

        return roundedResult;
    }

    public static double DivideAndNoRound(double numerator, double denominator)
    {
        if (denominator == 0)
        {
            // Sıfıra bölme hatası kontrolü
            throw new System.DivideByZeroException("Denominator cannot be zero.");
        }

        // Bölme işlemi ve sonucu aşağıya doğru yuvarlama
        double result = numerator / denominator;

        return result;
    }

    public static double DivideAndRound(double numerator, double denominator)
    {
        if (denominator == 0)
        {
            // Sıfıra bölme hatası kontrolü
            throw new System.DivideByZeroException("Denominator cannot be zero.");
        }

        // Bölme işlemi ve sonucu aşağıya doğru yuvarlama
        double result = numerator / denominator;
        result = System.Math.Floor(result);
        return result;
    }

    public static double CalculatePercentage(double numerator, double denominator)
    {
        if (denominator == 0)
        {
            // Sıfıra bölme hatası kontrolü
            throw new System.DivideByZeroException("Denominator cannot be zero.");
        }

        // Yüzde hesaplama formülü
        double percentage = (numerator / denominator) * 100;

        return percentage;
    }

    public static double YuzdeFarkiHesapla(double sayi1, double sayi2)
    {

        double fark = sayi1 - sayi2;
        double yuzdeFark = (fark / sayi2) * 100;

        return yuzdeFark;
    }

    public static double Bist30FiyatGetir(dynamic Sistem)
    {
        if (Sistem == null)
            return 0;

        return System.Math.Round(Sistem.SatisFiyat(bist30), 2);
    }

    public static string ViopSembolOlustur(string hisse, string vadeKodu)
    {
        return viopOrtam + "F_" + hisse + vadeKodu;
    }

    public static double ViopAlisFiyatiGetirVade(dynamic Sistem, string hisse, string vadeKodu)
    {
        if (Sistem == null)
            return 0;

        string sembol = ViopSembolOlustur(hisse, vadeKodu);
        return System.Math.Round(Sistem.SatisFiyat(sembol), 2);
    }

    public static double ViopSatisFiyatiGetirVade(dynamic Sistem, string hisse, string vadeKodu)
    {
        if (Sistem == null)
            return 0;

        string sembol = ViopSembolOlustur(hisse, vadeKodu);
        return System.Math.Round(Sistem.AlisFiyat(sembol), 2);
    }

    public static void ViopAlVade(dynamic Sistem, string hisseAdi, string vadeKodu, int lot, double fiyat)
    {
        Sistem.EmirSembol = ViopSembolOlustur(hisseAdi, vadeKodu);
        Sistem.EmirIslem = "ALIS";
        Sistem.EmirSuresi = "GUN";
        Sistem.EmirTipi = "Limitli";
        Sistem.EmirSatisTipi = "NORMAL";
        Sistem.EmirMiktari = lot;
        Sistem.EmirFiyati = fiyat;
        Sistem.EmirGonder();
        Sistem.PozisyonKontrolGuncelle(Sistem.EmirSembol, lot);
    }

    public static void ViopSatVade(dynamic Sistem, string hisseAdi, string vadeKodu, int lot, double fiyat)
    {
        Sistem.EmirSembol = ViopSembolOlustur(hisseAdi, vadeKodu);
        Sistem.EmirIslem = "SATIS";
        Sistem.EmirSuresi = "GUN";
        Sistem.EmirTipi = "Limitli";
        Sistem.EmirSatisTipi = "NORMAL";
        Sistem.EmirMiktari = lot;
        Sistem.EmirFiyati = fiyat;
        Sistem.EmirGonder();
        Sistem.PozisyonKontrolGuncelle(Sistem.EmirSembol, lot);
    }

    public static bool ArbitrajGelismisSaatiKontrolEt(dynamic Sistem)
    {
        return (Sistem.Saat.CompareTo("10:05:00") <= 0 || Sistem.Saat.CompareTo("17:50:00") >= 0);
    }

    public static bool HisseYesilMi(dynamic Sistem, string hisse)
    {
        try
        {
            var veri = Sistem.YuzeyselVeriOku(hisseOrtam + hisse);
            return veri.NetPerDay > 0;
        }
        catch
        {
            return false;
        }
    }

    public static bool YutanMumKontrol(dynamic Sistem, string hisse, double minHacimCarpani,
        out double dunkuAcilis, out double dunkuKapanis, out double bugunAcilis, out double bugunKapanis,
        out long dunkuHacim, out long bugunHacim)
    {
        dunkuAcilis = 0; dunkuKapanis = 0; bugunAcilis = 0; bugunKapanis = 0;
        dunkuHacim = 0; bugunHacim = 0;

        string sembol = hisseOrtam + hisse;

        // Kapanis verisi (zorunlu)
        var kapanislar = Sistem.GrafikFiyatOku(sembol, "G", "Kapanis");
        if (kapanislar == null || kapanislar.Count < 2)
            return false;

        int sonBar = kapanislar.Count - 1;
        bugunKapanis = kapanislar[sonBar];
        dunkuKapanis = kapanislar[sonBar - 1];

        // Acilis verisi
        try
        {
            var acilislar = Sistem.GrafikFiyatOku(sembol, "G", "Acilis");
            if (acilislar != null && acilislar.Count >= 2)
            {
                bugunAcilis = acilislar[sonBar];
                dunkuAcilis = acilislar[sonBar - 1];
            }
        }
        catch
        {
            // "Acilis" desteklenmiyorsa — engulfing kontrol yapilamaz
            return false;
        }

        if (bugunAcilis <= 0 || dunkuAcilis <= 0)
            return false;

        // Engulfing kontrolu: bugun yesil + onceki gunu yutma
        bool bugunYesil = bugunKapanis > bugunAcilis;
        bool yutma = bugunAcilis <= dunkuKapanis && bugunKapanis >= dunkuAcilis;

        if (!bugunYesil || !yutma)
            return false;

        // Hacim kontrolu (opsiyonel — calismiyorsa atlanir)
        try
        {
            var hacimler = Sistem.GrafikFiyatOku(sembol, "G", "Hacim");
            if (hacimler != null && hacimler.Count >= 2)
            {
                bugunHacim = (long)hacimler[sonBar];
                dunkuHacim = (long)hacimler[sonBar - 1];

                if (dunkuHacim > 0 && bugunHacim < (long)(dunkuHacim * minHacimCarpani))
                    return false;
            }
            // Hacim verisi yoksa — sadece pattern yeterli
        }
        catch
        {
            // "Hacim" desteklenmiyorsa — hacim kontrolu devre disi
        }

        return true;
    }

    public static bool MomentumKontrol(dynamic Sistem, string hisse,
        double closeThreshold, double minMomentum, double minHacimCarpani,
        out double bugunAcilis, out double bugunYuksek, out double bugunDusuk,
        out double bugunKapanis, out long bugunHacim,
        out double dunkuKapanis, out long dunkuHacim, out double momentumYuzde)
    {
        bugunAcilis = 0; bugunYuksek = 0; bugunDusuk = 0; bugunKapanis = 0;
        bugunHacim = 0; dunkuKapanis = 0; dunkuHacim = 0; momentumYuzde = 0;

        string sembol = hisseOrtam + hisse;

        // Kapanis verisi (zorunlu — bugun + dunku)
        var kapanislar = Sistem.GrafikFiyatOku(sembol, "G", "Kapanis");
        if (kapanislar == null || kapanislar.Count < 2)
            return false;

        int sonBar = kapanislar.Count - 1;
        bugunKapanis = kapanislar[sonBar];
        dunkuKapanis = kapanislar[sonBar - 1];

        // Acilis verisi
        try
        {
            var acilislar = Sistem.GrafikFiyatOku(sembol, "G", "Acilis");
            if (acilislar != null && acilislar.Count >= 1)
                bugunAcilis = acilislar[acilislar.Count - 1];
        }
        catch { return false; }

        if (bugunAcilis <= 0 || bugunKapanis <= 0)
            return false;

        // Yuksek verisi
        try
        {
            var yuksekler = Sistem.GrafikFiyatOku(sembol, "G", "Yuksek");
            if (yuksekler != null && yuksekler.Count >= 1)
                bugunYuksek = yuksekler[yuksekler.Count - 1];
        }
        catch { return false; }

        // Dusuk verisi
        try
        {
            var dusukler = Sistem.GrafikFiyatOku(sembol, "G", "Dusuk");
            if (dusukler != null && dusukler.Count >= 1)
                bugunDusuk = dusukler[dusukler.Count - 1];
        }
        catch { return false; }

        if (bugunYuksek <= 0 || bugunDusuk <= 0)
            return false;

        // 1. Close zirveye yakin: (C-L)/(H-L) >= closeThreshold
        double range = bugunYuksek - bugunDusuk;
        if (range <= 0)
            return false;
        double closePos = (bugunKapanis - bugunDusuk) / range;
        if (closePos < closeThreshold)
            return false;

        // 2. Gun ici momentum: (C-O)/O*100 >= minMomentum
        momentumYuzde = (bugunKapanis - bugunAcilis) / bugunAcilis * 100;
        if (momentumYuzde < minMomentum)
            return false;

        // 3. Dunden yukari: C > C[1]
        if (bugunKapanis <= dunkuKapanis)
            return false;

        // 4. Hacim artisi: V > V[1] * minHacimCarpani
        try
        {
            var hacimler = Sistem.GrafikFiyatOku(sembol, "G", "Hacim");
            if (hacimler != null && hacimler.Count >= 2)
            {
                bugunHacim = (long)hacimler[hacimler.Count - 1];
                dunkuHacim = (long)hacimler[hacimler.Count - 2];

                if (dunkuHacim > 0 && bugunHacim < (long)(dunkuHacim * minHacimCarpani))
                    return false;
            }
        }
        catch
        {
            // Hacim verisi yoksa — hacim kontrolu devre disi
        }

        return true;
    }

    public static bool YutanMumSaatiKontrolEt(dynamic Sistem, string islemSaati)
    {
        // islemSaati: "10:15" gibi bir format
        string saat = islemSaati + ":00";
        // 1 dakikalik pencere: islemSaati ~ islemSaati+1dk arasi aktif
        string saatBitis = islemSaati + ":59";
        return (Sistem.Saat.CompareTo(saat) < 0 || Sistem.Saat.CompareTo(saatBitis) > 0);
    }

    public static double EndeksEMAKontrol(dynamic Sistem)
    {
        if (Sistem == null) return 1.0;

        var kapanislar = Sistem.GrafikFiyatOku(bist30, "G", "Kapanis");
        if (kapanislar == null) return 1.0;
        if (kapanislar.Count < 25) return 1.0;

        var ema8 = Sistem.MA(kapanislar, "Exp", 8);
        var ema21 = Sistem.MA(kapanislar, "Exp", 21);
        if (ema8 == null || ema21 == null) return 1.0;

        int sonBar = kapanislar.Count - 1;
        double sonEma8 = ema8[sonBar];
        double sonEma21 = ema21[sonBar];

        if (sonEma8 > 0 && sonEma21 > 0 && sonEma8 < sonEma21) return 0.5;
        return 1.0;
    }
}