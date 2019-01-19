using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace PortfolioPicker.App
{
    [DataContract]
    public class Account
    {
        [DataMember(IsRequired = true)]
        public string Name { get; set; }

        [DataMember(IsRequired = true)]
        public string Brokerage { get; set; }

        [DataMember(IsRequired = true)]
        public bool Taxable { get; set; } = false;

        [DataMember(IsRequired = true)]
        public AccountType Type { get; set; } = AccountType.TAXABLE;

        [DataMember(IsRequired = true)]
        public decimal Value { get; set; } = 0m;

        [IgnoreDataMember]
        internal IReadOnlyList<Fund> Funds { get; set; }

        //
        // APIs
        //

        internal void ResolveFunds(IReadOnlyDictionary<string, IReadOnlyList<Fund>> allFunds)
        {
            if (Funds == null && allFunds != null)
            {
                Funds = allFunds[Brokerage];
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
            return string.Join("\n\t",
                Name,
                "Brokerage: " + Brokerage,
                "Value: " + string.Format("{0:c}", Convert.ToInt32(Value)),
                "Taxable?: " + Taxable.ToString(),
                "Type: " + Type.ToString(),
                "Funds: " + (this.Funds != null ? string.Join(", ", this.Funds) : "null")
            );
        }
    }
}