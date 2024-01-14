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