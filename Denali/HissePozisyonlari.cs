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