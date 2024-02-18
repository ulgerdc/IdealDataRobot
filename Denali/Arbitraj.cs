
public class Arbitraj
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

    public int ViopLot { get; set; }

    public int BistLot { get; set; }

    public int Marj { get; set; }

    public bool AktifMi { get; set; }

}