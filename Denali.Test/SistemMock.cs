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
        public string EmirIslem { get; set; }
        public int EmirMiktari { get;  set; }
        public double EmirFiyati { get;  set; }
        public string EmirSuresi { get;  set; }
        public string EmirTipi { get;  set; }
        public string EmirSatisTipi { get;  set; }

        public double SatisFiyat(string s)
        {
            return 42.40D;
        }

        public double AlisFiyat(string s)
        {
            return 50.40D;
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
        
        public double YuksekGun(string v)
        {
            return 1;
        }

        public double DusukGun(string v)
        {
            return 1;
        }

    }
}
