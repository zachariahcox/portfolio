using System;
using System.Collections.Generic;
using System.Text;

namespace PortfolioPicker
{
    public class Portfolio
    {
        public double ExpenseRatio = 0.0;
        public IReadOnlyList<Order> BuyOrders;
    }
}
