using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace AlimSatimRobotu
{
    internal class Pozisyonlar
    {
        public string Symbol
        {
            get; set;
        }

        public double Lot
        {
            get; set;
        }

        public double LastPrice
        {
            get; set;
        }

        public double Sellable
        {
            get; set;
        }

        public double Cost
        {
            get; set;
        }

        public double Bloke
        {
            get; set;
        }

        public double ProfitX
        {
            get; set;
        }

        public string AssetType
        {
            get; set;
        }

        public string BalanceType
        {
            get; set;
        }

        public double LotT1
        {
            get; set;
        }

        public double LotT2
        {
            get; set;
        }

        public double Profit
        {
            get;set;
        }

        public double ProfitYuzde
        {
            get; set;
        }

        public double TotalTL
        {
            get; set;
        }



    }

    internal class Emirler
    {
        public string LongAccountName{get;set;}

        public string AccountName{get;set;}

        public string AccountNo{get;set;}

        public string OrderNo{get;set;}

        public string Symbol{get;set;}

        public string BuySell{get;set;}

        public double Amount{get;set;}

        public double AmountShowing{get;set;}

        public double GAmount{get;set;}

        public double Balance{get;set;}

        public double GPrice{get;set;}

        public double Price{get;set;}

        public double Total{get;set;}

        public double GTotal{get;set;}

        public string ValorDate{get;set;}

        public string Status{get;set;}

        public string StatusCode{get;set;}

        public string Session{get;set;}

        public string OrderPermit{get;set;}

        public string OrderDate{get;set;}

        public string OrderUpdateDate{get;set;}

        public string OrderEndDate{get;set;}

        public string OrderType{get;set;}

        public string CancelPermit{get;set;}

        public string AmendPermit{get;set;}

        public string ImprovePermit{get;set;}

        public string OneSessionPermit{get;set;}

        public string OrderRef{get;set;}

        public string OrderSessionNo{get;set;}

        public string ZincirRef{get;set;}

        public string Note{get;set;}

        public string Validity{get;set;}

        public string SatisTip{get;set;}

        public string GSaat{get;set;}

        public int EmirUpdateNum{get;set;}

        public int SiraNo{get;set;}

        public int MaxZincirSiraNo{get;set;}

        public string SessionName{get;set;}

        public string ExecutionStatus{get;set;}

        public byte Selected{get;set;}

        public string OrderNoString{get;set;}


    }

    internal class Islemler
    {
        public string AccountNo{get;set;}

        public string ProcessDate{get;set;}

        public string Symbol{get;set;}

        public string BuySell{get;set;}

        public double Price{get;set;}

        public int BuyLot{get;set;}

        public int SellLot{get;set;}

        public double BuyTotalTL{get;set;}

        public double SellTotalTL{get;set;}

        public double CommissionAmount{get;set;}

        public double Commission{get;set;}

        public double BSMV{get;set;}
    }

    internal class BekleyenEmirler
    {
        public List<string> OrderNoList{get;set;}

        public string Symbol{get;set;}

        public string BuySell{get;set;}

        public double Amount{get;set;}

        public double GAmount{get;set;}

        public double Balance{get;set;}

        public double GPrice{get;set;}

        public double Price{get;set;}

        public double Total{get;set;}

        public double GTotal{get;set;}

        public string Status{get;set;}

        public string SessionName{get;set;}

        public string OrderRef{get;set;}
    }
}
