using System;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Decide which funds to use from available
        /// </summary>
        internal void SelectFunds(IReadOnlyList<Fund> funds)
        {
            if (Funds == null && funds != null)
            {
                var matches = funds.Where(x => string.Equals(x.Brokerage, Brokerage, StringComparison.OrdinalIgnoreCase)).ToList();
                matches.Sort((x, y) => x.Symbol.CompareTo(y.Symbol));
                this.Funds = matches as IReadOnlyList<Fund>;
            }
        }

        /// <summary>
        /// Pick the best fund meeting the requirements from the list available to this account
        /// </summary>
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