

public class RiskYoneticisi
{
    static System.Collections.Generic.List<int> customFibonacciList = new System.Collections.Generic.List<int> { 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144 };
    public static int RiskHesapla(Hisse hisse)
    {
        var hissePozisyonlari = DatabaseManager.AcikHissePozisyonlariGetir(hisse.HisseAdi);
        var rtn = 1;
        if (hissePozisyonlari == null)
            return rtn;

        var percentage = IdealManager.CalculatePercentage(hissePozisyonlari.AcikPozisyonAlimTutari, hisse.Butce);

        if (hisse.ButceLimitYuzde > 0 && percentage >= hisse.ButceLimitYuzde)
        {
            DatabaseManager.RiskDetayEkle(hisse.HisseAdi, string.Format("Butce hard limiti: %{0} >= %{1} - alim durduruldu", System.Math.Round(percentage, 1), hisse.ButceLimitYuzde));
            return 0;
        }

        if (percentage < 30)
        {
            rtn = customFibonacciList[0];
        }
        else if (percentage >= 30 && percentage < 50)
        {
            rtn = customFibonacciList[1];
        }
        else if (percentage >= 50 && percentage < 70)
        {
            rtn = customFibonacciList[2];
        }
        else if (percentage >= 70 && percentage < 90)
        {
            rtn = customFibonacciList[3];
        }
        else if (percentage >= 90)
        {
            rtn = customFibonacciList[4];
        }

        return rtn;

    }

    public static int RiskHesapla(dynamic Sistem, Hisse hisse, double alistutari, double marj)
    {
        double yuksekGun = IdealManager.YuksekGunGetir(Sistem, hisse.HisseAdi);
        if (yuksekGun > 0 && alistutari + marj >= yuksekGun)
        {
            DatabaseManager.RiskDetayEkle(hisse.HisseAdi, string.Format("YuksekGunGetir izin vermedi alistutari {0} marj {1}", alistutari, marj));
            return 0;
        }

        return RiskHesapla(hisse);
    }

    public static int EndeksDegerlendir(dynamic Sistem, Hisse hisse)
    {
        double bist100Yuzde = IdealManager.Bist100EndeksYuzde(Sistem);
        double bist30Yuzde = IdealManager.Bist30EndeksYuzde(Sistem);
        double viop30Yuzde = IdealManager.Viop30EndeksYuzde(Sistem);
        double hisseYuzde = IdealManager.HisseYuzde(Sistem, hisse.HisseAdi);
        int rtn = 0;

        try
        {
            if (bist100Yuzde > 0 && bist30Yuzde > 0 && viop30Yuzde > 0 && hisseYuzde > 0)
            {
                rtn = customFibonacciList[0];
                return rtn;
            }

            if (bist100Yuzde < 0 && bist30Yuzde < 0 && viop30Yuzde < 0)
                rtn = 1;

            if (bist100Yuzde < -1 && bist30Yuzde < -1 && viop30Yuzde < -1)
                rtn = 2;

            if (bist100Yuzde < -2 && bist30Yuzde < -2 && viop30Yuzde < -2)
                rtn = 3;

            if (bist100Yuzde < -3 && bist30Yuzde < -3 && viop30Yuzde < -3)
                rtn = 4;

            if (bist100Yuzde < -5 && bist30Yuzde < -5 && viop30Yuzde < -5)
                rtn = 5;

            if (hisseYuzde > 1 && rtn > 1)
                rtn = rtn - 1;

            return customFibonacciList[rtn];
        }
        catch (System.Exception ex)
        {
            return 1;
        }
    }



    public static int SabahCoskusuDegerlendir(dynamic Sistem, Hisse hisse)
    {
        double bist100Yuzde = IdealManager.Bist100EndeksYuzde(Sistem);
        double bist30Yuzde = IdealManager.Bist30EndeksYuzde(Sistem);
        double viop30Yuzde = IdealManager.Viop30EndeksYuzde(Sistem);
        double hisseYuzde = IdealManager.HisseYuzde(Sistem, hisse.HisseAdi);
        int rtn = 0;

        //try
        //{
        if (bist100Yuzde > 0 && bist30Yuzde > 0 && viop30Yuzde > 0 && hisseYuzde > 0)
            rtn = 1;

        return rtn;

    }

    public static int ArbitrajDegerlendir(dynamic Sistem, string hisseAdi)
    {
        int rtn = 1;
        return rtn;
    }


}