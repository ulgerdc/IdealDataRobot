namespace ideal {

public class DatabaseManager
{
    static string connectionString = "Data Source=.;Initial Catalog=robot;User ID=sa;Password=1;Connection Timeout=30;Min Pool Size=5;Max Pool Size=15;Pooling=true;TrustServerCertificate=True;";
    private static System.Data.SqlClient.SqlConnection OpenConnection()
    {
        var conn = new System.Data.SqlClient.SqlConnection(connectionString);
        conn.Open();
        return conn;
    }

    public static Hisse HisseGetir(string hisseAdi)
    {

        Hisse hisse = null;
        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.Text;

            cmd.CommandText = string.Format("select * from Hisse where HisseAdi='{0}' and Aktif=1", hisseAdi);
            cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
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
        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.CommandText = "sel_HisseHareket";
            cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
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
        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.CommandText = "[dbo].[sel_hissehareket]";
            cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    hissePozisyonlari = MapFromDataReader<HissePozisyonlari>(reader);

                }
            }
        }

        return hissePozisyonlari;
    }

    public static System.Tuple<int, System.Collections.Generic.List<HisseHareket>> HisseSatimKontrol(string hisseAdi, double satisFiyati, double marj)
    {
        HisseHareket hisse = null;
        int satilacakLot = 0;
        var hisseHareketleri = new System.Collections.Generic.List<HisseHareket>();

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.CommandText = "[dbo].[sel_hisseSatimKontrol]";
            cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);
            cmd.Parameters.AddWithValue("@SatisFiyati", System.Math.Round(satisFiyati, 2));
            cmd.Parameters.AddWithValue("@Marj", marj);

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    hisse = MapFromDataReader<HisseHareket>(reader);
                    hisseHareketleri.Add(hisse);
                    satilacakLot += hisse.Lot;
                }
            }
        }

        return new System.Tuple<int, System.Collections.Generic.List<HisseHareket>>(satilacakLot, hisseHareketleri);

    }

    public static bool HisseAlimKontrol(string hisseAdi, double alisFiyati, double marj)
    {
        bool al = true;

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "[dbo].[sel_hisseAlimKontrol]";
            cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);
            cmd.Parameters.AddWithValue("@AlisFiyati", System.Math.Round(alisFiyati, 2));
            cmd.Parameters.AddWithValue("@Marj", marj);

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
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

        System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
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

    public static System.Collections.Generic.List<HisseEmir> HisseEmirGetir(HisseEmirDurum durum)
    {

        var hisseEmirList = new System.Collections.Generic.List<HisseEmir>();

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.Text;

            cmd.CommandText = string.Format("select * from HisseEmir where durum='{0}'", durum.ToString());
            //cmd.Parameters.AddWithValue("@durum", durum);

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    HisseEmir hisseEmir = MapFromDataReader<HisseEmir>(reader);
                    hisseEmirList.Add(hisseEmir);
                }
            }
        }

        return hisseEmirList;

    }

    public static void HisseEmirGuncelle(HisseEmir hisseEmir)
    {
        var conn = OpenConnection();

        System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.Text;
        cmd.Connection = conn;
        if (hisseEmir.SatisTarihi == System.DateTime.MinValue)
        {
            cmd.CommandText = "Update HisseEmir set Durum=@Durum, AlisTarihi=@AlisTarihi Where Id = @Id";
            cmd.Parameters.AddWithValue("@AlisTarihi", hisseEmir.AlisTarihi);
        }
        else
        {
            cmd.CommandText = "Update HisseEmir set Durum=@Durum, Satistarihi=@SatisTarihi, Kar =@Kar  Where Id = @Id";
            cmd.Parameters.AddWithValue("@Satistarihi", hisseEmir.SatisTarihi);
            cmd.Parameters.AddWithValue("@Kar", hisseEmir.Kar);
        }

        cmd.Parameters.AddWithValue("@Id", hisseEmir.Id);
        cmd.Parameters.AddWithValue("@Durum", hisseEmir.Durum);

        cmd.ExecuteNonQuery();
        cmd.Connection.Close();

    }

    public static void RiskDetayEkle(string hisseAdi, string data)
    {
        var conn = OpenConnection();

        System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Connection = conn;
        cmd.CommandText = "ins_RiskDetay";
        cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);
        cmd.Parameters.AddWithValue("@Data", data);
        cmd.ExecuteNonQuery();
        cmd.Connection.Close();

    }

    static T MapFromDataReader<T>(System.Data.SqlClient.SqlDataReader reader) where T : new()
    {
        T instance = new T();

        for (int i = 0; i < reader.FieldCount; i++)
        {
            string fieldName = reader.GetName(i);
            object value = reader.GetValue(i);

            System.Reflection.PropertyInfo property = typeof(T).GetProperty(fieldName, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);

            if (property != null && value != System.DBNull.Value)
            {
                property.SetValue(instance, System.Convert.ChangeType(value, property.PropertyType));
            }
        }

        return instance;
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
    public System.DateTime PortfoyTarihi { get; set; }
    public System.DateTime SonIslemTarihi { get; set; }
    public int MarjTipi { get; set; }

    public int Marj { get; set; }

    public bool Aktif { get; set; }

}

