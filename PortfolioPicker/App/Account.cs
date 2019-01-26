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
        /// Pick the best fund meeting the requirements from the list available to this account. 
        /// A "better" fund has better ratios for the target exposure, or has the lowest expense ratio. 
        /// </summary>
        internal Fund GetFund(Exposure e)
        {
            var best = (Fund)null;
            var bestCoverage = -1.0;
            foreach (var f in Funds)
            {
                var coverageForThisExposure = f.Ratio(e);
                if (coverageForThisExposure == 0.0)
                    continue;

                if (best == null)
                {
                    best = f;
                    bestCoverage = coverageForThisExposure;
                }
                else if(coverageForThisExposure > bestCoverage || f.ExpenseRatio < best.ExpenseRatio)
                {
                    best = f;
                }
            }
            return best;
        }

        public override string ToString()
        {
            return $"{Name}, value: {string.Format("{0:c}", Convert.ToInt32(Value))}, taxable: {Taxable}, type: {Type}";
        }
    }
}