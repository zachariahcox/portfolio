using System.Collections.Generic;
using System.Linq;

namespace PortfolioPicker.App
{
    public class RebalancedPortfolio : Portfolio
    {
        public RebalancedPortfolio(IList<Account> accounts)
        : base(accounts)
        {
            
        }

        public IList<Exposure> TargetExposureRatios {get; set;}

        public IList<Order> Orders { get; set; }

        public override IList<string> ToMarkdown(Portfolio reference)
        {
            var lines = base.ToMarkdown(reference);

            // ORDERS
            if (Orders?.Any() == true)
            {
                lines.Add("## orders");
                lines.Add(Row("account", "action", "symbol", "value"));
                lines.Add(Row("---", "---", "---", "---:"));
                foreach (var o in Orders
                    .OrderBy(x => x.AccountName)
                    .ThenByDescending(x => x.Action)
                    .ThenBy(x => x.Symbol))
                {
                    lines.Add(Row(o.AccountName, o.Action, SymbolUrl(o.Symbol), string.Format("${0:n2}", o.Value)));
                }
                lines.Add("");
            }

            return lines;
        }
    }
}
