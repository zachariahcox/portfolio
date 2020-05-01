﻿using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System;
using System.IO;

namespace PortfolioPicker.App
{
    public class Portfolio
    {
        public Portfolio(IList<Account> accounts) => Accounts = accounts;

        public int DescriptorKey => (int)(Positions.Sum(p => (double)p.Symbol.GetHashCode() * p.Value) % int.MaxValue);
            
        public double Score {get; set;}

        public IList<string> Errors { get; set; }

        public IList<Account> Accounts {get; set;}

        private double _totalValue = -1.0;
        public double TotalValue => _totalValue == -1.0
            ? _totalValue = Accounts.SelectMany(x => x.Positions).Sum(x => x.Value)
            : _totalValue;

        private double _expenseRatio = -1;
        public double ExpenseRatio => _expenseRatio == -1.0 
            ? _expenseRatio = TotalValue == 0.0 
                ? 0.0 
                : Accounts
                    .SelectMany(x => x.Positions)
                    .Sum(x => x.Value * Fund.Get(x.Symbol).ExpenseRatio) / TotalValue
            : _expenseRatio;

        // public IList<Exposure> Exposures { get; private set; }

        /// <summary>
        /// collection of all positions from all accounts
        /// </summary>
        public IEnumerable<Position> Positions => Accounts.SelectMany(x => x.Positions).ToArray();

        private int _numberOfPositions = -1;
        public int NumberOfPositions => _numberOfPositions == -1 
            ? _numberOfPositions = Accounts.Sum(x => x.Positions.Count)
            : _numberOfPositions;

        /// <summary>
        /// portfolios are serialized as a list of accounts.
        /// </summary>
        public static Portfolio FromYaml(string yaml)
        {
            if (string.IsNullOrEmpty(yaml))
            {
                return null;
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            var accounts = deserializer.Deserialize<IList<Account>>(yaml);
            return new Portfolio(accounts);
        }

        /// <summary>
        /// portfolios are serialized as a list of accounts.
        /// </summary>
        public string ToYaml()
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();
            return serializer.Serialize(Accounts);
        }

        public double PercentOfPortfolio(AssetClass c, AssetLocation l) => 100 * Value(AccountType.None, c, l) / TotalValue;
        public double PercentOfPortfolio(AssetClass c) => PercentOfPortfolio(c, AssetLocation.None);
        public double PercentOfPortfolio(AssetLocation l) => PercentOfPortfolio(AssetClass.None, l);

        public double PercentOfAssetType(
            AccountType t, 
            AssetClass c, 
            AssetLocation l)
        {
            var total = Value(AccountType.None, c, l); // ignore account type
            return total <= 0 ? 0.0 : 100 * Value(t, c, l) / total;
        }

        public double Value(AccountType t, AssetClass c, AssetLocation l) => Accounts
            .Where(x => (t == AccountType.None || x.Type == t))
            .SelectMany(x => x.Exposures)
            .Where(x => (c == AssetClass.None || x.Class == c) && (l == AssetLocation.None || x.Location == l))
            .Sum(x => x.Value);

        public double GetScore(
            Func<AssetClass, AssetLocation, AccountType, double> GetScoreWeight,
            ICollection<Exposure> targetExposureRatios)
        {
            var score = 0.0;
            var perfectScore = 0.0;
            
            // penalize cash positions
            perfectScore += 1;
            var cashValue = Positions.Where(x => x.Symbol == Cash.CASH).Sum(x => x.Value);
            score += 1.0 - cashValue / TotalValue;

            // incentivize being close to the target exposure ratios
            perfectScore += targetExposureRatios.Count;
            foreach (var e in targetExposureRatios)
            {
                var actualRatio = PercentOfPortfolio(e.Class, e.Location) / 100;
                score += 1.0 - Math.Abs((actualRatio - e.Value));
            }

            // we want to meet our account type preference goals
            perfectScore += targetExposureRatios.Count;
            foreach (var e in targetExposureRatios)
            {
                if (e.Value == 0)
                {
                    score += 1; // no money should be allocated here
                    continue; 
                }

                // score each allocation
                foreach (var t in Enum.GetValues(typeof(AccountType)).Cast<AccountType>())
                {
                    var fraction = PercentOfAssetType(t, e.Class, e.Location) / 100;
                    score += GetScoreWeight(e.Class, e.Location, t) * fraction;
                }
            }

            // expenses are bad -- how does this ER compare to a baseline?
            var baselineER = 0.15; // vanguard target retirement fund account er
            perfectScore += 1;
            score += 1 - ExpenseRatio / baselineER;

            return score / perfectScore;
        }

        protected string Row(params object[] values) => "|" + string.Join("|", values) + "|";

