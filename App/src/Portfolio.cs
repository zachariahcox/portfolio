using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System;
using System.IO;
using System.Diagnostics;

namespace PortfolioPicker.App
{
    public class Portfolio
    {
        public Portfolio(IList<Account> accounts, SecurityCache securityCache)
        {
            Accounts = accounts;
            AvailableSecurities = securityCache;
        }

        public SecurityCache AvailableSecurities {get; private set;}

        public int DescriptorKey => (int)(Positions.Sum(p => (double)p.Symbol.GetHashCode() * p.Value) % int.MaxValue);
            
        public Score Score {get; set;}

        public IList<string> Errors { get; set; }

        public IList<Account> Accounts {get; set;}

        public double TotalValue {
            get {
                if (_totalValue == -1)
                    Compute();
                return _totalValue;
            }
        }

        public double ExpenseRatio
        {
            get 
            {
                if (_expenseRatio == -1.0)
                    Compute();
                return _expenseRatio; 
            }
        }

        private void Compute()
        {
            _totalValue = 0;
            _expenseRatio = 0;
            _allPositions = new List<Position>();
            foreach (var a in Accounts)
            foreach (var p in a.Positions)
            {
                _allPositions.Add(p);
                var v = p.Value;
                _totalValue += v;
                _expenseRatio += v * AvailableSecurities.Get(p.Symbol).ExpenseRatio;
            }
            _expenseRatio /= _totalValue;
        }

        private IList<Position> _allPositions;
        /// <summary>
        /// collection of all positions from all accounts
        /// </summary>
        public IEnumerable<Position> Positions 
        {
            get {
                if (_allPositions == default)
                    Compute();
                return _allPositions;
            }
        }

        public int NumberOfPositions {
            get {
                if (_allPositions == default)
                    Compute();
                return _allPositions.Count;
            }
        }

        /// <summary>
        /// portfolios are serialized as a list of accounts.
        /// </summary>
        public static Portfolio FromYaml(string yaml, SecurityCache sc = null)
        {
            if (string.IsNullOrEmpty(yaml))
            {
                return null;
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            var accounts = deserializer.Deserialize<IList<Account>>(yaml);
            
            if (sc is null)
            {
                sc = new SecurityCache();
                sc.Add(Security.LoadDefaults());
            }

            return new Portfolio(accounts, sc);
        }

        public static Portfolio FromGoogleSheet(string json)
        {
            var data = GoogleSheetPortfolio.FromJson(json);
            if (data == null)
                return null;

            // load special securities                    
            var sc = new SecurityCache();
            sc.Add(data.Securities);
            
            return new Portfolio(data.Accounts, sc);
        }

        /// <summary>
        /// portfolios are serialized as a list of accounts.
        /// </summary>
        public string ToYaml()
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();
            return serializer.Serialize(Accounts).Replace("\r\n", "\n");
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
            return total <= 0 
                ? 0.0 
                : 100 * Value(t, c, l) / total;
        }

        /// <summary>
        /// Manually rescrape for the aggregation. 
        /// </summary>
        public double Value(AccountType t, AssetClass c, AssetLocation l)
        {
            // TODO: this might be improved with a cache
            var value = 0.0;
            var sc = this.AvailableSecurities;
            foreach(var a in Accounts)
            {
                if (t != AccountType.None && t != a.Type)
                    continue;

                foreach(var e in a.GetExposures(sc))
                    if (c == AssetClass.None || c == e.Class)
                    if (l == AssetLocation.None || l == e.Location)
                        value += e.Value;
            }
            return value;
        }

        ///<summary>
        /// a "score" for the portfolio between 0 and 1
        ///</summary>
        public virtual Score GetScore(
            Func<AssetClass, AssetLocation, AccountType, double> GetTaxOptimizationScoreWeight,
            ICollection<Exposure> targetExposureRatios)
        {
            // total weights should sum to 100
            Debug.Assert(100.0 ==  
                  Score.weight_assetMix 
                + Score.weight_useTaxOptimalAccounts 
                + Score.weight_lowExpenseRatio
                );

            var score = new Score()
            {
                AssetMixWeight = Score.weight_assetMix,
                TaxEfficiencyWeight = Score.weight_useTaxOptimalAccounts,
                ExpenseRatioWeight = Score.weight_lowExpenseRatio,
                TaxableSalesWeight = Score.weight_taxableSales
            };

            // keep expense ratios low: award zero points if this ER is higher than baseline
            const double vanguardTargetDateFundER = 0.15; // source: vanguard target retirement fund account expense ratio (VFIFX)
            if (ExpenseRatio < vanguardTargetDateFundER)
            {
                score.ExpenseRatio = 1.0 - ExpenseRatio / vanguardTargetDateFundER;
            }

            // owning the right assets: subtract points for not owning enough of the target
            var totalUnderExposureRatio = 0.0;
            foreach (var t in targetExposureRatios)
            {
                var actualRatio = PercentOfPortfolio(t.Class, t.Location) / 100;
                var under = t.Value - actualRatio;
                if (under > 0)
                    totalUnderExposureRatio += under;
            }
            score.AssetMix = 1 - totalUnderExposureRatio;

            // owning assets in tax-optimal accounts: subtract points if assets could be owned in a better account
            var optimizationScore = 0.0;
            foreach (var e in targetExposureRatios)
            {
                foreach (var accountType in AccountTypes.ALL)
                {
                    var fraction = PercentOfAssetType(accountType, e.Class, e.Location) / 100.0;
                    optimizationScore += GetTaxOptimizationScoreWeight(e.Class, e.Location, accountType) * fraction;
                }
            }
            score.TaxEfficiency = optimizationScore / targetExposureRatios.Count;

            // no sales required here!
            score.TaxableSales = 1;

            return score;
        }

        protected string Row(params object[] values) => "|" + string.Join("|", values) + "|";

        protected string MdUrl(string anchor, string href) => $"[{anchor}]({href})";
        
        protected string SymbolUrl(string s, string url=null) => MdUrl(s, url ?? $"https://finance.yahoo.com/quote/{s}?p={s}");

        protected double NotNan(double d) => double.IsNaN(d) ? 0.0 : d;

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
        public IList<string> ToMarkdown(Portfolio reference = null)
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

                foreach (var c in AssetClasses.ALL)
                    foreach (var l in AssetLocations.ALL)
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
                    foreach (var c in AssetClasses.ALL)
                        foreach (var l in AssetLocations.ALL)
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
                {
                    var security = AvailableSecurities.Get(p.Symbol);
                    lines.Add(Row(
                        a.Name, 
                        SymbolUrl(security.Symbol, security.Url),
                        string.Format("${0:n0}", p.Value), 
                        security.Description
                        ));
                }
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
        /// produce orders required to move from original portfolio to rebalanced one
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
    
        private double _totalValue = -1.0;
        private double _expenseRatio = -1;
    }
}
