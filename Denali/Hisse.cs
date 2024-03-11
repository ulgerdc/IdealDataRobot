
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

    public bool AlisAktif { get; set; }
    public bool SatisAktif { get; set; }

    public double PiyasaAlis { get; set; }

    public double PiyasaSatis { get; set; }

}