        protected string MdUrl(string anchor, string href) => $"[{anchor}]({href})";
        
        protected string SymbolUrl(string s) => MdUrl(s, $"https://finance.yahoo.com/quote/{s}?p={s}");

        private double NotNan(double d) => double.IsNaN(d) ? 0.0 : d;

        private string GetRelativeRow(AssetClass c, AssetLocation l, Portfolio p)
        {
            var percentOfPortfolio = PercentOfPortfolio(c, l);
            var referencePercentOfPortfolio = p.PercentOfPortfolio(c, l);
            string F(double d) => d == 0 ? "--" : d > 0 
                ? string.Format("+{0:0.0}%", d)
                : string.Format("{0:0.0}%", d);
            string D(double d) => d == 0 ? "--" : d > 0
                ? string.Format("+${0:n0}", d)
                : string.Format("{0:n0}", d);
            return Row(
                c == AssetClass.None ? "*" : c.ToString().ToLower(),
                l == AssetLocation.None ? "*" : l.ToString().ToLower()
                
                // total value
                , D((TotalValue * percentOfPortfolio - p.TotalValue * referencePercentOfPortfolio) / 100)

                // percent of portfolio
                , F(percentOfPortfolio - referencePercentOfPortfolio)

                // percent of asset class
                , F(100 * (NotNan(percentOfPortfolio / PercentOfPortfolio(c)) - referencePercentOfPortfolio / p.PercentOfPortfolio(c)))

                // percent of asset location
                , F(100 * (NotNan(percentOfPortfolio / PercentOfPortfolio(l)) - referencePercentOfPortfolio / p.PercentOfPortfolio(l)))

                // percent of asset category in brokerage accounts
                , F(PercentOfAssetType(AccountType.BROKERAGE, c, l) - p.PercentOfAssetType(AccountType.BROKERAGE, c, l))

                // percent of asset category in ira accounts
                , F(PercentOfAssetType(AccountType.IRA, c, l) - p.PercentOfAssetType(AccountType.IRA, c, l))

                // percent of asset category in roth accounts
                , F(PercentOfAssetType(AccountType.ROTH, c, l) - p.PercentOfAssetType(AccountType.ROTH, c, l))
                );
        }

        private string GetRow(AssetClass c, AssetLocation l)
        {
            var percentOfPortfolio = PercentOfPortfolio(c, l);
            return Row(
                c == AssetClass.None ? "*" : c.ToString().ToLower(),
                l == AssetLocation.None ? "*" : l.ToString().ToLower()
                
                // total value
                , string.Format("${0:n0}", TotalValue * percentOfPortfolio / 100)

                // percent of portfolio
                , string.Format("{0:0.0}%", percentOfPortfolio)

                // percent of asset class
                , string.Format("{0:0.0}%", NotNan(100 * percentOfPortfolio / PercentOfPortfolio(c)))

                // percent of asset location
                , string.Format("{0:0.0}%", NotNan(100 * percentOfPortfolio / PercentOfPortfolio(l)))

                // percent of asset category in brokerage accounts
                , string.Format("{0:0.0}%", PercentOfAssetType(AccountType.BROKERAGE, c, l))

                // percent of asset category in ira accounts
                , string.Format("{0:0.0}%", PercentOfAssetType(AccountType.IRA, c, l))

                // percent of asset category in roth accounts
                , string.Format("{0:0.0}%", PercentOfAssetType(AccountType.ROTH, c, l))
                );
        }

        public virtual IList<string> GetMarkdownReportSummary(Portfolio reference = null)
        {
            var lines = new List<string>();
            lines.Add("## summary");
            lines.Add(Row("stat", "value"));
            lines.Add(Row("---", "---"));
            lines.Add(Row("total value of assets", string.Format("${0:n0}", TotalValue)));

            lines.Add(Row("total expense ratio", string.Format("{0:0.0000}", NotNan(ExpenseRatio))));
            if (reference != null)
            {
                lines.Add(Row("previous total expense ratio", 
                    string.Format("{0:0.0000}", NotNan(reference.ExpenseRatio))));   
            }
            
            lines.Add(Row("morningstar xray", MdUrl(
                    "upload associated csv", 
                    "https://www.tdameritrade.com/education/tools-and-calculators/morningstar-instant-xray.page")));
            return lines;
        }