public class HisseEmir
{
    public long Id { get; set; }

    public string RobotAdi { get; internal set; }
    public string HisseAdi { get; internal set; }
    public int Lot { get; set; }
    public double AlisHedefi { get; internal set; }
    public double SatisHedefi { get; internal set; }
    public double StopHedefi { get; internal set; }

    public double Kar { get; internal set; }

    public System.DateTime AlisTarihi { get; internal set; }

    public System.DateTime SatisTarihi { get; internal set; }

    public string Durum { get; internal set; }//Alis, Satis, Stop, Kar

}




public enum HisseEmirDurum
{
    Alinabilir, 
    Satilabilir, 
    StopOldu, 
    KarAlindi
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

public class IdealManager
{
    static string hisseOrtam = "IMKBH'";
    static string bist100 = "IMKBX'XU100";
    static string bist30 = "IMKBX'XU30";
    static string viop30 = "VIP'VIP-X030";
    public static bool SaatiKontrolEt(dynamic Sistem)
    {
        return (Sistem.Saat.CompareTo("10:00:00") <= 0 || Sistem.Saat.CompareTo("17:59:59") >= 0);
    }
    public static bool AlisSaatiKontrolEt(dynamic Sistem)
    {
        return (Sistem.Saat.CompareTo("11:00:15") <= 0 || Sistem.Saat.CompareTo("17:59:59") >= 0);
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


    }

    //piyasa satis fiyati bizim alis fiyatimiz
    public static double AlisFiyatiGetir(dynamic Sistem, string hisse)
    {
        if (Sistem == null)
            return 0.05D;

        return System.Math.Round(Sistem.SatisFiyat(hisseOrtam + hisse), 2);

    }

    //piyasa alis fiyati bizim alis fiyatimiz
    public static double SatisFiyatiGetir(dynamic Sistem, string hisse)
    {
        if (Sistem == null)
            return 0.05D;

        return System.Math.Round(Sistem.AlisFiyat(hisseOrtam + hisse), 2);

    }

    public static double YuksekGunGetir(dynamic Sistem, string hisse)
    {
        return Sistem.YuksekGun(hisseOrtam + hisse);
    }

    public static double DusukGunGetir(dynamic Sistem, string hisse)
    {

        return Sistem.DusukGun(hisseOrtam + hisse);
    }

    public static double DusukGunGetirBist100(dynamic Sistem)
    {

        return Sistem.DusukGun(bist100);
    }

    public static double YuksekGunGetirBist100(dynamic Sistem)
    {

        return Sistem.YuksekGun(bist100);
    }

    public static double DusukGunGetirBist30(dynamic Sistem)
    {

        return Sistem.DusukGun(bist30);
    }

    public static double YuksekGunGetirBist30(dynamic Sistem)
    {

        return Sistem.YuksekGun(bist30);
    }

