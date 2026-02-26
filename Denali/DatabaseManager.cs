

public class DatabaseManager
{
    static string connectionString = "Data Source=.;Initial Catalog=robot;User ID=sa;Password=1;Connection Timeout=30;Min Pool Size=5;Max Pool Size=15;Pooling=true;TrustServerCertificate=True;";
    public static System.Data.SqlClient.SqlConnection OpenConnection()
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

    public static System.Tuple<int, System.Collections.Generic.List<HisseHareket>> HisseSatimKontrol(string hisseAdi, double satisFiyati, double marj, int pozisyonTipi = 0)
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
            cmd.Parameters.AddWithValue("@PozisyonTipi", pozisyonTipi);

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
        cmd.Parameters.AddWithValue("@PozisyonTipi", hisseHareket.PozisyonTipi);
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

    public static System.Tuple<int, System.Collections.Generic.List<HisseHareket>> HisseSatimKontrolZamanli(string hisseAdi, double satisFiyati, double marj)
    {
        HisseHareket hisse = null;
        int satilacakLot = 0;
        var hisseHareketleri = new System.Collections.Generic.List<HisseHareket>();

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.CommandText = "[dbo].[sel_hisseSatimKontrolZamanli]";
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

    public static System.Tuple<int, System.Collections.Generic.List<HisseHareket>> CoreSatimKontrol(string hisseAdi, double satisFiyati, double coreMarj, double trailingStopYuzde)
    {
        HisseHareket hisse = null;
        int satilacakLot = 0;
        var hisseHareketleri = new System.Collections.Generic.List<HisseHareket>();

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;

            cmd.CommandText = "[dbo].[sel_hisseSatimKontrolCore]";
            cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);
            cmd.Parameters.AddWithValue("@SatisFiyati", System.Math.Round(satisFiyati, 2));
            cmd.Parameters.AddWithValue("@CoreMarj", coreMarj);
            cmd.Parameters.AddWithValue("@TrailingStopYuzde", trailingStopYuzde);

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

    public static void CoreTepeNoktasiGuncelle(string hisseAdi, double guncelFiyat)
    {
        var conn = OpenConnection();

        System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Connection = conn;
        cmd.CommandText = "[dbo].[upd_hisseHareketTepeNoktasi]";
        cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);
        cmd.Parameters.AddWithValue("@GuncelFiyat", System.Math.Round(guncelFiyat, 2));
        cmd.ExecuteNonQuery();
        cmd.Connection.Close();
    }

    public static void HisseHareketLotGuncelle(long id, int yeniLot)
    {
        var conn = OpenConnection();

        System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.Text;
        cmd.Connection = conn;
        cmd.CommandText = "UPDATE [dbo].[HisseHareket] SET Lot = @Lot WHERE Id = @Id AND AktifMi = 1";
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@Lot", yeniLot);
        cmd.ExecuteNonQuery();
        cmd.Connection.Close();
    }

