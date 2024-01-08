﻿using ideal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Denali.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var r = Math.Round(87.4499989,2);

            Lib lib = new Lib();

            var sonfiyat = IdealManager.SonFiyatGetir(92.65D, 92.90D);
            SistemMock sistemMock = new SistemMock();
            sistemMock.Name = "Test";
            //sonfiyat = 15.92D;
            for (int i = 0; i < 100; i++)
            {

                lib.Baslat(sistemMock, "ALFAS");

                sonfiyat = sonfiyat - IdealManager.KademeFiyatiGetir(sistemMock, "ALFAS");

                //sonfiyat = IdealManager.SonFiyatGetir(108.05D, 117.10D);
                //Impl.Start("ALFAS", sonfiyat);


                Thread.Sleep(1000);
            }
        }
    }
}
