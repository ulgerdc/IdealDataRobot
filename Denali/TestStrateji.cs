
public class TestStrateji
{
    public static void Baslat(dynamic Sistem, string hisseAdi)
    {
        //Sistem.Debug("adadasdsada");

        var hisse = DatabaseManager.HisseGetir(hisseAdi);
        double yuksekGun = IdealManager.YuksekGunGetir(Sistem, "AKBNK");
        //Sistem.Debug(yuksekGun.ToString());
        RiskYoneticisi.RiskHesapla(Sistem, hisse,40.90D,0.16D);
        RiskYoneticisi.EndeksDegerlendir(Sistem, hisse);

        //Sistem.Debug("adadasdsada");
    }
}
