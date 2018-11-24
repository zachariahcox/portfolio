using System.Collections.Generic;

namespace PortfolioPicker.App
{
    public class Portfolio
    {
        public double ExpenseRatio = 0.0;
        public IReadOnlyList<Order> BuyOrders;
    }
}
