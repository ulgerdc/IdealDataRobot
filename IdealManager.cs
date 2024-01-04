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

        public static void HesapOku(dynamic Sistem)
        {
            var BistHesap = Sistem.BistHesapOku();
            var Limit = BistHesap.IslemLimit;
            var Bakiye = BistHesap.Bakiye;
            Pozisyonlar PozList = BistHesap.Pozisyonlar;
            Emirler[] BekleyenList = BistHesap.BekleyenEmirler;
            Emirler[] GerceklesenList = BistHesap.GerceklesenEmirler;

            Sistem.Debug("Debug");

        }

        public static double AlisFiyatiGetir(dynamic Sistem, string hisse)
        {
            if (Sistem == null)
                return 0.05D;

            return Sistem.AlisFiyat("IMKBH'" + hisse);

        }
        public static void Al(dynamic Sistem, Hisse hisse, int lot, double fiyat)
        {

            Sistem.EmirSembol = "IMKBH'" + hisse.HisseAdi;
            Sistem.EmirIslem = "Alış";
            Sistem.EmirMiktari = lot;
            Sistem.EmirFiyati = "Limit"; //veya Piyasa
            Sistem.EmirSuresi = "GUN"; // SEANS, GUN
            Sistem.EmirTipi = "NORMAL"; // NORMAL, KIE, KPY, AFE/KAFE
            Sistem.EmirSatisTipi = "NORMAL"; // imkb (NORMAL, ACIGA, VIRMANDAN)
            //Sistem.EmirHesapAdi = "123456, ABC YATIRIM";
            Sistem.EmirGonder();

            Sistem.PozisyonKontrolGuncelle(Sistem.EmirSembol, lot);
            Sistem.PozisyonKontrolOku(Sistem.EmirSembol);


        }

        public static void Sat(dynamic Sistem, Hisse hisse, int lot, double fiyat)
        {

            Sistem.EmirSembol = "IMKBH'" + hisse.HisseAdi;
            Sistem.EmirIslem = "Satış";
            Sistem.EmirTipi = "Limit";
            Sistem.EmirSuresi = "GUN";
            Sistem.EmirMiktari = lot;
            Sistem.EmirGonder();

        }

        public static double TabanFiyat(dynamic Sistem, Hisse hisse)
        {
            double tabanFiyat = Sistem.Taban("IMKBH'" + hisse.HisseAdi);
            return tabanFiyat;
        }

        public static double TavanFiyat(dynamic Sistem, Hisse hisse)
        {
            double tavanFiyat = Sistem.Tavan("IMKBH'" + hisse.HisseAdi);
            return tavanFiyat;
        }

        public static double KademeFiyatiGetir(dynamic Sistem, string hisse)
        {
            double tavanFiyat = Sistem.Tavan("IMKBH'" + hisse);
            return tavanFiyat;
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
