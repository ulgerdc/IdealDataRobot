
public class YutanMumHareket
{
    public long Id { get; set; }
    public long BatchId { get; set; }
    public string HisseAdi { get; set; }
    public int Lot { get; set; }
    public double AlisFiyati { get; set; }
    public double SatisFiyati { get; set; }
    public double Kar { get; set; }
    public System.DateTime AlisTarihi { get; set; }
    public System.DateTime SatisTarihi { get; set; }
    public bool AktifMi { get; set; }
    public double DunkuAcilis { get; set; }
    public double DunkuKapanis { get; set; }
    public double BugunAcilis { get; set; }
    public double BugunKapanis { get; set; }
    public long DunkuHacim { get; set; }
    public long BugunHacim { get; set; }
    public double BugunYuksek { get; set; }
    public double BugunDusuk { get; set; }
    public double MomentumYuzde { get; set; }
}
