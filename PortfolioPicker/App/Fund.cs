
using System;

namespace PortfolioPicker.App
{
    public class Fund
    {
        public string Symbol { get; set; }

        public string Description { get; set; }

        public string URL { get; set; }

        public double ExpenseRatio { get; set; } = -1.0;

        public bool Domestic { get; set; } = true;

        public bool Stock { get; set; } = true;

        public string Exposure { get; set; }

        public AssetLocation GetLocation()
        {
            return Domestic
                ? AssetLocation.Domestic
                : AssetLocation.International;
        }

        public AssetClass GetClass()
        {
            return Stock
                ? AssetClass.Stock
                : AssetClass.Bond;
        }

        public override string ToString()
        {
            return string.Format("{0} ({1})", Symbol, ExpenseRatio);
        }
    }
}