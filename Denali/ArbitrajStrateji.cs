

public class ArbitrajStrateji
{
    public static void Baslat(dynamic Sistem)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        
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

        var hisseList = DatabaseManager.ArbitrajGetir();
        sb.AppendLine(hisseList.Count.ToString());
        foreach (var hisse in hisseList)
        {
            //sb.Append(hisse.HisseAdi);

            double bistSatisFiyati = IdealManager.AlisFiyatiGetir(Sistem, hisse.HisseAdi);
            double viopAlisFiyati = IdealManager.ViopAlisFiyatiGetir(Sistem, hisse.HisseAdi);

          

            if (bistSatisFiyati == 0 || viopAlisFiyati == 0)
                continue;

            var arbitrajHareket = DatabaseManager.ArbitrajKontrol(hisse.HisseAdi);
            if (arbitrajHareket != null)
            {
                sb.AppendLine("bist =" + bistSatisFiyati + "viop " +  viopAlisFiyati);

                if (bistSatisFiyati >= viopAlisFiyati)
                {
                    IdealManager.ViopAl(Sistem, hisse.HisseAdi, hisse.ViopLot, viopAlisFiyati);
                    IdealManager.Sat(Sistem, hisse.HisseAdi, hisse.BistLot, bistSatisFiyati);
                    arbitrajHareket.ViopAlisFiyati = viopAlisFiyati;
                    arbitrajHareket.BistSatisFiyati = bistSatisFiyati;
                    DatabaseManager.ArbitrajHareketGuncelle(arbitrajHareket);
                }
                Sistem.Mesaj(sb.ToString());
                continue;//yeni pozisyon acma
            }

            //sb.AppendLine("2");

            double bistAlisFiyati = IdealManager.AlisFiyatiGetir(Sistem, hisse.HisseAdi);
            double viopSatisFiyati = IdealManager.ViopSatisFiyatiGetir(Sistem, hisse.HisseAdi);

            if (bistAlisFiyati == 0 || viopSatisFiyati == 0)
                continue;
            var yuzde = IdealManager.YuzdeFarkiHesapla(viopSatisFiyati, bistAlisFiyati);

            sb.AppendLine(hisse.HisseAdi + "#" + yuzde.ToString() + "#" + bistSatisFiyati.ToString() + "#" + viopAlisFiyati.ToString());

            if (yuzde >= hisse.Marj)
            {
               
                if (RiskYoneticisi.ArbitrajDegerlendir(Sistem, hisse.HisseAdi) == 1)
                {
                    //Sistem.Mesaj("5");
                    IdealManager.ViopSat(Sistem, hisse.HisseAdi, hisse.ViopLot, viopSatisFiyati);
                    IdealManager.Al(Sistem, hisse.HisseAdi, hisse.BistLot, bistAlisFiyati);
                    var pozisyonAl = new ArbitrajHareket();
                    pozisyonAl.ViopSatisFiyati = viopSatisFiyati;
                    pozisyonAl.BistAlisFiyati = bistAlisFiyati;
                    pozisyonAl.HisseAdi = hisse.HisseAdi;
                    pozisyonAl.BistLot = hisse.BistLot;
                    pozisyonAl.ViopLot = hisse.ViopLot;
                    pozisyonAl.RobotAdi = Sistem.Name;
                    pozisyonAl.PozisyonTarih = System.DateTime.Now;

                    DatabaseManager.ArbitrajHareketGuncelle(pozisyonAl);

                    //sb.AppendLine("6");
                }
            }
            else 
            {
                //sb.AppendLine("7 " + yuzde.ToString() + " " + hisse.Marj);
            }

            Sistem.Mesaj(sb.ToString());
        }
    }


}
