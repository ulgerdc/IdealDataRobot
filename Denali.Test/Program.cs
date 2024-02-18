
using ideal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Denali.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            string hisse = "ASELS";

            var r = Math.Round(87.4499989,2);

            Lib lib = new Lib();

            var sonfiyat = IdealManager.SonFiyatGetir(42.00D, 43.90D);
            SistemMock sistemMock = new SistemMock();
            sistemMock.Name = "Test";

            ArbitrajStrateji.Baslat(sistemMock);

            //sonfiyat = 15.92D;
            for (int i = 0; i < 100; i++)
            {

                SabahCoskusuStrateji.Baslat(sistemMock);
                //ManuelAnalizStrateji.Baslat(sistemMock);

                lib.Baslat(sistemMock, hisse);

                sonfiyat = IdealManager.SonFiyatGetir(42.00D, 43.90D);
                try
                {

                    var Liste = Sistem.SembolAdListesi("VIP'VIP-", "");
                    var Metin = "";
                    foreach (var item in Liste)
                    {
                        Metin += item.Replace("VIP'VIP-","")+ ";"+ Sistem.SatisFiyat(item) + ";" + Sistem.AlisFiyat(item.Replace("VIP'VIP-", "IMKBH'"));
                        Metin +=  "\r\n";
                    }
                    Sistem.Mesaj(Metin);


                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }



                Thread.Sleep(1000);
            }
        }
    }
}
