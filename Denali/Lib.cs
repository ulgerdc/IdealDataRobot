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

