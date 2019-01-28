using System.Collections.Generic;

namespace PortfolioPicker.App
{
    public class Portfolio
    {
        public string Strategy { get; set; }

        public IReadOnlyList<Order> BuyOrders { get; set; }

        public IList<string> Warnings { get; set; }

        public IList<string> Errors { get; set; }

        public double Score { get; set; }

        // Stats
        public decimal TotalValue { get; set; }

        public double ExpenseRatio { get; set; } = 0.0;

        public double BondRatio { get; set; } = 0.0;

        public double StockRatio { get; set; } = 0.0;

        public double DomesticRatio { get; set; }

        public double InternationalRatio { get; set; }
    }
}
