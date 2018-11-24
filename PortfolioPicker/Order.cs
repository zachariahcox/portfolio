using System;
using System.Collections.Generic;

namespace PortfolioPicker
{
    public class Order
    {
        public Account Account { get; set; }
        public Fund Fund { get; set; }
        public decimal Value { get; set; } = 0m;

        public Order(
            Account account = null, 
            Fund fund = null, 
            decimal value = 0m)
        {
            this.Account = account;
            this.Fund = fund;
            this.Value = value;
        }

        public override string ToString()
        {
            return String.Join(", ", new List<String> {
                "Order",
                (Account != null ? Account.Name : "null"),
                (Fund != null ? Fund.Symbol : "null"),
                String.Format("{0:c}", Convert.ToInt32(Value))});
        }
    }
}
