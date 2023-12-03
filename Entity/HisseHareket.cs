﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlimSatimRobotu.Entity
{
    internal class HisseHareket
    {
        public long Id { get; set; }
        public string HisseAdi { get; internal set; }
        public double AlisFiyati { get; internal set; }
        public double SatisFiyati { get; internal set; }
        public int Lot { get; set; }
        public bool AktifMi { get; internal set; }
        public double Kar { get; internal set; }
    }

    internal class HissePozisyonlari
    {
        public long Id { get; set; }
        public string HisseAdi { get; internal set; }
        public double ToplamAlisFiyati { get; internal set; }
        public double ToplamSatisFiyati { get; internal set; }
        public int AcikPozisyonSayisi { get; set; }
        public double AcikPozisyonAlimTutari { get; internal set; }
        public double ToplamKar { get; internal set; }
    }
}
