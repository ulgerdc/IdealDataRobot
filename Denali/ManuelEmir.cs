public class ManuelEmir
{
    public long Id { get; set; }
    public string HisseAdi { get; set; }
    public int Lot { get; set; }
    public double AlisFiyati { get; set; }
    public int Durum { get; set; }          // 0=Bekliyor, 1=Gerceklesti, 2=Iptal
    public System.DateTime OlusturmaTarihi { get; set; }
    public System.DateTime? GerceklesmeTarihi { get; set; }
    public double? GercekFiyat { get; set; }
    public string Aciklama { get; set; }
}
