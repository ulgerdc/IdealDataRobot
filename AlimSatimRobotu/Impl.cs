using AlimSatimRobotu.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace AlimSatimRobotu
{
    internal class Impl
    {
        public static void Start(dynamic Sistem, string hisseAdi, double sonfiyat)
        {
            Sistem.AlgoIslem = "OK";
            if(!Sistem.BaglantiVar)
            {
                return;
            }

            double marj = 0;
            int lot;

            var hisse = DatabaseManager.HisseGetir(hisseAdi);
            if (hisse is null)
                return;
            if (hisse.MarjTipi == MarjTipiEnum.Kademe)
            {
                var kademeFiyati = IdealManager.KademeFiyatiGetir(null, hisse.HisseAdi);
                marj = hisse.Marj * kademeFiyati;
            }  
            else if(hisse.MarjTipi == MarjTipiEnum.Binde)
                marj = IdealManager.DivideAndNoRound(hisse.Marj, 1000) * sonfiyat;

            var risk = RiskYoneticisi.AlisKontrolleri(hisse);
            marj = marj * risk;
            lot = IdealManager.DivideAndRoundToInt(hisse.AlimTutari, sonfiyat);

            var satisKontrol = DatabaseManager.HisseSatimKontrol(hisseAdi, sonfiyat, marj);
            if (satisKontrol.Item1 > 0)
            {
                IdealManager.Sat(Sistem, hisse, satisKontrol.Item1, sonfiyat);
                foreach (var item in satisKontrol.Item2)
                {
                    item.SatisFiyati = sonfiyat;
                    item.AktifMi = false;
                    DatabaseManager.HisseHareketEkleGuncelle(item);
                }
            }
            else
            {
                Console.WriteLine($"{hisseAdi} {sonfiyat} satis icin uygun degil");
            }

          
            var hisseAlimKontrol = DatabaseManager.HisseAlimKontrol(hisseAdi, sonfiyat, marj);
            if (hisseAlimKontrol)
            {
                IdealManager.Al(Sistem, hisse, lot, sonfiyat);
                var hisseAl = new HisseHareket();
                hisseAl.AlisFiyati = sonfiyat;
                hisseAl.HisseAdi = hisseAdi;
                hisseAl.Lot = lot;
                DatabaseManager.HisseHareketEkleGuncelle(hisseAl);
            }
            else
            {
                Console.WriteLine($"{hisseAdi} {sonfiyat} alis icin uygun degil");
            }
           
        }
    }
}
