using AlimSatimRobotu.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlimSatimRobotu
{
    internal class RiskYoneticisi
    {
        //1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144, 23
        public static int AlisKontrolleri(Hisse hisse)
        {
            var hissePozisyonlari = DatabaseManager.AcikHissePozisyonlariGetir(hisse.HisseAdi);
            var rtn = 1;
            if (hissePozisyonlari == null)
                return rtn;

            var percentage = IdealManager.CalculatePercentage(hissePozisyonlari.ToplamAlisFiyati, hisse.Butce);

            if (percentage < 30)
            {
                rtn = 1;
            }
            else if (percentage > 30 && percentage < 50)
            {
                rtn = 2;
            }
            else if (percentage > 50 && percentage < 70)
            {
                rtn = 3;
            }
            else if (percentage > 70 && percentage < 90)
            {
                rtn = 5;
            }
            else if (percentage > 90 && percentage < 100)
            {
                rtn = 8;
            }

            switch (hisse.MarjTipi)
            {
                case MarjTipiEnum.Kademe:
                    return rtn;
                case MarjTipiEnum.Binde:
                    return rtn;
                case MarjTipiEnum.ATR:
                    return rtn;
                default:
                    return rtn;
            }
        }

    }
}
