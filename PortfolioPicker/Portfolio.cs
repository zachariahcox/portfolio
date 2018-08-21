using System;
using System.Collections.Generic;
using System.Text;

namespace PortfolioPicker
{
    public class Portfolio
    {
        public double total_expense_ratio = 0.0;
        public IReadOnlyList<Order> buy_orders;
    }
}
