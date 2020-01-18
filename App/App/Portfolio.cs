using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System;

namespace PortfolioPicker.App
{
    public class Portfolio
    {
        public IList<Account> Accounts
        {
            get => _accounts;
            set
            {
                if (value is null)
                {
                    _accounts = null;
                }
                else
                {
                    _accounts = value.OrderBy(x => x.Name).ToList();
                    
                    // total value
                    TotalValue = _accounts
                        .SelectMany(x => x.Positions)
                        .Sum(x => x.Value);

                    // expense ratio
                    var weightedValue = _accounts
                        .SelectMany(x => x.Positions)
                        .Sum(x => (double)x.Value * Fund.Get(x.Symbol).ExpenseRatio); 
                    ExpenseRatio = TotalValue == 0 ? 0 : weightedValue / (double)TotalValue;
                }
            }
        }
        private IList<Account> _accounts;

        public decimal TotalValue { get; private set; }

        public double ExpenseRatio { get; private set; }

        // public IList<Exposure> Exposures { get; private set; }

        /// <summary>
        /// collection of all positions from all accounts
        /// </summary>
        public IEnumerable<Position> Positions => Accounts.SelectMany(x => x.Positions);

        public int NumberOfPositions => Accounts.Sum(x => x.Positions.Count);

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
            return new Portfolio { Accounts = accounts };
        }

        public double PercentOfPortfolio(AssetClass c, AssetLocation l) => 
            100 * Value(c, l) / (double)TotalValue;
        public double PercentOfPortfolio(AssetClass c) => 
            100 * Value(c) / (double)TotalValue;
        public double PercentOfPortfolio(AssetLocation l) => 
            100 * Value(l) / (double)TotalValue;

        public double PercentOfAssetType(
            AccountType t, 
            AssetClass c, 
            AssetLocation l)
        {
            var total = Value(c, l);
            return total <= 0 ? 0.0 : 100 * Value(t, c, l) / total;
        }

        public double Value(AccountType t, AssetClass c, AssetLocation l) =>  Accounts
            .Where(x => x.Type == t)
            .SelectMany(x => x.Exposures)
            .Where(x => x.Class == c && x.Location == l)
            .Sum(x => x.Value);

        public double Value(AssetClass c, AssetLocation l) => Accounts
            .SelectMany(x => x.Exposures)
            .Where(x => x.Class == c && x.Location == l)
            .Sum(x => x.Value);

        public double Value(AssetClass c) => Accounts
            .SelectMany(x => x.Exposures)
            .Where(x => x.Class == c)
            .Sum(x => x.Value);

        public double Value(AssetLocation l) => Accounts
            .SelectMany(x => x.Exposures)
            .Where(x => x.Location == l)
            .Sum(x => x.Value);

        protected string Row(params object[] values) => "|" + string.Join("|", values) + "|";

        protected string MdUrl(string anchor, string href) => $"[{anchor}]({href})";
        
        protected string SymbolUrl(string s) => MdUrl(s, $"https://finance.yahoo.com/quote/{s}?p={s}");

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

        public virtual IList<string> ToMarkdown()
        {
            double NotNan(double d){
                return double.IsNaN(d) ? 0.0 : d;
            }
            
            var lines = new List<string>
            {
                // STATS
                "## highlights",
                Row("stat", "value"),
                Row("---", "---"),
                Row("total value of assets", string.Format("${0:n2}", TotalValue)),
                Row("total expense ratio", string.Format("{0:0.0000}", NotNan(ExpenseRatio))),
                Row("morningstar xray", MdUrl(
                    "upload associated csv", 
                    "https://www.tdameritrade.com/education/tools-and-calculators/morningstar-instant-xray.page")),
                "", // new line

                // COMPOSITION
                "## position composition",
                Row("class", "location", "% total", "% class", "value"),
                Row("---", "---", "---:", "---:", "---:")
            };
            foreach (var c in Enum.GetValues(typeof(AssetClass)).Cast<AssetClass>())
            {
                var percent = PercentOfPortfolio(c);
                lines.Add(Row(
                    c.ToString().ToLower(),
                    "*",
                    string.Format("{0:0.0}%", percent),
                    string.Format("{0:0.0}%", 100),
                    string.Format("${0:n2}", TotalValue * (decimal)percent / 100)
                    ));

                foreach (var l in Enum.GetValues(typeof(AssetLocation)).Cast<AssetLocation>())
                {
                    var e = PercentOfPortfolio(c, l);
                    lines.Add(Row(
                        c.ToString().ToLower(),
                        l.ToString().ToLower(),
                        string.Format("{0:0.0}%", e),
                        string.Format("{0:0.0}%", NotNan(100 * e / PercentOfPortfolio(c))),
                        string.Format("${0:n2}", TotalValue * (decimal)e / 100)
                        ));
                }
            }
            lines.Add("");

            // ACCOUNT STATS
            if (Accounts?.Any() == true)
            {
                lines.Add("## account composition");
                lines.Add(Row("class", "location", 
                    AccountType.BROKERAGE.ToString().ToLower(), 
                    AccountType.IRA.ToString().ToLower(), 
                    AccountType.ROTH.ToString().ToLower()));
                lines.Add(Row("---", "---", "---:", "---:", "---:"));
                foreach (var c in Enum.GetValues(typeof(AssetClass)).Cast<AssetClass>())
                {
                    foreach (var l in Enum.GetValues(typeof(AssetLocation)).Cast<AssetLocation>())
                    {
                        lines.Add(Row(
                            c.ToString().ToLower(),
                            l.ToString().ToLower(),
                            string.Format("{0:0.0}%", PercentOfAssetType(AccountType.BROKERAGE, c, l)),
                            string.Format("{0:0.0}%", PercentOfAssetType(AccountType.IRA, c, l)),
                            string.Format("{0:0.0}%", PercentOfAssetType(AccountType.ROTH, c, l))
                        ));
                    }
                }
                lines.Add("");
            }

            // POSIITONS
            if (Positions?.Any() == true)
            {
                lines.Add($"## positions ({NumberOfPositions})");
                lines.Add(Row("account", "symbol", "value", "description"));
                lines.Add(Row("---", "---", "---:", "---"));
                foreach (var a in Accounts.OrderBy(x => x.Name))
                {
                    foreach (var p in a.Positions.OrderByDescending(x => x.Value))
                    {
                        var f = Fund.Get(p.Symbol);
                        lines.Add(Row(a.Name, SymbolUrl(p.Symbol), string.Format("${0:n2}", p.Value), f.Description));
                    }
                }
            }
            lines.Add("");

            return lines;
        }

        /// <summary>
        /// produce CSV file compatible with: https://www.tdameritrade.com/education/tools-and-calculators/morningstar-instant-xray.page
        /// </summary>
        public IList<string> ToXrayCsv()
        {
            var byPosition = Positions
                .GroupBy(x => x.Symbol,
                         (key, values) => new {
                            Symbol = key,
                            Value = values.Sum(x => x.Value)})
                .OrderByDescending(x => x.Value)
                .ThenBy(x => x.Symbol);

            var lines = new List<string>();
            lines.Add("Ticker,Dollar Amount");
            foreach(var g in byPosition)
                lines.Add($"{g.Symbol},{g.Value}");
            return lines;
        }
    }
}
