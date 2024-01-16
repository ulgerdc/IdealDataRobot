
public class HisseEmir
{
    public long Id { get; set; }

    public string RobotAdi { get; internal set; }
    public string HisseAdi { get; internal set; }
    public int Lot { get; set; }
    public double AlisHedefi { get; internal set; }
    public double SatisHedefi { get; internal set; }
    public double StopHedefi { get; internal set; }

    public double Kar { get; internal set; }

    public System.DateTime AlisTarihi { get; internal set; }

    public System.DateTime SatisTarihi { get; internal set; }

    public string Durum { get; internal set; }//Alis, Satis, Stop, Kar

}


