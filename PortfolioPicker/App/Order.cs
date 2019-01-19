using System;
using System.Collections.Generic;

namespace PortfolioPicker.App
{
    public class Order
    {
        public Guid Id { get; private set; }

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
            this.Id = Guid.NewGuid();
        }

        public override string ToString()
        {
            return string.Join(", ", new List<string> {
                "Order",
                (Account != null ? Account.Name : "null"),
                (Fund != null ? Fund.Symbol : "null"),
                string.Format("{0:c}", Convert.ToInt32(Value))});
        }
    }
}
