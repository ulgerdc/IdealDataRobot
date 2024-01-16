
public class Lib
{
        public void Baslat(dynamic Sistem, string hisseAdi)
        {
            Sistem.Debug("Basladik" + Sistem.Name);

            Sistem.AlgoIslem = "OK";
            if (Sistem.BaglantiVar == false)
            {
                Sistem.Debug("Baglanti Yok");
                return;
            }

            if (IdealManager.SaatiKontrolEt(Sistem) == true)
            {
                Sistem.Debug("Saat Uygun Degil");
                return;
            }

            double alisFiyati = IdealManager.AlisFiyatiGetir(Sistem, hisseAdi);//303.25
            Sistem.Debug(string.Format("AlisFiyat {0}", alisFiyati));

            double satisFiyati = IdealManager.SatisFiyatiGetir(Sistem, hisseAdi);//303.00
            Sistem.Debug(string.Format("SatisFiyat {0}", satisFiyati));

            if (alisFiyati == 0 || satisFiyati == 0)
            {
                //devre kesmis
                return;
            }

            double marj = 0;
            int lot;

            var hisse = DatabaseManager.HisseGetir(hisseAdi);
            if (hisse == null)
            {
                hisse = DatabaseManager.HisseGetir("Default");
                if (hisse == null)
                {
                    Sistem.Debug(string.Format("Hisse Tablosunda {0} Bulunamadi", hisseAdi));
                    return;
                }
                hisse.HisseAdi = hisseAdi;
            }


            if (hisse.MarjTipi == 0)//kademe
            {
                var kademeFiyati = System.Math.Round((alisFiyati - satisFiyati), 2);
                marj = hisse.Marj * kademeFiyati;
            }
            else if (hisse.MarjTipi == 1)//Binde
            {
                marj = IdealManager.DivideAndNoRound(hisse.Marj, 1000) * alisFiyati;

            }

            if (IdealManager.SatisSaatiKontrolEt(Sistem) == false)
            {
                var satisKontrol = DatabaseManager.HisseSatimKontrol(hisseAdi, satisFiyati, marj);
                if (satisKontrol.Item1 > 0)
                {
                    IdealManager.Sat(Sistem, hisse.HisseAdi, satisKontrol.Item1, satisFiyati);
                    foreach (var item in satisKontrol.Item2)
                    {
                        item.SatisFiyati = satisFiyati;
                        item.AktifMi = false;
                        DatabaseManager.HisseHareketEkleGuncelle(item);
                    }
                }
                else
                {
                    Sistem.Debug(string.Format("{0} {1} satis icin uygun degil", hisseAdi, satisFiyati));
                }
            }


            if (IdealManager.AlisSaatiKontrolEt(Sistem) == false)
            {

                var risk = RiskYoneticisi.RiskHesapla(Sistem, hisse, alisFiyati, marj);
                if (risk > 0)
                {
                    marj = System.Math.Round(marj * risk, 2);

                    lot = IdealManager.DivideAndRoundToInt(hisse.AlimTutari, alisFiyati);

                    var hisseAlimKontrol = DatabaseManager.HisseAlimKontrol(hisseAdi, alisFiyati, marj);
                    if (hisseAlimKontrol)
                    {
                        IdealManager.Al(Sistem, hisse.HisseAdi, lot, alisFiyati);
                        var hisseAl = new HisseHareket();
                        hisseAl.AlisFiyati = alisFiyati;
                        hisseAl.HisseAdi = hisseAdi;
                        hisseAl.Lot = lot;
                        hisseAl.RobotAdi = Sistem.Name;
                        DatabaseManager.HisseHareketEkleGuncelle(hisseAl);
                    }
                    else
                    {
                        Sistem.Debug(string.Format("{0} {1} alis icin uygun degil", hisseAdi, alisFiyati));
                    }
                }
               
            }
        }

    public void ManuelAnalizBaslat(dynamic Sistem)
    {
        ManuelAnalizStrateji.Baslat(Sistem);
    }
}

