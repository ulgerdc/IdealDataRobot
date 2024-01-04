using AlimSatimRobotu.Entity;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AlimSatimRobotu
{
    internal class DatabaseManager
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

                cmd.CommandText = string.Format("select * from Hisse where HisseAdi='{0}'", hisseAdi);
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
    
                cmd.CommandText = $"sel_HisseHareket";
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

        public static bool HisseAlimKontrol(string hisseAdi, double alisFiyati, double marj)
        {
            var conn = OpenConnection();

            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Connection = conn;
            cmd.CommandText = $"[dbo].[sel_hisseAlimKontrol]";
            cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);
            cmd.Parameters.AddWithValue("@AlisFiyati", alisFiyati);
            cmd.Parameters.AddWithValue("@Marj", marj);

            cmd.ExecuteNonQuery();
            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            bool al=true;
     
            if (dt.Rows.Count > 0)
            {
                al = false;
            }
            cmd.Connection.Close();

            return al;

        }

        public static Tuple<int,List<HisseHareket>> HisseSatimKontrol(string hisseAdi, double satisFiyati, double marj)
        {

            var conn = OpenConnection();

            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Connection = conn;
            cmd.CommandText = "[dbo].[sel_hisseSatimKontrol]";
            cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);
            cmd.Parameters.AddWithValue("@SatisFiyati", satisFiyati);
            cmd.Parameters.AddWithValue("@Marj", marj);

            cmd.ExecuteNonQuery();
            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dt);
            HisseHareket hisse = null;
            int satilacakLot = 0;
            List<HisseHareket> hisseHareketleri = new List<HisseHareket>();
            if (dt.Rows.Count > 0)
            {
                foreach (DataRow row in dt.Rows)
                {
                    hisse = MapDataRowToObject<HisseHareket>(row);
                    hisseHareketleri.Add(hisse);
                    satilacakLot += hisse.Lot;
                }
               
            }
            cmd.Connection.Close();
           
            return new Tuple<int, List<HisseHareket>>(satilacakLot, hisseHareketleri);

        }

        internal static HissePozisyonlari AcikHissePozisyonlariGetir(string hisseAdi)
        {
            var conn = OpenConnection();

            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Connection = conn;
            cmd.CommandText = $"[dbo].[sel_hissehareket]";
            cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);

            cmd.ExecuteNonQuery();
            DataTable dt = new DataTable();
            SqlDataAdapter da = new SqlDataAdapter(cmd);
            da.Fill(dt);

            HissePozisyonlari hissePozisyonlari = null;

            if (dt.Rows.Count > 0)
            {
                var row = dt.Rows[0];
                hissePozisyonlari = MapDataRowToObject<HissePozisyonlari>(row);
            }
            cmd.Connection.Close();

            return hissePozisyonlari;
        }

        public static void HisseEkleGuncelle(Hisse hisse)
        {


            var conn = OpenConnection();

            SqlCommand cmd = conn.CreateCommand();
            cmd.CommandType = CommandType.StoredProcedure;
            cmd.Connection = conn;
            cmd.CommandText = "ins_Hisse";
     
            cmd.Parameters.AddWithValue("@HisseAdi", hisse.HisseAdi);
            cmd.Parameters.AddWithValue("@IlkFiyat", hisse.IlkFiyat);
            cmd.Parameters.AddWithValue("@Butce", hisse.Butce);
            cmd.Parameters.AddWithValue("@BaslangicKademe", hisse.BaslangicKademe);
            cmd.Parameters.AddWithValue("@AlimTutari", hisse.AlimTutari);
            cmd.Parameters.AddWithValue("@SonAlimTutari", hisse.SonAlimTutari);
            cmd.Parameters.AddWithValue("@PortfoyTarihi", hisse.PortfoyTarihi);
            cmd.Parameters.AddWithValue("@SonIslemTarihi", hisse.SonIslemTarihi);
            cmd.Parameters.AddWithValue("@Aktif", hisse.Aktif);

            cmd.ExecuteNonQuery();
            cmd.Connection.Close();

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
  
            cmd.ExecuteNonQuery();
            cmd.Connection.Close();

        }



        public static T MapDataRowToObject<T>(DataRow row) where T : new()
        {
            T obj = new T();

            foreach (var property in typeof(T).GetProperties())
            {
                if (row.Table.Columns.Contains(property.Name) && row[property.Name] != DBNull.Value)
                {
                    if (property.PropertyType.IsEnum)
                    {
                        property.SetValue(obj, Enum.Parse(property.PropertyType, Convert.ToString(row[property.Name])));
                    }
                    else
                        property.SetValue(obj, Convert.ChangeType(row[property.Name], property.PropertyType));
                }
            }

            return obj;
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
}
