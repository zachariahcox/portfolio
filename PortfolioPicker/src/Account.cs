using System;
using System.Collections.Generic;

namespace PortfolioPicker
{
    public class Account
    {
        public string Name { get; set; }

        public string Brokerage { get; set; }

        public bool Taxable { get; set; } = false;

        public AccountType AccountType { get; set; } = AccountType.TAXABLE;

        public decimal Value { get; set; } = 0m;

        public IReadOnlyCollection<Fund> Funds { get; set; }

        public void ResolveFunds(FundsByBrokerageMap allFunds)
        {
            if (Funds == null && allFunds != null)
            {
                Funds = allFunds[this.Brokerage];
            }
        }

        public Fund GetFund(
            AssetClass c,
            AssetLocation l)
        {
            var best = (Fund)null;
            foreach (var f in Funds)
            {
                if (f.GetLocation() == l &&
                    f.GetClass() == c &&
                    (best == null || f.ExpenseRatio < best.ExpenseRatio))
                {
                    best = f;
                }
            }
            return best;
        }

        public override string ToString()
        {
            return String.Join("\n\t",
                Name,
                "Brokerage: " + Brokerage,
                "Value: " + String.Format("{0:c}", Convert.ToInt32(Value)),
                "Taxable?: " + Taxable.ToString(),
                "Type: " + AccountType.ToString(),
                "Funds: " + (this.Funds != null ? String.Join(", ", this.Funds) : "null")
            );
        }
    }
}