    public static double DusukGunGetirViop30(dynamic Sistem)
    {

        return Sistem.DusukGun(viop30);
    }

    public static double YuksekGunGetirViop30(dynamic Sistem)
    {

        return Sistem.YuksekGun(viop30);
    }

    public static double Bist100EndeksYuzde(dynamic Sistem)
    {
        var Endeks = Sistem.YuzeyselVeriOku(bist100);
        return Endeks.NetPerDay;
    }

    public static double Bist30EndeksYuzde(dynamic Sistem)
    {
        var Endeks = Sistem.YuzeyselVeriOku(bist30);
        return Endeks.NetPerDay;
    }

    public static double Viop30EndeksYuzde(dynamic Sistem)
    {
        var Endeks = Sistem.YuzeyselVeriOku(viop30);
        return Endeks.NetPerDay;
    }

    public static double HisseYuzde(dynamic Sistem,string hisse)
    {
        var Endeks = Sistem.YuzeyselVeriOku(hisseOrtam + hisse);
        return Endeks.NetPerDay;
    }
    public static void Al(dynamic Sistem, string hisseAdi, int lot, double fiyat)
    {

        Sistem.EmirSembol = "IMKBH'" + hisseAdi;
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

    public static void Sat(dynamic Sistem, string hisseAdi, int lot, double fiyat)
    {
        Sistem.EmirSembol = "IMKBH'" + hisseAdi;
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
        System.Random random = new System.Random();

        // Belirtilen aralıkta rastgele bir double sayı üretme
        double randomNumber = minValue + (maxValue - minValue) * random.NextDouble();

        // Noktadan sonraki ondalık basamak sayısını kontrol et
        int decimalPlaces = System.BitConverter.GetBytes(decimal.GetBits((decimal)randomNumber)[3])[2];
        int maxDecimalPlaces = 2;

        if (decimalPlaces > maxDecimalPlaces)
        {
            // Daha fazla ondalık basamak varsa sadece belirtilen kadarını kullan
            double power = System.Math.Pow(10, maxDecimalPlaces);
            randomNumber = System.Math.Truncate(randomNumber * power) / power;
        }

        return randomNumber;
    }

    public static double MakeTwoDigit(System.Double randomNumber)
    {
        int decimalPlaces = System.BitConverter.GetBytes(decimal.GetBits((decimal)randomNumber)[3])[2];
        int maxDecimalPlaces = 2;

        if (decimalPlaces > maxDecimalPlaces)
        {
            // Daha fazla ondalık basamak varsa sadece belirtilen kadarını kullan
            double power = System.Math.Pow(10, maxDecimalPlaces);
            randomNumber = System.Math.Truncate(randomNumber * power) / power;
        }

        return randomNumber;
    }

    public static int DivideAndRoundToInt(double numerator, double denominator)
    {
        if (denominator == 0)
        {
            // Sıfıra bölme hatası kontrolü
            throw new System.DivideByZeroException("Denominator cannot be zero.");
        }

        // Bölme işlemi ve sonucu tam sayıya yuvarlama
        double result = numerator / denominator;
        int roundedResult = System.Convert.ToInt32(result);

        return roundedResult;
    }

    public static double DivideAndNoRound(double numerator, double denominator)
    {
        if (denominator == 0)
        {
            // Sıfıra bölme hatası kontrolü
            throw new System.DivideByZeroException("Denominator cannot be zero.");
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
            throw new System.DivideByZeroException("Denominator cannot be zero.");
        }

        // Bölme işlemi ve sonucu aşağıya doğru yuvarlama
        double result = numerator / denominator;
        result = System.Math.Floor(result);
        return result;
    }

    public static double CalculatePercentage(double numerator, double denominator)
    {
        if (denominator == 0)
        {
            // Sıfıra bölme hatası kontrolü
            throw new System.DivideByZeroException("Denominator cannot be zero.");
        }

        // Yüzde hesaplama formülü
        double percentage = (numerator / denominator) * 100;

        return percentage;
    }
}

public class Lib
{
        public void Baslat(dynamic Sistem, string hisseAdi)
        {
           
            //Sistem.Debug("Basladik" + Sistem.Name);

            Sistem.AlgoIslem = "OK";
            if (Sistem.BaglantiVar == false)
            {
                Sistem.Debug("Baglanti Yok");
                return;
            }

            if (IdealManager.SaatiKontrolEt(Sistem) == true)
            {
                //Sistem.Debug("Saat Uygun Degil");
                return;
            }

            double alisFiyati = IdealManager.AlisFiyatiGetir(Sistem, hisseAdi);//303.25
            //Sistem.Debug(string.Format("AlisFiyat {0}", alisFiyati));

            double satisFiyati = IdealManager.SatisFiyatiGetir(Sistem, hisseAdi);//303.00
            //Sistem.Debug(string.Format("SatisFiyat {0}", satisFiyati));

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

                    var endeksBoleni = RiskYoneticisi.EndeksDegerlendir(Sistem, hisse);
                    var alimTutari = IdealManager.DivideAndRoundToInt(hisse.AlimTutari, endeksBoleni); 
                    lot = IdealManager.DivideAndRoundToInt(alimTutari, alisFiyati);

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

    public void TestStratejiBaslat(dynamic Sistem, string hisseAdi)
    {
        TestStrateji.Baslat(Sistem, hisseAdi);
    }
}



public class ManuelAnalizStrateji
{
    public static void Baslat(dynamic Sistem)
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

