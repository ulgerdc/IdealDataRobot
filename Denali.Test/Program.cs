
using ideal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Denali.Test
{
    internal class Program
    {
        static void Main(string[] args)
        {

            var gridBot = new GridBot();
            Console.WriteLine("Fiyat izleme başlatılıyor...");
            gridBot.StartWatching();

        }
    }
}
