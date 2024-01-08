using System;
using System.Data.SqlClient;
using System.Data;
using System.Reflection;
using System.Collections.Generic;

namespace ideal
{
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
                var kademeFiyati = (alisFiyati - satisFiyati);
                marj = hisse.Marj * kademeFiyati;
            }
            else if (hisse.MarjTipi == 1)//Binde
            {
                marj = IdealManager.DivideAndNoRound(hisse.Marj, 1000) * alisFiyati;
               
            }
               


            var risk = RiskYoneticisi.AlisKontrolleri(hisse);

            marj = marj * risk;
            marj = Math.Round(marj,2);

            lot = IdealManager.DivideAndRoundToInt(hisse.AlimTutari, alisFiyati);
            
            //lot = 1;

            if (IdealManager.SatisSaatiKontrolEt(Sistem) == false)
            {
                var satisKontrol = DatabaseManager.HisseSatimKontrol(hisseAdi, satisFiyati, marj);
                if (satisKontrol.Item1 > 0)
                {
                    IdealManager.Sat(Sistem, hisse, satisKontrol.Item1, satisFiyati);
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

                var hisseAlimKontrol = DatabaseManager.HisseAlimKontrol(hisseAdi, alisFiyati, marj);
                if (hisseAlimKontrol)
                {
                    IdealManager.Al(Sistem, hisse, lot, alisFiyati);
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
    public class DatabaseManager
    {
        static string connectionString = "Data Source=.;Initial Catalog=robot;User ID=sa;Password=1;Connection Timeout=30;Min Pool Size=5;Max Pool Size=15;Pooling=true;TrustServerCertificate=True;";
        private static SqlConnection OpenConnection()
        {
            var conn = new SqlConnection(connectionString);
            conn.Open();
            return conn;
        }

        public static Hisse HisseGetir(string hisseAdi)
        {

            Hisse hisse = null;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.Text;

                cmd.CommandText = string.Format("select * from Hisse where HisseAdi='{0}' and Aktif=1", hisseAdi);
                cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        hisse = MapFromDataReader<Hisse>(reader);
                    }
                }
            }

            return hisse;

        }

        public static HisseHareket HisseHareketGetir(string hisseAdi, double fiyat)
        {
            HisseHareket hisse = null;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.CommandText = "sel_HisseHareket";
                cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        hisse = MapFromDataReader<HisseHareket>(reader);

                    }
                }
            }

            return hisse;

        }

        public static HissePozisyonlari AcikHissePozisyonlariGetir(string hisseAdi)
        {
            HissePozisyonlari hissePozisyonlari = null;
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.CommandText = "[dbo].[sel_hissehareket]";
                cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        hissePozisyonlari = MapFromDataReader<HissePozisyonlari>(reader);

                    }
                }
            }

            return hissePozisyonlari;
        }

        public static Tuple<int, List<HisseHareket>> HisseSatimKontrol(string hisseAdi, double satisFiyati, double marj)
        {
            HisseHareket hisse = null;
            int satilacakLot = 0;
            List<HisseHareket> hisseHareketleri = new List<HisseHareket>();

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;

                cmd.CommandText = "[dbo].[sel_hisseSatimKontrol]";
                cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);
                cmd.Parameters.AddWithValue("@SatisFiyati", satisFiyati);
                cmd.Parameters.AddWithValue("@Marj", marj);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        hisse = MapFromDataReader<HisseHareket>(reader);
                        hisseHareketleri.Add(hisse);
                        satilacakLot += hisse.Lot;
                    }
                }
            }

            return new Tuple<int, List<HisseHareket>>(satilacakLot, hisseHareketleri);

        }

        public static bool HisseAlimKontrol(string hisseAdi, double alisFiyati, double marj)
        {
            bool al = true;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();

                SqlCommand cmd = connection.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "[dbo].[sel_hisseAlimKontrol]";
                cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);
                cmd.Parameters.AddWithValue("@AlisFiyati", alisFiyati);
                cmd.Parameters.AddWithValue("@Marj", marj);

                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        al = false;
                    }
                }
            }

            return al;

        }

        public static void HisseHareketEkleGuncelle(HisseHareket hisseHareket)
        {
            var conn = OpenConnection();

            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Connection = conn;
            cmd.CommandText = "ins_HisseHareket";

            cmd.Parameters.AddWithValue("@Id", hisseHareket.Id);
            cmd.Parameters.AddWithValue("@HisseAdi", hisseHareket.HisseAdi);
            cmd.Parameters.AddWithValue("@Lot", hisseHareket.Lot);
            cmd.Parameters.AddWithValue("@AlisFiyati", hisseHareket.AlisFiyati);
            cmd.Parameters.AddWithValue("@SatisFiyati", hisseHareket.SatisFiyati);
            cmd.Parameters.AddWithValue("@RobotAdi", hisseHareket.RobotAdi);
            cmd.ExecuteNonQuery();
            cmd.Connection.Close();

        }

        static T MapFromDataReader<T>(SqlDataReader reader) where T : new()
        {
            T instance = new T();

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string fieldName = reader.GetName(i);
                object value = reader.GetValue(i);

                PropertyInfo property = typeof(T).GetProperty(fieldName, BindingFlags.Public | BindingFlags.Instance);

                if (property != null && value != DBNull.Value)
                {
                    property.SetValue(instance, Convert.ChangeType(value, property.PropertyType));
                }
            }

            return instance;
        }



    }

    public class IdealManager
    {
        static string hisseOrtam = "IMKBH'";

        public static bool SaatiKontrolEt(dynamic Sistem)
        {
            return (Sistem.Saat.CompareTo("10:00:00") <= 0 || Sistem.Saat.CompareTo("17:59:59") >= 0);
        }
        public static bool AlisSaatiKontrolEt(dynamic Sistem)
        {
            return (Sistem.Saat.CompareTo("10:30:00") <= 0 || Sistem.Saat.CompareTo("17:59:59") >= 0);
        }

        public static bool SatisSaatiKontrolEt(dynamic Sistem)
        {
            return (Sistem.Saat.CompareTo("10:00:00") <= 0 || Sistem.Saat.CompareTo("17:59:59") >= 0);
        }

        public static void HesapOku(dynamic Sistem)
        {
            var BistHesap = Sistem.BistHesapOku();
            var Limit = BistHesap.IslemLimit;
            var Bakiye = BistHesap.Bakiye;
            //Pozisyonlar PozList = BistHesap.Pozisyonlar;
            //Emirler[] BekleyenList = BistHesap.BekleyenEmirler;
            //Emirler[] GerceklesenList = BistHesap.GerceklesenEmirler;

            Sistem.Debug("Debug");

        }

        //piyasa satis fiyati bizim alis fiyatimiz
        public static double AlisFiyatiGetir(dynamic Sistem, string hisse)
        {
            if (Sistem == null)
                return 0.05D;

            return Math.Round(Sistem.SatisFiyat(hisseOrtam + hisse),2);

        }

        //piyasa alis fiyati bizim alis fiyatimiz
        public static double SatisFiyatiGetir(dynamic Sistem, string hisse)
        {
            if (Sistem == null)
                return 0.05D;

            return Math.Round(Sistem.AlisFiyat(hisseOrtam + hisse),2);

        }

        public static void Al(dynamic Sistem, Hisse hisse, int lot, double fiyat)
        {
            
            Sistem.EmirSembol = "IMKBH'" + hisse.HisseAdi;
            Sistem.EmirIslem = "ALIS";
            Sistem.EmirSuresi = "GUN"; // SEANS, GUN
            Sistem.EmirTipi = "Limit"; // NORMAL, KIE, KPY, AFE/KAFE
            Sistem.EmirSatisTipi = "NORMAL"; // imkb (NORMAL, ACIGA, VIRMANDAN)
            Sistem.EmirMiktari = lot;
            Sistem.EmirFiyati = fiyat; //veya Piyasa

            Sistem.EmirGonder();

            Sistem.PozisyonKontrolGuncelle(Sistem.EmirSembol, lot);
            //Sistem.PozisyonKontrolOku(Sistem.EmirSembol);


        }

        public static void Sat(dynamic Sistem, Hisse hisse, int lot, double fiyat)
        {
            Sistem.EmirSembol = "IMKBH'" + hisse.HisseAdi;
            Sistem.EmirIslem = "SATIS";
            Sistem.EmirSuresi = "GUN";
            Sistem.EmirTipi = "Limit";
            Sistem.EmirSatisTipi = "NORMAL";
            Sistem.EmirMiktari = lot;
            Sistem.EmirFiyati = fiyat; 
            Sistem.EmirGonder();

            Sistem.PozisyonKontrolGuncelle(Sistem.EmirSembol, lot);
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
        public static double SonFiyatGetir(double min, double max)
        {
            return GenerateRandomDouble(min, max);
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

    public class RiskYoneticisi
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

            return rtn;
       
        }

     

    }

    public class Hisse
    {
        public long Id { get; set; }
        private string hisseAdi;
        public string HisseAdi
        {
            get
            {
                return hisseAdi;
            }
            set
            {
                hisseAdi = value;
            }
        }
        public double IlkFiyat { get; set; }
        public double Butce { get; set; }
        public double BaslangicKademe { get; set; }
        public double AlimTutari { get; set; }
        public double SonAlimTutari { get; set; }
        public DateTime PortfoyTarihi { get; set; }
        public DateTime SonIslemTarihi { get; set; }
        public int MarjTipi { get; set; }

        public int Marj { get; set; }

        public bool Aktif { get; set; }

    }

    public class HisseHareket
    {
        public long Id { get; set; }
        
        public string RobotAdi { get; internal set; }
        public string HisseAdi { get; internal set; }
        public double AlisFiyati { get; internal set; }
        public double SatisFiyati { get; internal set; }
        public int Lot { get; set; }
        public bool AktifMi { get; internal set; }
        public double Kar { get; internal set; }
    }
    public class HissePozisyonlari
    {
        public long Id { get; set; }
        public string HisseAdi { get; internal set; }
        public double ToplamAlisFiyati { get; internal set; }
        public double ToplamSatisFiyati { get; internal set; }
        public int AcikPozisyonSayisi { get; set; }
        public double AcikPozisyonAlimTutari { get; internal set; }
        public double ToplamKar { get; internal set; }
    }

}
