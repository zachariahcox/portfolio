using System.Collections.Generic;

namespace PortfolioPicker.App
{
    public class Portfolio
    {
        public string Strategy { get; set; }

        public decimal TotalValue { get; set; }

        public double ExpenseRatio { get; set; } = 0.0;

        public double BondPercent { get; set; } = 0.0;

        public double StockPercent { get; set; } = 0.0;

        public IReadOnlyList<Order> BuyOrders { get; set; }
    }
}
