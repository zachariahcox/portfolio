using System.Collections.Generic;
using System.Linq;
using System;
using System.IO;

namespace PortfolioPicker.App
{
    public class RebalancedPortfolio : Portfolio
    {
        public RebalancedPortfolio(
            IList<Account> accounts,
            Portfolio original,
            ICollection<Exposure> targetExposureRatios,
            IList<string> errors
            ) 
            : base(accounts, original.AvailableSecurities) 
        {
            Errors = errors;
            Original = original;
            TargetExposureRatios = targetExposureRatios;
            Orders = Portfolio.ComputeOrders(original, this);
            Score = GetScore(Score.GetScoreWeight, targetExposureRatios);
        }

        public Portfolio Original {get; set;}

        public ICollection<Exposure> TargetExposureRatios {get; set;}

        public ICollection<Order> Orders { get; set; }

        public override Score GetScore(
            Func<AssetClass, AssetLocation, AccountType, double> GetTaxOptimizationScoreWeight,
            ICollection<Exposure> targetExposureRatios)
        {
            var s = base.GetScore(GetTaxOptimizationScoreWeight, targetExposureRatios);
            s.TaxableSales = 1.0 - SumOfTaxableSales() / TotalValue;
            return s;
        }

        public override void Save(string directory)
        {
            base.Save(directory);
            File.WriteAllLines(Path.Combine(directory, $"rebalance.md"), ToMarkdown(Original));
        }

        private double SumOfTaxableSales()
        {
            var sum = 0.0;
            foreach(var o in Orders)
                if (o.Account.Type == AccountType.BROKERAGE && o.Action == Order.Sell)
                    sum += o.Value;
            return sum;
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

                lines.Add(Row("sum of taxable sales", string.Format("${0:n0}", SumOfTaxableSales())));
                lines.Add(Row("exposure priority order", string.Join("<br/>", 
                TargetExposureRatios.Select(x => x.Class.ToString().ToLower() + x.Location.ToString().ToLower()))
                ));
            }
            

            // SCORE
            if (Score != null)
            {
                lines.Add(Row("weight breakdown", 
                    "<table><tr><td>"
                    + string.Join("</td></tr><tr><td>",
                        string.Format("asset mix</td><td>{0:0.0}", Score.AssetMixWeight),
                        string.Format("tax efficiency</td><td>{0:0.0}", Score.TaxEfficiencyWeight),
                        string.Format("expense ratio</td><td>{0:0.0}", Score.ExpenseRatioWeight),
                        string.Format("taxable sales</td><td>{0:0.0}", Score.TaxableSalesWeight))
                    + "</td></tr></table>"
                ));

                lines.Add("## score breakdown");
                lines.Add(Row("portfolio", "total (sales)", "total", "asset mix", "tax efficiency", "expense ratio", "taxable sales"));
                lines.Add(Row("---", "---:", "---:", "---:", "---:", "---:", "---:"));
                lines.Add(Row("new", 
                    string.Format("{0:0.0000}", Score.RebalanceTotal),
                    string.Format("{0:0.0000}", Score.Total), 
                    string.Format("{0:0.0000}", Score.AssetMix), 
                    string.Format("{0:0.0000}", Score.TaxEfficiency), 
                    string.Format("{0:0.0000}", Score.ExpenseRatio),
                    string.Format("{0:0.0000}", Score.TaxableSales)
                ));

                lines.Add(Row("previous", 
                    "-",
                    string.Format("{0:0.0000}", reference.Score.Total), 
                    string.Format("{0:0.0000}", reference.Score.AssetMix), 
                    string.Format("{0:0.0000}", reference.Score.TaxEfficiency), 
                    string.Format("{0:0.0000}", reference.Score.ExpenseRatio),
                    string.Format("{0:0.0000}", reference.Score.TaxableSales)
                ));

                lines.Add("");
            }

            return lines;
        }

        public IList<string> ToMarkdown()
        {
            var lines = base.ToMarkdown(this.Original);

            // ORDERS
            if (Orders?.Any() == true)
            {
                lines.Add("## orders (" + Orders.Count + ")");
                lines.Add(Row("account", "action", "symbol", "value", "description"));
                lines.Add(Row("---", "---", "---", "---:", "---"));
                foreach (var o in Orders
                    .OrderBy(x => x.Account.Name)
                    .ThenByDescending(x => x.Action)
                    .ThenBy(x => x.Symbol))
                {
                    if (o.Value < 10)
                        continue; // not worth transaction cost.

                    var fund = AvailableSecurities.Get(o.Symbol);
                    lines.Add(Row(
                        o.Account.Name, 
                        o.Action, 
                        SymbolUrl(o.Symbol, fund.Url), 
                        string.Format("${0:n0}", o.Value), 
                        fund.Description
                        ));
                }
                lines.Add("");
            }

            return lines;
        }
    }
}
