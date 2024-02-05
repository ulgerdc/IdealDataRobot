
public class SabahCoskusuStrateji
{
    public static void Baslat(dynamic Sistem)
    {
        Sistem.Debug("Basladik" + Sistem.Name);

        Sistem.AlgoIslem = "OK";
        if (Sistem.BaglantiVar == false)
        {
            Sistem.Debug("Baglanti Yok");
            return;
        }

        if (IdealManager.SabahCoskusuAlimSaatiKontrolEt(Sistem) == true)
        {
            Sistem.Debug("Saat Uygun Degil");
            return;
        }

        var hisseList = DatabaseManager.SabahCoskusuGetir();
    
        foreach (var hisse in hisseList)
        {
            double alisFiyati = IdealManager.AlisFiyatiGetir(Sistem, hisse.HisseAdi);
            int lot;
            var al = DatabaseManager.SabahCoskusuKontrol(hisse.HisseAdi);
            if (al)
            {
                lot = IdealManager.DivideAndRoundToInt(hisse.AlimTutari, alisFiyati);

                if (RiskYoneticisi.EndeksDegerlendir(Sistem,hisse) == 1)
                {
                    IdealManager.Al(Sistem, hisse.HisseAdi, lot, alisFiyati);
                    var hisseAl = new HisseHareket();
                    hisseAl.AlisFiyati = alisFiyati;
                    hisseAl.HisseAdi = hisse.HisseAdi;
                    hisseAl.Lot = lot;
                    hisseAl.RobotAdi = Sistem.Name;
                    var sabahCoskusu = new SabahCoskusuHareket();
                    sabahCoskusu.AlisFiyati = alisFiyati;
                    sabahCoskusu.HisseAdi = hisse.HisseAdi;
                    sabahCoskusu.Lot = lot;
                    sabahCoskusu.RobotAdi = Sistem.Name;

                    DatabaseManager.SabahCoskusuHareketGuncelle(sabahCoskusu);
                    DatabaseManager.HisseHareketEkleGuncelle(hisseAl);
                }
                else
                {
                    Sistem.Debug(string.Format("{0} {1} alis icin uygun degil", hisse.HisseAdi, alisFiyati));
                }
            }
        }
    }

  
}
