using System;

namespace PortfolioPicker.App
{
    public class Fund
    {
        public String Symbol { get; set; }
        public String Description { get; set; }
        public String URL { get; set; }
        public double ExpenseRatio { get; set; } = -1.0;
        public bool Domestic { get; set; } = true;
        public bool Stock { get; set; } = true;
        public String Exposure { get; set; }

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
            return String.Format("{0} ({1})", Symbol, ExpenseRatio);
        }
    }
}