    public static void GunlukVeriKaydet(string hisseAdi, double yuksek, double dusuk, double kapanis)
    {
        var conn = OpenConnection();

        System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Connection = conn;
        cmd.CommandText = "[dbo].[ins_hisseGunlukVeri]";
        cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);
        cmd.Parameters.AddWithValue("@Yuksek", System.Math.Round(yuksek, 2));
        cmd.Parameters.AddWithValue("@Dusuk", System.Math.Round(dusuk, 2));
        cmd.Parameters.AddWithValue("@Kapanis", System.Math.Round(kapanis, 2));
        cmd.ExecuteNonQuery();
        cmd.Connection.Close();
    }

    public static void GunlukVeriKaydet(string hisseAdi, double acilis, double yuksek, double dusuk, double kapanis, long hacim)
    {
        var conn = OpenConnection();

        System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Connection = conn;
        cmd.CommandText = "[dbo].[ins_hisseGunlukVeri]";
        cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);
        cmd.Parameters.AddWithValue("@Yuksek", System.Math.Round(yuksek, 2));
        cmd.Parameters.AddWithValue("@Dusuk", System.Math.Round(dusuk, 2));
        cmd.Parameters.AddWithValue("@Kapanis", System.Math.Round(kapanis, 2));
        cmd.Parameters.AddWithValue("@Acilis", System.Math.Round(acilis, 2));
        cmd.Parameters.AddWithValue("@Hacim", hacim);
        cmd.ExecuteNonQuery();
        cmd.Connection.Close();
    }

    public static double MomentumHesapla(string hisseAdi, int gun)
    {
        double momentum = 0;

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandText = "SELECT TOP (@Gun) Kapanis FROM HisseGunlukVeri WHERE HisseAdi = @HisseAdi ORDER BY Tarih DESC";
            cmd.Parameters.AddWithValue("@Gun", gun);
            cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);

            var fiyatlar = new System.Collections.Generic.List<double>();
            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    fiyatlar.Add(System.Convert.ToDouble(reader["Kapanis"]));
                }
            }

            if (fiyatlar.Count >= 2)
            {
                double enYeni = fiyatlar[0];
                double enEski = fiyatlar[fiyatlar.Count - 1];
                if (enEski > 0)
                {
                    momentum = (enYeni - enEski) / enEski * 100;
                }
            }
        }

        return momentum;
    }

    public static double ATRHesapla(string hisseAdi, int periyot, string zamanDilimi)
    {
        double atr = 0;

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "[dbo].[sel_hisseATR]";
            cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);
            cmd.Parameters.AddWithValue("@Periyot", periyot);
            cmd.Parameters.AddWithValue("@ZamanDilimi", zamanDilimi ?? "D");

            object result = cmd.ExecuteScalar();
            if (result != null && result != System.DBNull.Value)
            {
                atr = System.Convert.ToDouble(result);
            }
        }

        return atr;
    }

    public static double ArbitrajGelismisGlobalButceGetir()
    {
        double butce = 250000;

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandText = "SELECT Butce FROM ArbitrajGelismis WHERE HisseAdi='_GLOBAL'";

            object result = cmd.ExecuteScalar();
            if (result != null && result != System.DBNull.Value)
            {
                butce = System.Convert.ToDouble(result);
            }
        }

        return butce;
    }

    public static double ArbitrajGelismisAcikTutar()
    {
        double tutar = 0;

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.Text;
            cmd.CommandText = "SELECT ISNULL(SUM(Bacak1GirisFiyat * Bacak1Lot + Bacak2GirisFiyat * Bacak2Lot), 0) FROM ArbitrajGelismisHareket WHERE AktifMi=1";

            object result = cmd.ExecuteScalar();
            if (result != null && result != System.DBNull.Value)
            {
                tutar = System.Convert.ToDouble(result);
            }
        }

        return tutar;
    }

    public static System.Collections.Generic.List<ArbitrajGelismisConfig> ArbitrajGelismisGetir()
    {
        var list = new System.Collections.Generic.List<ArbitrajGelismisConfig>();

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "[dbo].[sel_arbitrajGelismis]";

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    ArbitrajGelismisConfig config = MapFromDataReader<ArbitrajGelismisConfig>(reader);
                    list.Add(config);
                }
            }
        }

        return list;
    }

    public static void ArbitrajSpreadLogKaydet(long arbitrajGelismisId, string hisseAdi, int arbitrajTipi,
        double bacak1Fiyat, double bacak2Fiyat, double spreadYuzde, double spreadTutar,
        bool girisSinyali, bool cikisSinyali, string atlanmaAciklamasi,
        double bist100Yuzde, double bist30Yuzde, double viop30Yuzde,
        double adilSpreadYuzde, double netPrimYuzde, int kalanGun)
    {
        var conn = OpenConnection();

        System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Connection = conn;
        cmd.CommandText = "[dbo].[ins_arbitrajSpreadLog]";

        cmd.Parameters.AddWithValue("@ArbitrajGelismisId", arbitrajGelismisId);
        cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);
        cmd.Parameters.AddWithValue("@ArbitrajTipi", arbitrajTipi);
        cmd.Parameters.AddWithValue("@Bacak1Fiyat", System.Math.Round(bacak1Fiyat, 2));
        cmd.Parameters.AddWithValue("@Bacak2Fiyat", System.Math.Round(bacak2Fiyat, 2));
        cmd.Parameters.AddWithValue("@SpreadYuzde", System.Math.Round(spreadYuzde, 4));
        cmd.Parameters.AddWithValue("@SpreadTutar", System.Math.Round(spreadTutar, 2));
        cmd.Parameters.AddWithValue("@GirisSinyali", girisSinyali);
        cmd.Parameters.AddWithValue("@CikisSinyali", cikisSinyali);
        cmd.Parameters.AddWithValue("@AtlanmaAciklamasi", (object)atlanmaAciklamasi ?? System.DBNull.Value);
        cmd.Parameters.AddWithValue("@Bist100Yuzde", bist100Yuzde);
        cmd.Parameters.AddWithValue("@Bist30Yuzde", bist30Yuzde);
        cmd.Parameters.AddWithValue("@Viop30Yuzde", viop30Yuzde);
        cmd.Parameters.AddWithValue("@AdilSpreadYuzde", System.Math.Round(adilSpreadYuzde, 4));
        cmd.Parameters.AddWithValue("@NetPrimYuzde", System.Math.Round(netPrimYuzde, 4));
        cmd.Parameters.AddWithValue("@KalanGun", kalanGun);

        cmd.ExecuteNonQuery();
        cmd.Connection.Close();
    }

    public static System.Tuple<double, System.DateTime> ArbitrajSonSpreadGetir(long arbitrajGelismisId)
    {
        double sonSpread = 0;
        System.DateTime sonTarih = System.DateTime.MinValue;

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "[dbo].[sel_arbitrajSonSpread]";
            cmd.Parameters.AddWithValue("@ArbitrajGelismisId", arbitrajGelismisId);

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    sonSpread = System.Convert.ToDouble(reader["SpreadYuzde"]);
                    sonTarih = System.Convert.ToDateTime(reader["Tarih"]);
                }
            }
        }

        return new System.Tuple<double, System.DateTime>(sonSpread, sonTarih);
    }

    public static ArbitrajGelismisHareket ArbitrajGelismisKontrol(string hisseAdi, int arbitrajTipi)
    {
        ArbitrajGelismisHareket hareket = null;

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "[dbo].[sel_arbitrajGelismisKontrol]";
            cmd.Parameters.AddWithValue("@HisseAdi", hisseAdi);
            cmd.Parameters.AddWithValue("@ArbitrajTipi", arbitrajTipi);

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    hareket = MapFromDataReader<ArbitrajGelismisHareket>(reader);
                }
            }
        }

        return hareket;
    }

    public static void ArbitrajGelismisHareketGuncelle(ArbitrajGelismisHareket hareket)
    {
        var conn = OpenConnection();

        System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Connection = conn;
        cmd.CommandText = "[dbo].[ins_arbitrajGelismisHareket]";

        cmd.Parameters.AddWithValue("@Id", hareket.Id);
        cmd.Parameters.AddWithValue("@ArbitrajGelismisId", hareket.ArbitrajGelismisId);
        cmd.Parameters.AddWithValue("@RobotAdi", hareket.RobotAdi);
        cmd.Parameters.AddWithValue("@HisseAdi", hareket.HisseAdi);
        cmd.Parameters.AddWithValue("@ArbitrajTipi", hareket.ArbitrajTipi);
        cmd.Parameters.AddWithValue("@Bacak1Sembol", hareket.Bacak1Sembol);
        cmd.Parameters.AddWithValue("@Bacak1Yon", hareket.Bacak1Yon);
        cmd.Parameters.AddWithValue("@Bacak1GirisFiyat", System.Math.Round(hareket.Bacak1GirisFiyat, 2));
        cmd.Parameters.AddWithValue("@Bacak1CikisFiyat", hareket.Bacak1CikisFiyat > 0 ? (object)System.Math.Round(hareket.Bacak1CikisFiyat, 2) : System.DBNull.Value);
        cmd.Parameters.AddWithValue("@Bacak1Lot", hareket.Bacak1Lot);
        cmd.Parameters.AddWithValue("@Bacak2Sembol", hareket.Bacak2Sembol);
        cmd.Parameters.AddWithValue("@Bacak2Yon", hareket.Bacak2Yon);
        cmd.Parameters.AddWithValue("@Bacak2GirisFiyat", System.Math.Round(hareket.Bacak2GirisFiyat, 2));
        cmd.Parameters.AddWithValue("@Bacak2CikisFiyat", hareket.Bacak2CikisFiyat > 0 ? (object)System.Math.Round(hareket.Bacak2CikisFiyat, 2) : System.DBNull.Value);
        cmd.Parameters.AddWithValue("@Bacak2Lot", hareket.Bacak2Lot);
        cmd.Parameters.AddWithValue("@GirisSpreadYuzde", System.Math.Round(hareket.GirisSpreadYuzde, 4));
        cmd.Parameters.AddWithValue("@CikisSpreadYuzde", hareket.CikisSpreadYuzde != 0 ? (object)System.Math.Round(hareket.CikisSpreadYuzde, 4) : System.DBNull.Value);

        cmd.ExecuteNonQuery();
        cmd.Connection.Close();
    }

    // =============================================
    // Yutan Mum Stratejisi Metodlari
    // =============================================

    public static YutanMumConfig YutanMumConfigGetir()
    {
        YutanMumConfig config = null;

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "[dbo].[sel_yutanMumConfig]";

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
            {
                if (reader.Read())
                {
                    config = MapFromDataReader<YutanMumConfig>(reader);
                }
            }
        }

        return config;
    }

    public static System.Collections.Generic.List<string> Bist100HisselerGetir()
    {
        var list = new System.Collections.Generic.List<string>();

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "[dbo].[sel_bist100Hisseler]";

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    list.Add(reader["HisseAdi"].ToString());
                }
            }
        }

        return list;
    }

    public static long YutanMumBatchOlustur(string robotAdi, int hisseSayisi, double toplamAlimTutari)
    {
        long batchId = 0;

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "[dbo].[ins_yutanMumBatch]";
            cmd.Parameters.AddWithValue("@RobotAdi", robotAdi);
            cmd.Parameters.AddWithValue("@HisseSayisi", hisseSayisi);
            cmd.Parameters.AddWithValue("@ToplamAlimTutari", toplamAlimTutari);

            object result = cmd.ExecuteScalar();
            if (result != null && result != System.DBNull.Value)
            {
                batchId = System.Convert.ToInt64(result);
            }
        }

        return batchId;
    }

    public static void YutanMumHareketEkle(YutanMumHareket hareket)
    {
        var conn = OpenConnection();

        System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Connection = conn;
        cmd.CommandText = "[dbo].[ins_yutanMumHareket]";

        cmd.Parameters.AddWithValue("@BatchId", hareket.BatchId);
        cmd.Parameters.AddWithValue("@HisseAdi", hareket.HisseAdi);
        cmd.Parameters.AddWithValue("@Lot", hareket.Lot);
        cmd.Parameters.AddWithValue("@AlisFiyati", System.Math.Round(hareket.AlisFiyati, 2));
        cmd.Parameters.AddWithValue("@DunkuAcilis", hareket.DunkuAcilis > 0 ? (object)hareket.DunkuAcilis : System.DBNull.Value);
        cmd.Parameters.AddWithValue("@DunkuKapanis", hareket.DunkuKapanis > 0 ? (object)hareket.DunkuKapanis : System.DBNull.Value);
        cmd.Parameters.AddWithValue("@BugunAcilis", hareket.BugunAcilis > 0 ? (object)hareket.BugunAcilis : System.DBNull.Value);
        cmd.Parameters.AddWithValue("@BugunKapanis", hareket.BugunKapanis > 0 ? (object)hareket.BugunKapanis : System.DBNull.Value);
        cmd.Parameters.AddWithValue("@DunkuHacim", hareket.DunkuHacim > 0 ? (object)hareket.DunkuHacim : System.DBNull.Value);
        cmd.Parameters.AddWithValue("@BugunHacim", hareket.BugunHacim > 0 ? (object)hareket.BugunHacim : System.DBNull.Value);
        cmd.Parameters.AddWithValue("@BugunYuksek", hareket.BugunYuksek > 0 ? (object)hareket.BugunYuksek : System.DBNull.Value);
        cmd.Parameters.AddWithValue("@BugunDusuk", hareket.BugunDusuk > 0 ? (object)hareket.BugunDusuk : System.DBNull.Value);
        cmd.Parameters.AddWithValue("@MomentumYuzde", hareket.MomentumYuzde != 0 ? (object)hareket.MomentumYuzde : System.DBNull.Value);

        cmd.ExecuteNonQuery();
        cmd.Connection.Close();
    }

    public static System.Collections.Generic.List<YutanMumBatch> YutanMumAktifBatchlerGetir()
    {
        var list = new System.Collections.Generic.List<YutanMumBatch>();

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "[dbo].[sel_yutanMumAktifBatchler]";

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    YutanMumBatch batch = MapFromDataReader<YutanMumBatch>(reader);
                    list.Add(batch);
                }
            }
        }

        return list;
    }

    public static System.Collections.Generic.List<YutanMumHareket> YutanMumBatchHareketlerGetir(long batchId)
    {
        var list = new System.Collections.Generic.List<YutanMumHareket>();

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "[dbo].[sel_yutanMumBatchHareketler]";
            cmd.Parameters.AddWithValue("@BatchId", batchId);

            using (System.Data.SqlClient.SqlDataReader reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    YutanMumHareket hareket = MapFromDataReader<YutanMumHareket>(reader);
                    list.Add(hareket);
                }
            }
        }

        return list;
    }

    public static void YutanMumHareketSat(long id, double satisFiyati)
    {
        var conn = OpenConnection();

        System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Connection = conn;
        cmd.CommandText = "[dbo].[upd_yutanMumHareketSat]";
        cmd.Parameters.AddWithValue("@Id", id);
        cmd.Parameters.AddWithValue("@SatisFiyati", System.Math.Round(satisFiyati, 2));

        cmd.ExecuteNonQuery();
        cmd.Connection.Close();
    }

    public static void YutanMumBatchKapat(long batchId, double toplamKar, string neden)
    {
        var conn = OpenConnection();

        System.Data.SqlClient.SqlCommand cmd = conn.CreateCommand();
        cmd.CommandType = System.Data.CommandType.StoredProcedure;
        cmd.Connection = conn;
        cmd.CommandText = "[dbo].[upd_yutanMumBatchKapat]";
        cmd.Parameters.AddWithValue("@BatchId", batchId);
        cmd.Parameters.AddWithValue("@ToplamKar", System.Math.Round(toplamKar, 2));
        cmd.Parameters.AddWithValue("@KapanisNedeni", neden);

        cmd.ExecuteNonQuery();
        cmd.Connection.Close();
    }

    public static bool YutanMumBugunBatchVarMi()
    {
        bool var = false;

        using (System.Data.SqlClient.SqlConnection connection = new System.Data.SqlClient.SqlConnection(connectionString))
        {
            connection.Open();

            System.Data.SqlClient.SqlCommand cmd = connection.CreateCommand();
            cmd.CommandType = System.Data.CommandType.StoredProcedure;
            cmd.CommandText = "[dbo].[sel_yutanMumBugunBatchVar]";

            object result = cmd.ExecuteScalar();
            if (result != null && result != System.DBNull.Value)
            {
                var = true;
            }
        }

        return var;
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