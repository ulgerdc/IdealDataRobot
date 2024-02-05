
public class IdealManager
{
    static string hisseOrtam = "IMKBH'";
    static string bist100 = "IMKBX'XU100";
    static string bist30 = "IMKBX'XU030";
    static string viop30 = "VIP'VIP-X030";
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
}