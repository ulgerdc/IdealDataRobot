
public class ArbitrajHareket
{
    public long Id { get; set; }

    public string RobotAdi { get; internal set; }
    public string HisseAdi { get; internal set; }
    public double ViopSatisFiyati { get; internal set; }
    public double ViopAlisFiyati { get; internal set; }
    public int ViopLot { get; set; }
    public double BistAlisFiyati { get; internal set; }
    public double BistSatisFiyati { get; internal set; }
    public int BistLot { get; set; }
    public double Kar { get; internal set; }
    public System.DateTime PozisyonTarih { get; internal set; }
    public System.DateTime KapanisTarihi { get; internal set; }
    public bool AktifMi { get; internal set; }

}