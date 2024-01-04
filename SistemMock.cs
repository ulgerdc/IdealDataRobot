using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlimSatimRobotu
{
    internal class SistemMock
    {
        public string AlgoIslem { get; set; }
        public bool BaglantiVar { get; set;}

        public string Saat { get { return "10:05:00"; } }

        public string EmirSembol { get; set; }
        public string EmirIslem { get; internal set; }
        public int EmirMiktari { get; internal set; }
        public string EmirFiyati { get; internal set; }
        public string EmirSuresi { get; internal set; }
        public string EmirTipi { get; internal set; }
        public string EmirSatisTipi { get; internal set; }

        public void Debug (string s)
        {

        }

        public double AlisFiyat(string s)
        {
            return 1;
        }

        internal void EmirGonder()
        {
            throw new NotImplementedException();
        }

        internal void PozisyonKontrolGuncelle(string emirSembol, int lot)
        {
            throw new NotImplementedException();
        }

        internal void PozisyonKontrolOku(string emirSembol)
        {
            throw new NotImplementedException();
        }

        internal double Taban(string v)
        {
            throw new NotImplementedException();
        }

        internal double Tavan(string v)
        {
            throw new NotImplementedException();
        }
    }
}
