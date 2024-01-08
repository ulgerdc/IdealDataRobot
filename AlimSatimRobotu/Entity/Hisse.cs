using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlimSatimRobotu.Entity
{
    internal class Hisse
    {
        public long Id { get; set; }
        private string hisseAdi;
        public string HisseAdi 
        {
            get {
                return hisseAdi; }
            set {
                hisseAdi = value;
            }
        }
        public double IlkFiyat { get; set; }
        public double Butce { get; set; }
        public double BaslangicKademe { get; set; }
        public double AlimTutari { get; set; }
        public double SonAlimTutari { get; set; }
        public DateTime PortfoyTarihi { get; set; }
        public DateTime SonIslemTarihi { get; set; }
        public MarjTipiEnum MarjTipi { get; set; }

        public int Marj { get; set; }
  
        public bool Aktif { get; set; }
    }

    public enum MarjTipiEnum
    {
        Kademe = 0,
        Binde = 1,
        ATR = 2
    }
}