        var hisseEmirList = DatabaseManager.HisseEmirGetir(HisseEmirDurum.Satilabilir);
        foreach (var hisseEmir in hisseEmirList)
        {
            double satisFiyati = IdealManager.SatisFiyatiGetir(Sistem, hisseEmir.HisseAdi);
            if (satisFiyati == 0 || satisFiyati == 0)
            {
                //devre kesmis
                continue;
            }
            if (hisseEmir.SatisHedefi <= satisFiyati)
            {
                IdealManager.Sat(Sistem, hisseEmir.HisseAdi, hisseEmir.Lot, satisFiyati);
                hisseEmir.SatisTarihi = System.DateTime.Now;
                hisseEmir.Durum = HisseEmirDurum.KarAlindi.ToString();
                hisseEmir.Kar = System.Math.Round(satisFiyati - hisseEmir.AlisHedefi, 2);
                DatabaseManager.HisseEmirGuncelle(hisseEmir);
            }

            if (hisseEmir.StopHedefi >= satisFiyati)
            {
                IdealManager.Sat(Sistem, hisseEmir.HisseAdi, hisseEmir.Lot, satisFiyati);
                hisseEmir.SatisTarihi = System.DateTime.Now;
                hisseEmir.Durum = HisseEmirDurum.StopOldu.ToString();
                hisseEmir.Kar = System.Math.Round(satisFiyati - hisseEmir.AlisHedefi, 2);
                DatabaseManager.HisseEmirGuncelle(hisseEmir);
            }
        }

        hisseEmirList = DatabaseManager.HisseEmirGetir(HisseEmirDurum.Alinabilir);
        foreach (var hisseEmir in hisseEmirList)
        {
            double alisFiyati = IdealManager.AlisFiyatiGetir(Sistem, hisseEmir.HisseAdi);
            if (alisFiyati == 0 || alisFiyati == 0)
            {
                //devre kesmis
                continue;
            }
            if (hisseEmir.AlisHedefi >= alisFiyati)
            {
                IdealManager.Al(Sistem, hisseEmir.HisseAdi, hisseEmir.Lot, alisFiyati);
                hisseEmir.AlisTarihi = System.DateTime.Now;
                hisseEmir.Durum = HisseEmirDurum.Satilabilir.ToString();
                DatabaseManager.HisseEmirGuncelle(hisseEmir);
            }
        }
    }
}



public class RiskYoneticisi
{
    static System.Collections.Generic.List<int> customFibonacciList = new System.Collections.Generic.List<int> { 1, 2, 3, 5, 8, 13, 21, 34, 55, 89, 144 };
    public static int RiskHesapla(Hisse hisse)
    {
        var hissePozisyonlari = DatabaseManager.AcikHissePozisyonlariGetir(hisse.HisseAdi);
        var rtn = 1;
        if (hissePozisyonlari == null)
            return rtn;

        var percentage = IdealManager.CalculatePercentage(hissePozisyonlari.AcikPozisyonAlimTutari, hisse.Butce);

        if (percentage < 30)
        {
            rtn = customFibonacciList[0];
        }
        else if (percentage > 30 && percentage < 50)
        {
            rtn = customFibonacciList[1];
        }
        else if (percentage > 50 && percentage < 70)
        {
            rtn = customFibonacciList[2];
        }
        else if (percentage > 70 && percentage < 90)
        {
            rtn = customFibonacciList[3];
        }
        else if (percentage > 90 && percentage < 100)
        {
            rtn = customFibonacciList[4];
        }

        return rtn;

    }

