using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AlimSatimRobotu
{
    internal class Helper
    {
        public static void AlimIcinKararVer(string hisseAdi, double tutar)
        {
            if (DatabaseHelper.VarmiKontrolEt(hisseAdi, tutar, IslemEnum.Alim))
            { 
                
            }
        }

        public static void SatimIcinKararVer(string hisseAdi, double tutar)
        {
            if (DatabaseHelper.VarmiKontrolEt(hisseAdi, tutar, IslemEnum.Satim))
            {

            }
        }
    }

    internal class DatabaseHelper
    {
        
        public static bool VarmiKontrolEt(string hisseAdi, double tutar, IslemEnum islemEnum )
        {
            return true;
        }
    }

    public enum IslemEnum
    { 
        Alim,
        Satim
    }
}
