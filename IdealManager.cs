using AlimSatimRobotu.Entity;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace AlimSatimRobotu
{
    internal class IdealManager
    {
        public static bool SaatiKontrolEt(dynamic Sistem)
        {
            return !(Sistem.Saat.CompareTo("10:00:00") <= 0 || Sistem.Saat.CompareTo("17:59:59") >= 0);
        }

        public static double KademeFiyatiGetir(dynamic Sistem, string hisse)
        {
            if (Sistem == null)
                return 0.05D;

            var basicitem = Sistem.YuzeyselVeriOku(hisse);
            var sonfiyat = (double)basicitem.LastPrice;
            var bidfiyat = (double)basicitem.BidPriceDec;
            var askfiyat = (double)basicitem.AskPriceDec;

            return (askfiyat - bidfiyat);
        }
        public static void Al(dynamic Sistem, Hisse hisse, int lot, double fiyat)
        {

            Sistem.EmirSembol = "IMKBH'" + hisse.HisseAdi;
            Sistem.EmirIslem = "Satış";
            Sistem.EmirMiktari = lot;
            Sistem.EmirFiyati = "Aktif";
            Sistem.EmirSuresi = "SEANS"; // SEANS, GUN
            Sistem.EmirTipi = "NORMAL"; // NORMAL, KIE, KPY, AFE/KAFE
            Sistem.EmirSatisTipi = "NORMAL"; // imkb (NORMAL, ACIGA, VIRMANDAN)
            Sistem.EmirGonder();

            Sistem.PozisyonKontrolGuncelle(Sistem.EmirSembol, lot);
            Sistem.PozisyonKontrolOku(Sistem.EmirSembol);


        }

        public static void Sat(Hisse hisse, int lot, double fiyat)
        {

        }

        internal static double SonFiyatGetir(double min, double max)
        {
            return GenerateRandomDouble(min,max);
        }

        static double GenerateRandomDouble(double minValue, double maxValue)
        {
            // .NET'te Random sınıfı kullanarak rastgele sayı üretme
            Random random = new Random();

            // Belirtilen aralıkta rastgele bir double sayı üretme
            double randomNumber = minValue + (maxValue - minValue) * random.NextDouble();

            // Noktadan sonraki ondalık basamak sayısını kontrol et
            int decimalPlaces = BitConverter.GetBytes(decimal.GetBits((decimal)randomNumber)[3])[2];
            int maxDecimalPlaces = 2;

            if (decimalPlaces > maxDecimalPlaces)
            {
                // Daha fazla ondalık basamak varsa sadece belirtilen kadarını kullan
                double power = Math.Pow(10, maxDecimalPlaces);
                randomNumber = Math.Truncate(randomNumber * power) / power;
            }

            return randomNumber;
        }

        public static double MakeTwoDigit(Double randomNumber)
        {
            int decimalPlaces = BitConverter.GetBytes(decimal.GetBits((decimal)randomNumber)[3])[2];
            int maxDecimalPlaces = 2;

            if (decimalPlaces > maxDecimalPlaces)
            {
                // Daha fazla ondalık basamak varsa sadece belirtilen kadarını kullan
                double power = Math.Pow(10, maxDecimalPlaces);
                randomNumber = Math.Truncate(randomNumber * power) / power;
            }

            return randomNumber;
        }

        public static int DivideAndRoundToInt(double numerator, double denominator)
        {
            if (denominator == 0)
            {
                // Sıfıra bölme hatası kontrolü
                throw new DivideByZeroException("Denominator cannot be zero.");
            }

            // Bölme işlemi ve sonucu tam sayıya yuvarlama
            double result = numerator / denominator;
            int roundedResult = Convert.ToInt32(result);

            return roundedResult;
        }

        public static double DivideAndNoRound(double numerator, double denominator)
        {
            if (denominator == 0)
            {
                // Sıfıra bölme hatası kontrolü
                throw new DivideByZeroException("Denominator cannot be zero.");
            }

            // Bölme işlemi ve sonucu aşağıya doğru yuvarlama
            double result = numerator / denominator;
  
            return result;
        }

        public static double DivideAndRound(double numerator, double denominator)
        {
            if (denominator == 0)
            {
                // Sıfıra bölme hatası kontrolü
                throw new DivideByZeroException("Denominator cannot be zero.");
            }

            // Bölme işlemi ve sonucu aşağıya doğru yuvarlama
            double result = numerator / denominator;
            result = Math.Floor(result);
            return result;
        }

        public static double CalculatePercentage(double numerator, double denominator)
        {
            if (denominator == 0)
            {
                // Sıfıra bölme hatası kontrolü
                throw new DivideByZeroException("Denominator cannot be zero.");
            }

            // Yüzde hesaplama formülü
            double percentage = (numerator / denominator) * 100;

            return percentage;
        }
    }
}
