using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlimSatimRobotu
{
    internal class Pozisyonlar
    {
        public string Symbol { get; set; }
        
        public int Lot { get; set; }
        
        public double Cost { get; set; }
        
        public double Profit { get; set; }
        
        public double IlkLastPriceFiyat { get; set; }
        
    }

    internal class Emirler
    {
        public string Symbol { get; set; }
        public string OrderNo { get; set; }
        public string OrderDate { get; set; }
        public string BuySell { get; set; }
        public string Session { get; set; }
        public string OrderType { get; set; }
        public string Price { get; set; }
        public string Status { get; set; }


    }
}
