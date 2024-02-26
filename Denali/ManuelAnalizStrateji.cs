
public class ManuelAnalizStrateji
{
    public static void Baslat(dynamic Sistem)
    {
        //Sistem.Debug("Basladik" + Sistem.Name);

        Sistem.AlgoIslem = "OK";
        if (Sistem.BaglantiVar == false)
        {
            //Sistem.Debug("Baglanti Yok");
            return;
        }

        if (IdealManager.SaatiKontrolEt(Sistem) == true)
        {
            //Sistem.Debug("Saat Uygun Degil");
            return;
        }

        var hisseEmirList = DatabaseManager.HisseEmirGetir(HisseEmirDurum.Satilabilir);
        foreach (var hisseEmir in hisseEmirList)
        {
            double satisFiyati = IdealManager.SatisFiyatiGetir(Sistem, hisseEmir.HisseAdi);
            if (satisFiyati == 0 || satisFiyati == 0)
            {
                //devre kesmis
                continue;
            }
            if (hisseEmir.SatisHedefi <= satisFiyati)
            {
                IdealManager.Sat(Sistem, hisseEmir.HisseAdi, hisseEmir.Lot, satisFiyati);
                hisseEmir.SatisTarihi = System.DateTime.Now;
                hisseEmir.Durum = HisseEmirDurum.KarAlindi.ToString();
                hisseEmir.Kar = System.Math.Round(satisFiyati - hisseEmir.AlisHedefi, 2);
                DatabaseManager.HisseEmirGuncelle(hisseEmir);
            }

            if (hisseEmir.StopHedefi >= satisFiyati)
            {
                IdealManager.Sat(Sistem, hisseEmir.HisseAdi, hisseEmir.Lot, satisFiyati);
                hisseEmir.SatisTarihi = System.DateTime.Now;
                hisseEmir.Durum = HisseEmirDurum.StopOldu.ToString();
                hisseEmir.Kar = System.Math.Round(satisFiyati - hisseEmir.AlisHedefi, 2);
                DatabaseManager.HisseEmirGuncelle(hisseEmir);
            }
        }

        hisseEmirList = DatabaseManager.HisseEmirGetir(HisseEmirDurum.Alinabilir);
        foreach (var hisseEmir in hisseEmirList)
        {
            double alisFiyati = IdealManager.AlisFiyatiGetir(Sistem, hisseEmir.HisseAdi);
            if (alisFiyati == 0 || alisFiyati == 0)
            {
                //devre kesmis
                continue;
            }
            if (hisseEmir.AlisHedefi >= alisFiyati)
            {
                IdealManager.Al(Sistem, hisseEmir.HisseAdi, hisseEmir.Lot, alisFiyati);
                hisseEmir.AlisTarihi = System.DateTime.Now;
                hisseEmir.Durum = HisseEmirDurum.Satilabilir.ToString();
                DatabaseManager.HisseEmirGuncelle(hisseEmir);
            }
        }
    }
}
