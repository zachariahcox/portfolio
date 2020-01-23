using System.Collections.Generic;
using System.Linq;
using System;

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

        public override IList<string> GetMarkdownReportSummary(Portfolio reference = null)
        {
            var lines = base.GetMarkdownReportSummary(reference);
                    
            if (TargetExposureRatios?.Any() == true)
            {
                // percent stocks
                var p = TargetExposureRatios
                    .Where(x => x.Class == AssetClass.Stock)
                    .Sum(x => x.Value) * 100;
                lines.Add(Row("target % stocks", string.Format("{0:0.0}%", p)));

                // percent bonds
                p = TargetExposureRatios
                    .Where(x => x.Class == AssetClass.Bond)
                    .Sum(x => x.Value) * 100;
                lines.Add(Row("target % bonds", string.Format("{0:0.0}%", p)));

                // rebalance score
                lines.Add(Row("score", string.Format("{0:0.0000}", Score)));

                var exposures = Picker.ComputeExposures(TargetExposureRatios, reference.TotalValue);
                lines.Add(Row("previous score", string.Format("{0:0.0000}", reference.GetScore(exposures))));
            }

            return lines;
        }

        public override IList<string> ToMarkdown(Portfolio reference)
        {
            var lines = base.ToMarkdown(reference);

            // ORDERS
            if (Orders?.Any() == true)
            {
                lines.Add("## orders");
                lines.Add(Row("account", "action", "symbol", "value", "description"));
                lines.Add(Row("---", "---", "---", "---:", "---"));
                foreach (var o in Orders
                    .OrderBy(x => x.AccountName)
                    .ThenByDescending(x => x.Action)
                    .ThenBy(x => x.Symbol))
                {
                    lines.Add(Row(
                        o.AccountName, 
                        o.Action, 
                        SymbolUrl(o.Symbol), 
                        string.Format("${0:n0}", o.Value), 
                        Fund.Get(o.Symbol).Description
                        ));
                }
                lines.Add("");
            }

            return lines;
        }
    }
}
