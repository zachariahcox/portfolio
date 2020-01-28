using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

namespace PortfolioPicker.App
{
    public class RebalancedPortfolio : Portfolio
    {
        public RebalancedPortfolio(IList<Account> accounts)
        : base(accounts)
        {   
        }

        public ICollection<Exposure> TargetExposureRatios {get; set;}

        public ICollection<Order> Orders { get; set; }

        public double OrdersScore {get; set;}

        public Portfolio Original {get; set;}

        /// <summary>
        /// how bad would it be to perform this rebalance?
        /// </summary>
        public double GetOrdersScore()
        {
            // taxable sales are bad
            var taxableSales = Orders
                .Where(x => x.Account.Type == AccountType.BROKERAGE)
                .Where(x => x.Action == Order.Sell)
                .Sum(x => x.Value);

            return taxableSales / TotalValue / 4;
        }

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
                lines.Add(Row("previous score", string.Format("{0:0.0000}", reference.GetScore(TargetExposureRatios))));
            }
            
            lines.Add(Row("orders score", string.Format("{0:0.0000}", OrdersScore)));
            lines.Add(Row("sum of taxable sales", 
                string.Format("${0:n0}", Orders
                    .Where(x => x.Account.Type == AccountType.BROKERAGE)
                    .Where(x => x.Action == Order.Sell)
                    .Sum(x => x.Value))
                ));
            lines.Add(Row("weighted score", string.Format("{0:0.0000}", Score - OrdersScore)));
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
                    .OrderBy(x => x.Account.Name)
                    .ThenByDescending(x => x.Action)
                    .ThenBy(x => x.Symbol))
                {
                    lines.Add(Row(
                        o.Account.Name, 
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

        public override void Save(string directory)
        {
            base.Save(directory);
            File.WriteAllLines(Path.Combine(directory, $"rebalance.md"), ToMarkdown(Original));
        }
    }
}