        /// <summary>
        /// produces a markdown report description of how current portfolio is different from reference
        /// </summary>
        public virtual IList<string> ToMarkdown(Portfolio reference = null)
        {
            var lines = new List<string>();

            lines.AddRange(GetMarkdownReportSummary(reference));
            lines.Add("");

            // ACCOUNT STATS
            if (Accounts?.Any() == true)
            {
                lines.Add("## composition");
                lines.Add(Row("class", "location", "value"
                    , "% total", "% class", "% location"
                    , AccountType.BROKERAGE.ToString().ToLower() 
                    , AccountType.IRA.ToString().ToLower()
                    , AccountType.ROTH.ToString().ToLower()
                    ));
                lines.Add(Row("---", "---", "---:",
                    "---:", "---:", "---:", 
                    "---:", "---:", "---:"));

                foreach (var c in Enum.GetValues(typeof(AssetClass)).Cast<AssetClass>())
                    foreach (var l in Enum.GetValues(typeof(AssetLocation)).Cast<AssetLocation>())
                        lines.Add(GetRow(c, l));
                lines.Add("");

                if (reference != null)
                {
                    lines.Add("## composition relative to original");
                    lines.Add(Row("class", "location", "value"
                        , "% total", "% class", "% location"
                        , AccountType.BROKERAGE.ToString().ToLower() 
                        , AccountType.IRA.ToString().ToLower()
                        , AccountType.ROTH.ToString().ToLower()
                        ));
                    lines.Add(Row("---", "---", "---:",
                        "---:", "---:", "---:", 
                        "---:", "---:", "---:"));
                    foreach (var c in Enum.GetValues(typeof(AssetClass)).Cast<AssetClass>())
                        foreach (var l in Enum.GetValues(typeof(AssetLocation)).Cast<AssetLocation>())
                            lines.Add(GetRelativeRow(c, l, reference));   
                    lines.Add("");
                }
            }

            // POSIITONS
            if (Positions?.Any() == true)
            {
                lines.Add($"## positions ({NumberOfPositions})");
                lines.Add(Row("account", "symbol", "value", "description"));
                lines.Add(Row("---", "---", "---:", "---"));
                foreach (var a in Accounts.OrderBy(x => x.Name))
                    foreach (var p in a.Positions.OrderByDescending(x => x.Value))
                        lines.Add(Row(
                            a.Name, 
                            SymbolUrl(p.Symbol), 
                            string.Format("${0:n0}", p.Value), 
                            Fund.Get(p.Symbol).Description));
                lines.Add("");
            }
            
            return lines;
        }

        /// <summary>
        /// produce CSV file compatible with: 
        /// https://www.tdameritrade.com/education/tools-and-calculators/morningstar-instant-xray.page
        /// </summary>
        public IList<string> ToXrayCsv()
        {
            var byPosition = Positions
                .GroupBy(x => x.Symbol,
                         (key, values) => new {
                            Symbol = key,
                            Value = Math.Floor(values.Sum(x => x.Value))
                            })
                .OrderByDescending(x => x.Value)
                .ThenBy(x => x.Symbol);

            var lines = new List<string>();
            lines.Add("Ticker,Dollar Amount");
            foreach(var g in byPosition)
                lines.Add($"{g.Symbol},{g.Value}");
            return lines;
        }

        public virtual void Save(string directory)
        {   
            Directory.CreateDirectory(directory);
            File.WriteAllText(Path.Combine(directory, $"portfolio.yaml"), ToYaml());
            File.WriteAllLines(Path.Combine(directory, $"portfolio.csv"), ToXrayCsv());
        }

        /// <summary>
        /// produce orders required to move from original portfolio to new one
        /// </summary>
        public static IList<Order> ComputeOrders(Portfolio original, Portfolio balanced)
        {
            var orders = new List<Order>();
            var accounts = original.Accounts.Union(balanced.Accounts);

            foreach (var a in accounts)
            {
                var newA = balanced.Accounts.FirstOrDefault(x => x == a);
                var oldA = original.Accounts.FirstOrDefault(x => x == a);

                if (newA is null)
                {
                    // sell entire account
                    orders.AddRange(oldA.Positions.Select(x => Order.Create(a, x.Symbol, -x.Value)));
                }
                else if (oldA is null)
                {
                    // buy entire account?
                    orders.AddRange(newA.Positions.Select(x => Order.Create(a, x.Symbol, x.Value)));
                }
                else
                {
                    // modify position
                    var symbols = new HashSet<string>();
                    foreach (var p in oldA.Positions) { symbols.Add(p.Symbol); }
                    foreach (var p in newA.Positions) { symbols.Add(p.Symbol); }
                    foreach (var s in symbols)
                    {
                        var newP = newA.Positions.FirstOrDefault(x => x.Symbol == s);
                        var oldP = oldA.Positions.FirstOrDefault(x => x.Symbol == s);
                        var difference = (newP == null ? 0.0 : newP.Value) - (oldP == null ? 0.0 : oldP.Value);
                        orders.Add(Order.Create(a, s, difference));
                    }
                }
            }

            return orders.Where(x => x != null).ToList();
        }
    }
}