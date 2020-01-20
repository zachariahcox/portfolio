using System;

namespace PortfolioPicker.App
{
    public class Order
    {
        public static Order Create(
            string accountName,
            string symbol,
            double value)
        {
            if (value == 0.0)
            {
                return null;
            }
            return new Order
            {
                AccountName = accountName,
                Symbol = symbol,
                Value = Math.Abs(value),
                Action = value < 0 ? "sell" : "buy",
            };
        }

        public string AccountName { get; set; }

        public string Symbol { get; set; }

        public double Value { get; set; }

        public string Action { get; set; }
    }
}
