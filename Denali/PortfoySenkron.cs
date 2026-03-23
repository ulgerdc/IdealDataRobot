
public class PortfoySenkron
{
    static string sonGuncelleme = "";

    public static void Baslat(dynamic Sistem)
    {
        Sistem.AlgoIslem = "OK";
        if (Sistem.BaglantiVar == false)
            return;

        if (IdealManager.SaatiKontrolEt(Sistem))
            return;

        // Her 5 dakikada bir guncelle
        string simdi = System.DateTime.Now.ToString("HH:mm");
        int dakika = System.DateTime.Now.Minute;
        if (dakika % 5 != 0)
            return;
        if (sonGuncelleme == simdi)
            return;

        try
        {
            DatabaseManager.IdealPortfoyTemizle();

            try
            {
                var hesap = Sistem.BistHesapOku();
                double bakiye = 0;
                double islemLimit = 0;
                try { bakiye = (double)hesap.Bakiye; } catch { }
                try { islemLimit = (double)hesap.IslemLimit; } catch { }
                DatabaseManager.IdealHesapGuncelle(bakiye, islemLimit);

                var pozlar = hesap.Pozisyonlar;
                if (pozlar == null)
                {
                    sonGuncelleme = simdi;
                    return;
                }

                int pozCount = 0;
                try { pozCount = pozlar.Count; } catch { }

                int eklenen = 0;
                for (int i = 0; i < pozCount; i++)
                {
                    try
                    {
                        var poz = pozlar[i];

                        // Symbol (kesfedildi)
                        string sembol = "";
                        try { sembol = poz.Symbol.ToString(); } catch { }
                        if (sembol == "") continue;

                        // Lot (kesfedildi)
                        int lot = 0;
                        try { lot = (int)poz.Lot; } catch { }
                        if (lot <= 0) continue;

                        // Cost = ort maliyet fiyati (kesfedildi)
                        double maliyet = 0;
                        try { maliyet = (double)poz.Cost; } catch { }

                        // LastPrice = guncel fiyat (kesfedildi)
                        double guncelFiyat = 0;
                        try { guncelFiyat = (double)poz.LastPrice; } catch { }

                        // ProfitX = kar/zarar TL (kesfedildi)
                        double karZarar = 0;
                        try { karZarar = (double)poz.ProfitX; } catch { }

                        // IMKBH' prefix'i kaldir
                        string hisse = sembol.Replace("IMKBH'", "");

                        DatabaseManager.IdealPortfoyEkle(hisse, lot, maliyet, guncelFiyat, karZarar);
                        eklenen++;
                    }
                    catch { }
                }
            }
            catch { }

            // Core senkron: fark kadar CoreSenkron pozisyonu olustur/guncelle
            try { DatabaseManager.CoreSenkronGuncelle(); } catch { }

            sonGuncelleme = simdi;
        }
        catch { }
    }
}
