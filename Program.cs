using AlimSatimRobotu;
using AlimSatimRobotu.Entity;

var sonfiyat = IdealManager.SonFiyatGetir(92.65D, 92.90D);
SistemMock sistemMock = new SistemMock();
//sonfiyat = 15.92D;
for (int i = 0; i < 100; i++)
{
    
    Impl.Start(sistemMock, "ALFAS",sonfiyat);

    sonfiyat = sonfiyat - IdealManager.KademeFiyatiGetir(sistemMock, "ALFAS");

    //sonfiyat = IdealManager.SonFiyatGetir(108.05D, 117.10D);
    //Impl.Start("ALFAS", sonfiyat);


    Thread.Sleep(1000);
}

//double max = 20.88D;
//for (int i = 0; i < 25; i++)
//{
//    var sonfiyat = max;

//    Impl.Start(IdealManager.MakeTwoDigit(sonfiyat));
//    Thread.Sleep(1000);
//    max = max - 0.05D;
//}

Console.WriteLine("Hello, World!");
Console.ReadLine();


