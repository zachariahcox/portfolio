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

        public dynamic ToReport()
        {
            var reference = this.Original;
            // basic rebalance stats
            //
            var stats = new List<dynamic>();
            void AddStat(dynamic k, dynamic v){stats.Add(new {stat = k, value = v});}
            AddStat("total value of assets", NotNan(TotalValue));
            AddStat("total expense ratio", NotNan(ExpenseRatio));
            AddStat("previous total expense ratio", NotNan(reference.ExpenseRatio));
            AddStat("morningstar xray (upload csv)", "https://www.tdameritrade.com/education/tools-and-calculators/morningstar-instant-xray.page");
            
            if (TargetExposureRatios?.Any() == true)
            {
                AddStat("target % stocks", TargetExposureRatios
                    .Where(x => x.Class == AssetClass.Stock)
                    .Sum(x => x.Value));
                AddStat("target % bonds", TargetExposureRatios
                    .Where(x => x.Class == AssetClass.Bond)
                    .Sum(x => x.Value));
                
                AddStat("sum of taxable sales", SumOfTaxableSales());
                AddStat("exposure priority order",
                    string.Join(", ", TargetExposureRatios.Select(x => x.Class.ToString().ToLower() + x.Location.ToString().ToLower()))
                );
            }

            var scoreComparison = new List<dynamic>();
            if (Score != null)
            {
                AddStat("weights: asset mix", Score.AssetMixWeight);
                AddStat("weights: tax efficiency", Score.TaxEfficiencyWeight);
                AddStat("weights: expense ratio", Score.ExpenseRatioWeight);
                AddStat("weights: taxable sales", Score.TaxableSalesWeight);
            
                scoreComparison.Add(new {
                    portfolio = "proposed",
                    totalModifiedBySales = Score.RebalanceTotal,
                    totalIgnoringSales = Score.Total,
                    assetMix = Score.AssetMix,
                    taxEfficiency = Score.TaxEfficiency,
                    expenseRatio = Score.ExpenseRatio,
                    taxableSalesPenalty = Score.TaxableSales
                });
                scoreComparison.Add(new {
                    portfolio = "current",
                    totalModifiedBySales = reference.Score.RebalanceTotal,
                    totalIgnoringSales = reference.Score.Total,
                    assetMix = reference.Score.AssetMix,
                    taxEfficiency = reference.Score.TaxEfficiency,
                    expenseRatio = reference.Score.ExpenseRatio,
                    taxableSalesPenalty = reference.Score.TaxableSales
                });
            }

            // final composition
            //
            var composition = new List<dynamic>();
            foreach (var c in AssetClasses.ALL)
            foreach (var l in AssetLocations.ALL)
            {
                var percentOfPortfolio = PercentOfPortfolio(c, l);
                composition.Add(new {
                    Class = c == AssetClass.None ? "*" : c.ToString().ToLower(),
                    Location = l == AssetLocation.None ? "*" : l.ToString().ToLower()
                    
                    // total value
                    , Value = TotalValue * percentOfPortfolio / 100.0

                    // percent of portfolio
                    , TotalPercent = percentOfPortfolio / 100.0

                    // percent of asset class
                    , ClassPercent = NotNan(percentOfPortfolio / PercentOfPortfolio(c))

                    // percent of asset location
                    , LocationPercent = NotNan(percentOfPortfolio / PercentOfPortfolio(l))

                    // percent of asset category in brokerage accounts
                    , BrokeragePercent = PercentOfAssetType(AccountType.BROKERAGE, c, l) / 100.0

                    // percent of asset category in ira accounts
                    , IraPercent = PercentOfAssetType(AccountType.IRA, c, l) / 100.0

                    // percent of asset category in roth accounts
                    , RothPercent = PercentOfAssetType(AccountType.ROTH, c, l) / 100.0
                });
            }
            
            // comparison vs reference
            //
            var comparisonObject = default(List<dynamic>);
            if (reference != null)
            {
                comparisonObject = new List<dynamic>();
                foreach (var c in AssetClasses.ALL)
                foreach (var l in AssetLocations.ALL)
                {
                    var percentOfPortfolio = PercentOfPortfolio(c, l);
                    var referencePercentOfPortfolio = reference.PercentOfPortfolio(c, l);
                    comparisonObject.Add(new {
                        // aggregated by x
                        Class = c == AssetClass.None ? "*" : c.ToString().ToLower(),
                        Location = l == AssetLocation.None ? "*" : l.ToString().ToLower(), 
                        
                        // total value
                        Value = NotNan(TotalValue * percentOfPortfolio - reference.TotalValue * referencePercentOfPortfolio) / 100.0,

                        // percent of portfolio
                        TotalPercent = percentOfPortfolio - referencePercentOfPortfolio, 

                        // percent of asset class
                        ClassPercent = NotNan((NotNan(percentOfPortfolio / PercentOfPortfolio(c)) - referencePercentOfPortfolio / reference.PercentOfPortfolio(c))),

                        // percent of asset location
                        LocationPercent = NotNan((NotNan(percentOfPortfolio / PercentOfPortfolio(l)) - referencePercentOfPortfolio / reference.PercentOfPortfolio(l))),

                        // percent of asset category in brokerage accounts
                        BrokeragePercent = (PercentOfAssetType(AccountType.BROKERAGE, c, l) - reference.PercentOfAssetType(AccountType.BROKERAGE, c, l)) / 100.0,

                        // percent of asset category in ira accounts
                        IraPercent = (PercentOfAssetType(AccountType.IRA, c, l) - reference.PercentOfAssetType(AccountType.IRA, c, l)) / 100.0,

                        // percent of asset category in roth accounts
                        RothPercent = (PercentOfAssetType(AccountType.ROTH, c, l) - reference.PercentOfAssetType(AccountType.ROTH, c, l)) / 100.0
                    });
                }    
            }

            // currently held positions
            //
            var positions = new List<dynamic>();
            foreach (var a in Accounts.OrderBy(x => x.Name))
            foreach (var p in a.Positions.OrderByDescending(x => x.Value))
            {
                var security = AvailableSecurities.Get(p.Symbol);
                positions.Add(new {
                    Account = a.Name,
                    Symbol = security.Symbol,
                    Value = p.Value,
                    // Url = security.Url,
                    // Description = security.Description
                });
            }

            // orders to get to new positions
            //
            var ordersObject = default(List<dynamic>);
            if (Orders?.Any() == true)
            {
                ordersObject = new List<dynamic>();
                foreach (var o in Orders
                    .Where(x => x.Value >= 10)
                    .OrderBy(x => x.Account.Name)
                    .ThenByDescending(x => x.Action)
                    .ThenBy(x => x.Symbol))
                {
                    var security = AvailableSecurities.Get(o.Symbol);
                    ordersObject.Add(new {
                        Action = o.Action, 
                        Account = o.Account.Name, 
                        Symbol = o.Symbol, 
                        Value = o.Value, 
                        Shares = $"=ifna({o.Value} / GOOGLEFINANCE(\"o.Symbol\", \"price\"), 0)"
                        // Url = security.Url,
                        // Description = security.Description
                    });
                }
            }

            // construct json object
            //
            return new {
                Stats = stats,
                ScoreComparison = scoreComparison,
                Composition = composition,
                CompositionComparison = comparisonObject,
                Positions = positions,
                Orders = ordersObject,
            };
        }
    }
}
