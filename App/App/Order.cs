using System;

namespace PortfolioPicker.App
{
    public class Order
    {
        public static string Sell = "sell";
        public static string Buy = "buy";

        public static Order Create(
            Account account,
            string symbol,
            double value)
        {
            if (value == 0.0)
            {
                return null;
            }
            return new Order
            {
                Account = account,
                Symbol = symbol,
                Value = Math.Abs(value),
                Action = value < 0 ? Sell: Buy,
            };
        }

        public Account Account { get; set; }

        public string Symbol { get; set; }

        public double Value { get; set; }

        public string Action { get; set; }
    }
}