    public static int RiskHesapla(dynamic Sistem, Hisse hisse, double alistutari, double marj)
    {
      
        double yuksekGun = IdealManager.YuksekGunGetir(Sistem, hisse.HisseAdi);
        if (alistutari + marj >= yuksekGun)
        {
            DatabaseManager.RiskDetayEkle(hisse.HisseAdi, string.Format("YuksekGunGetir izin vermedi alistutari {0} marj {1}", alistutari, marj));
            return 0;
        }
       
        return RiskHesapla(hisse);
    }

    public static int EndeksDegerlendir(dynamic Sistem, Hisse hisse)
    {
        double bist100Yuzde = IdealManager.Bist100EndeksYuzde(Sistem);
        double bist30Yuzde = IdealManager.Bist30EndeksYuzde(Sistem);
        double viop30Yuzde = IdealManager.Viop30EndeksYuzde(Sistem);
        double hisseYuzde = IdealManager.HisseYuzde(Sistem, hisse.HisseAdi);
        int rtn = 0;

        if (bist100Yuzde > 0 && bist30Yuzde > 0 && viop30Yuzde > 0 && hisseYuzde > 0)
        {
            
            rtn = customFibonacciList[0];
            return rtn;
        }
            
        if (bist100Yuzde<0 && bist30Yuzde < 0 && viop30Yuzde < 0)
            rtn = 1;

        if (bist100Yuzde < -1 && bist30Yuzde < -1 && viop30Yuzde < -1)
            rtn = 2;

        if (bist100Yuzde < -2 && bist30Yuzde < -2 && viop30Yuzde < -2)
            rtn = 3;

        if (bist100Yuzde < -3 && bist30Yuzde < -3 && viop30Yuzde < -3)
            rtn = 4;

        if (bist100Yuzde < -5 && bist30Yuzde < -5 && viop30Yuzde < -5)
            rtn = 5;

        if (hisseYuzde > 0)
            rtn = rtn - 1;

        DatabaseManager.RiskDetayEkle(hisse.HisseAdi, string.Format("EndeksDegerlendir bist100Yuzde={0} bist30Yuzde={1} viop30Yuzde={2} hisseYuzde={3} fibo={4}",
            System.Math.Round(bist100Yuzde, 2),
            System.Math.Round(bist30Yuzde, 2),
            System.Math.Round(viop30Yuzde, 2),
            System.Math.Round(hisseYuzde, 2),
            customFibonacciList[rtn]
            ));

        return customFibonacciList[rtn];
    }



}

public class TestStrateji
{
    public static void Baslat(dynamic Sistem, string hisseAdi)
    {
        Sistem.Debug("adadasdsada");

        var hisse = DatabaseManager.HisseGetir(hisseAdi);
        double yuksekGun = IdealManager.YuksekGunGetir(Sistem, "AKBNK");
        Sistem.Debug(yuksekGun.ToString());
        RiskYoneticisi.RiskHesapla(Sistem, hisse,40.90D,0.16D);
        RiskYoneticisi.EndeksDegerlendir(Sistem, hisse);

        Sistem.Debug("adadasdsada");
    }
}

}