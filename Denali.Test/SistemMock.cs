using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Denali.Test
{
    public class SistemMock
    {
        public string Name { get; set; }
        public string AlgoIslem { get; set; }
        public bool BaglantiVar { get { return true; } }

        public string Saat { get { return "10:05:00"; } }

        public string EmirSembol { get; set; }
        public string EmirIslem { get; internal set; }
        public int EmirMiktari { get; internal set; }
        public string EmirFiyati { get; internal set; }
        public string EmirSuresi { get; internal set; }
        public string EmirTipi { get; internal set; }
        public string EmirSatisTipi { get; internal set; }

        public double SatisFiyat(string s)
        {
            return 92.65D;
        }

        public double AlisFiyat(string s)
        {
            return 92.45D;
        }

        public void Debug (string s)
        {

        }

        public void EmirGonder()
        {
            
        }

        public void PozisyonKontrolGuncelle(string emirSembol, int lot)
        {
            
        }

        public void PozisyonKontrolOku(string emirSembol)
        {
            
        }

        public double Taban(string v)
        {
            return 1;
        }

        public double Tavan(string v)
        {
            return 1;
        }
    }
}
