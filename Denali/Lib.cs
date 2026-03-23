public class Lib
{
    public void Baslat(dynamic Sistem,string hisseAdi)
    {
        KademeStrateji.Baslat(Sistem, hisseAdi);
    }
    public void ManuelAnalizBaslat(dynamic Sistem)
    {
        ManuelAnalizStrateji.Baslat(Sistem);
    }
    
    public void KademeGelismisBaslat(dynamic Sistem,string hisseAdi)
    {
        KademeStratejiGelismis.Baslat(Sistem, hisseAdi);
    }

    public void SabahCoskusuBaslat(dynamic Sistem)
    {
        SabahCoskusuStrateji.Baslat(Sistem);
    }

    public void TestStratejiBaslat(dynamic Sistem, string hisseAdi)
    {
        TestStrateji.Baslat(Sistem, hisseAdi);
    }

    public void ArbitrajStratejiBaslat(dynamic Sistem)
    {
        ArbitrajStrateji.Baslat(Sistem);
    }

    public void ArbitrajGelismisBaslat(dynamic Sistem)
    {
        ArbitrajStratejiGelismis.Baslat(Sistem);
    }
    public void YutanMumBaslat(dynamic Sistem)
    {
        YutanMumStrateji.Baslat(Sistem);
    }

    public void OvernightBaslat(dynamic Sistem)
    {
        OvernightStrateji.Baslat(Sistem);
    }

    public void OvernightTestBaslat(dynamic Sistem)
    {
        OvernightStrateji.Test(Sistem);
    }

    public void OvernightAnalizBaslat(dynamic Sistem)
    {
        OvernightAnaliz.Baslat(Sistem);
    }

    public void PortfoySenkronBaslat(dynamic Sistem)
    {
        PortfoySenkron.Baslat(Sistem);
    }

    public void GucluKapanisBacktestBaslat(dynamic Sistem)
    {
        GucluKapanisBacktest.Baslat(Sistem);
    }

    public void MomentumBacktestBaslat(dynamic Sistem)
    {
        MomentumBacktest.Baslat(Sistem);
    }

    public void YutanMumBacktestBaslat(dynamic Sistem)
    {
        YutanMumBacktest.Baslat(Sistem);
    }

    public void DiptenTepkiBacktestBaslat(dynamic Sistem)
    {
        DiptenTepkiBacktest.Baslat(Sistem);
    }

    public void T2LikiditBacktestBaslat(dynamic Sistem)
    {
        T2LikiditBacktest.Baslat(Sistem);
    }

    public void FakeBreakdownBacktestBaslat(dynamic Sistem)
    {
        FakeBreakdownBacktest.Baslat(Sistem);
    }

    public void DiptenTepkiGunIciBacktestBaslat(dynamic Sistem)
    {
        DiptenTepkiGunIciBacktest.Baslat(Sistem);
    }

    public void FakeBreakdownGunIciBacktestBaslat(dynamic Sistem)
    {
        FakeBreakdownGunIciBacktest.Baslat(Sistem);
    }

    public void DiptenTepkiTPSLBacktestBaslat(dynamic Sistem)
    {
        DiptenTepkiTPSLBacktest.Baslat(Sistem);
    }

    public void FakeBreakdownTPSLBacktestBaslat(dynamic Sistem)
    {
        FakeBreakdownTPSLBacktest.Baslat(Sistem);
    }

    public void GucluKapanisGapBacktestBaslat(dynamic Sistem)
    {
        GucluKapanisGapBacktest.Baslat(Sistem);
    }

    public void T2LikiditGapBacktestBaslat(dynamic Sistem)
    {
        T2LikiditGapBacktest.Baslat(Sistem);
    }

    public void GucluKapanisV2BacktestBaslat(dynamic Sistem)
    {
        GucluKapanisV2Backtest.Baslat(Sistem);
    }

    public void T2LikiditV2BacktestBaslat(dynamic Sistem)
    {
        T2LikiditV2Backtest.Baslat(Sistem);
    }

    public void HaftalikSimulasyonBaslat(dynamic Sistem)
    {
        HaftalikSimulasyon.Baslat(Sistem);
    }

    public void OvernightSemihBaslat(dynamic Sistem)
    {
        OvernightSemihStrateji.Baslat(Sistem);
    }

    public void OvernightSemihTestBaslat(dynamic Sistem)
    {
        OvernightSemihStrateji.Test(Sistem);
    }

    public void OvernightSemihBacktestBaslat(dynamic Sistem)
    {
        OvernightSemihBacktest.Baslat(Sistem);
    }

    public void SabahMomentumBacktestBaslat(dynamic Sistem)
    {
        SabahMomentumBacktest.Baslat(Sistem);
    }

    static Portfoy p = null;
    public void Portfoy(dynamic Sistem)
    {
        if (p == null)
        { 
            p = new Portfoy();
            p.Show();
        }
        p.Refresh(Sistem);

        //foreach (System.Windows.Forms.Form frm in System.Windows.Forms.Application.OpenForms)
        //{
        //    if (frm.Name == "formPortfolio")
        //    {
        //        var button = new System.Windows.Forms.Button();
        //        button.Size = new System.Drawing.Size(20, 43);
        //        button.Name = "Iko Portfoy";
        //        button.Click += Button_Click;
        //        button.Location = new System.Drawing.Point(100, 100);
        //        frm.Controls.Add(p);
        //    }
        //}

        
    }

    private void Button_Click(object sender, System.EventArgs e)
    {
        Portfoy p = new Portfoy();
        p.Show();
    }

}

