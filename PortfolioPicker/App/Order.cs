using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace PortfolioPicker.App
{
    public class Order
    {
        public static Order Create(
            string accountName,
            string symbol,
            decimal value)
        {
            if (value == 0m)
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

        public decimal Value { get; set; }

        public string Action { get; set; }
    }
}
