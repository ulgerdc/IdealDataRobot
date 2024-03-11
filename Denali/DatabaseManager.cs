﻿

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

    public static void HisseGuncelle(Hisse hisse)
    {
        var conn = OpenConnection();

        System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Connection = conn;
        cmd.CommandText = "upd_hisse";

        cmd.Parameters.AddWithValue("@Id", hisse.Id);
        cmd.Parameters.AddWithValue("@PiyasaAlis", hisse.PiyasaAlis);
        cmd.Parameters.AddWithValue("@PiyasaSatis", hisse.PiyasaSatis);
        cmd.ExecuteNonQuery();
        cmd.Connection.Close();

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

    public static bool SabahCoskusuKontrol(string hisseAdi)
    {
        bool al = true;

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "[dbo].[sel_sabahcoskusukontrol]";
            cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);
 
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

    public static void SabahCoskusuHareketGuncelle(SabahCoskusuHareket hisseHareket)
    {
        var conn = OpenConnection();

        System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Connection = conn;
        cmd.CommandText = "[dbo].[ins_sabahcoskusuhareket]";

        cmd.Parameters.AddWithValue("@Id", hisseHareket.Id);
        cmd.Parameters.AddWithValue("@HisseAdi", hisseHareket.HisseAdi);
        cmd.Parameters.AddWithValue("@Lot", hisseHareket.Lot);
        cmd.Parameters.AddWithValue("@AlisFiyati", hisseHareket.AlisFiyati);
        cmd.Parameters.AddWithValue("@RobotAdi", hisseHareket.RobotAdi);
        cmd.ExecuteNonQuery();
        cmd.Connection.Close();

    }

    public static System.Collections.Generic.List<Hisse> SabahCoskusuGetir()
    {

        var hisseEmirList = new System.Collections.Generic.List<Hisse>();

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.CommandText = "[dbo].[sel_sabahcoskusu]"; 
           
            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Hisse hisseEmir = MapFromDataReader<Hisse>(reader);
                    hisseEmirList.Add(hisseEmir);
                }
            }
        }

        return hisseEmirList;

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

    public static System.Collections.Generic.List<Arbitraj> ArbitrajGetir()
    {

        var list = new System.Collections.Generic.List<Arbitraj>();

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.CommandText = "[dbo].[sel_arbitraj]";

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    Arbitraj map = MapFromDataReader<Arbitraj>(reader);
                    list.Add(map);
                }
            }
        }

        return list;

    }

    public static ArbitrajHareket ArbitrajKontrol(string hisseAdi)
    {
        ArbitrajHareket arbitrajHareket = null;

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "[dbo].[sel_arbitrajkontrol]";
            cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    arbitrajHareket = MapFromDataReader<ArbitrajHareket>(reader);
                }
            }
        }

        return arbitrajHareket;

    }

    public static void ArbitrajHareketGuncelle(ArbitrajHareket hareket)
    {
        var conn = OpenConnection();

        System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Connection = conn;
        cmd.CommandText = "[dbo].[ins_arbitrajhareket]";

        cmd.Parameters.AddWithValue("@Id", hareket.Id);
        cmd.Parameters.AddWithValue("@RobotAdi", hareket.RobotAdi);
        cmd.Parameters.AddWithValue("@HisseAdi", hareket.HisseAdi);
        cmd.Parameters.AddWithValue("@ViopSatisFiyati", hareket.ViopSatisFiyati);
        cmd.Parameters.AddWithValue("@ViopAlisFiyati", hareket.ViopAlisFiyati);
        cmd.Parameters.AddWithValue("@ViopLot", hareket.ViopLot);
        cmd.Parameters.AddWithValue("@BistSatisFiyati", hareket.BistSatisFiyati);
        cmd.Parameters.AddWithValue("@BistAlisFiyati", hareket.BistAlisFiyati);
        cmd.Parameters.AddWithValue("@BistLot", hareket.BistLot);

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