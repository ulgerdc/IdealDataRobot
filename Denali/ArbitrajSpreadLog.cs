
public class ArbitrajSpreadLog
{
    public long Id { get; set; }
    public long ArbitrajGelismisId { get; set; }
    public string HisseAdi { get; set; }
    public int ArbitrajTipi { get; set; }
    public double Bacak1Fiyat { get; set; }
    public double Bacak2Fiyat { get; set; }
    public double SpreadYuzde { get; set; }
    public double SpreadTutar { get; set; }
    public bool GirisSinyali { get; set; }
    public bool CikisSinyali { get; set; }
    public string AtlanmaAciklamasi { get; set; }
    public double Bist100Yuzde { get; set; }
    public double Bist30Yuzde { get; set; }
    public double Viop30Yuzde { get; set; }
    public System.DateTime Tarih { get; set; }
    public double AdilSpreadYuzde { get; set; }
    public double NetPrimYuzde { get; set; }
    public int KalanGun { get; set; }
}
