using System;
using System.Collections.Generic;

namespace PortfolioPicker
{
    public enum AccountType
    {
        INVESTMENT,
        CORPORATE,
        ROTH
    }

    public class Account
    {
        public string name;
        public string brokerage;
        public bool taxable = false;
        public AccountType type = AccountType.INVESTMENT;
        public decimal value = 0m;
        public IReadOnlyList<string> funds = null;
        public IReadOnlyList<Fund> resolvedFunds = null;

        public void ResolveFunds(Data data)
        {
            //if (funds == null)
            //    resolvedFunds = data.GetBrokerageDefault(brokerage);
            //resolvedFunds = data.GetFunds(symbols: funds);
        }

        public override string ToString()
        {
            return String.Join("\n\t",
                name,
                "Brokerage: " + brokerage,
                "Value: " + String.Format("{0:c}", Convert.ToInt32(value)),
                "Taxable?: " + taxable.ToString(),
                "Type: " + type.ToString(),
                "Funds: " + (funds != null ? String.Join(", ", funds) : "null")
            );
        }
    }
}