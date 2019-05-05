using System.Collections.Generic;
using System.Linq;

namespace PortfolioPicker.App
{
    public class RebalancedPortfolio : Portfolio
    {
        public double Score { get; set; }

        public IList<Order> Orders { get; set; }

        public IList<string> Warnings { get; set; }

        public IList<string> Errors { get; set; }

        public override IList<string> ToMarkdown()
        {
            var lines = base.ToMarkdown();

            // ORDERS
            if (Orders?.Any() == true)
            {
                lines.Add("## orders");
                lines.Add(Row(lines, "account", "action", "symbol", "value"));
                lines.Add(Row(lines, "---", "---", "---", "---:"));
                foreach (var o in Orders
                    .OrderBy(x => x.AccountName)
                    .ThenByDescending(x => x.Action)
                    .ThenBy(x => x.Symbol))
                {
                    lines.Add(Row(lines, o.AccountName, o.Action, Url(o.Symbol), string.Format("{0:c}", o.Value)));
                }
            }
            return lines;
        }
    }
}
