
public class ArbitrajGelismisHareket
{
    public long Id { get; set; }
    public long ArbitrajGelismisId { get; set; }
    public string RobotAdi { get; set; }
    public string HisseAdi { get; set; }
    public int ArbitrajTipi { get; set; }
    public string Bacak1Sembol { get; set; }
    public string Bacak1Yon { get; set; }
    public double Bacak1GirisFiyat { get; set; }
    public double Bacak1CikisFiyat { get; set; }
    public int Bacak1Lot { get; set; }
    public string Bacak2Sembol { get; set; }
    public string Bacak2Yon { get; set; }
    public double Bacak2GirisFiyat { get; set; }
    public double Bacak2CikisFiyat { get; set; }
    public int Bacak2Lot { get; set; }
    public double GirisSpreadYuzde { get; set; }
    public double CikisSpreadYuzde { get; set; }
    public double Kar { get; set; }
    public bool AktifMi { get; set; }
    public System.DateTime PozisyonTarihi { get; set; }
    public System.DateTime KapanisTarihi { get; set; }
}
