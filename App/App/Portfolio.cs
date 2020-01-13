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
                    Positions = null;
                }
                else
                {
                    _accounts = value.OrderBy(x => x.Name).ToList();
                    Positions = value.SelectMany(x => x.Positions).ToList();
                    ComputeStats();
                }
            }
        }
        private IList<Account> _accounts;

        public decimal TotalValue { get; private set; }

        public double ExpenseRatio { get; private set; }

        public IList<Exposure> Exposures { get; private set; }

        public IDictionary<AccountType, IList<Exposure>> ExposuresByAccountType {get; private set;}

        /// <summary>
        /// collection of all positions from all accounts
        /// </summary>
        public IList<Position> Positions { get; private set; }

        public int NumberOfPositions => Positions.Count;

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


        protected string Row(params object[] values)
        {
            return "|" + string.Join("|", values) + "|";
        }

        protected string Url(string _s)
        {
            return $"[{_s}](https://finance.yahoo.com/quote/{_s}?p={_s})";
        }

        public virtual IList<string> ToMarkdown()
        {
            double NotNan(double d){
                if (double.IsNaN(d))
                    return 0.0;
                return d;
            }
            var lines = new List<string>
            {
                // STATS
                "## highlights",
                Row("stat", "value"),
                Row("---", "---"),
                Row("total value of assets", string.Format("${0:n2}", TotalValue)),
                Row("total expense ratio", string.Format("{0:0.0000}", NotNan(ExpenseRatio))),
                Row("percent of stocks are domestic", string.Format("{0:0.0}%", NotNan(100 * Exposures.Percent(AssetClass.Stock, AssetLocation.Domestic) / Exposures.Percent(AssetClass.Stock)))),
                Row("percent of bonds are domestic", string.Format("{0:0.0}%", NotNan(100 * Exposures.Percent(AssetClass.Bond, AssetLocation.Domestic) / Exposures.Percent(AssetClass.Bond)))),
                "", // new line

                // COMPOSITION
                "## position composition",
                Row("class", "location", "percentage", "value"),
                Row("---", "---", "---:", "---:")
            };
            foreach (var c in Enum.GetValues(typeof(AssetClass)).Cast<AssetClass>())
            {
                var percent = Exposures.Percent(c);
                lines.Add(Row(
                    c.ToString().ToLower(),
                    "*",
                    string.Format("{0:0.0}%", percent),
                    string.Format("${0:n2}", TotalValue * (decimal)percent / 100)
                    ));

                foreach (var l in Enum.GetValues(typeof(AssetLocation)).Cast<AssetLocation>())
                {
                    var e = Exposures.Percent(c, l);
                    lines.Add(Row(
                        c.ToString().ToLower(),
                        l.ToString().ToLower(),
                        string.Format("{0:0.0}%", e),
                        string.Format("${0:n2}", TotalValue * (decimal)e / 100)
                        ));
                }
            }
            lines.Add("");

            // ACCOUNT STATS
            lines.Add("## account composition");
            lines.Add(Row("class", "location", 
                AccountType.BROKERAGE.ToString().ToLower(), 
                AccountType.IRA.ToString().ToLower(), 
                AccountType.ROTH.ToString().ToLower()));
            lines.Add(Row("---", "---", "---:", "---:", "---:"));
            
            string GetValue(AccountType _t, AssetClass c, AssetLocation l)
            {
                var percent = 0m;
                if (ExposuresByAccountType.TryGetValue(_t, out var exposures))
                {
                    var e = exposures.First(x => x.Class == c && x.Location == l);
                    percent = 100 * (decimal)e.Value / TotalValue;
                }
                return string.Format("{0:0.0}%", percent);
            }
            foreach (var c in Enum.GetValues(typeof(AssetClass)).Cast<AssetClass>())
            {
                foreach (var l in Enum.GetValues(typeof(AssetLocation)).Cast<AssetLocation>())
                {
                    lines.Add(Row(
                        c.ToString().ToLower(),
                        l.ToString().ToLower(),
                        GetValue(AccountType.BROKERAGE, c, l),
                        GetValue(AccountType.IRA, c, l),
                        GetValue(AccountType.ROTH, c, l)
                    ));
                }
            }
            lines.Add("");

            // POSIITONS
            if (Positions?.Any() == true)
            {
                lines.Add("## positions");
                lines.Add(Row("account", "symbol", "value", "description"));
                lines.Add(Row("---", "---", "---:", "---"));
                foreach (var a in Accounts.OrderBy(x => x.Name))
                {
                    foreach (var p in a.Positions.OrderByDescending(x => x.Value))
                    {
                        var f = Fund.Get(p.Symbol);
                        lines.Add(Row(a.Name, Url(p.Symbol), string.Format("${0:n2}", p.Value), f.Description));
                    }
                }
            }

            return lines;
        }

        /// <summary>
        /// automatically called when `Accounts` is set.
        /// </summary>
        private void ComputeStats()
        {
            var totalValue = TotalValue = Positions.Sum(x => x.Value);
            var stockTotal = 0m;
            var domesticStockTotal = 0m;
            var bondTotal = 0m;
            var domesticBondTotal = 0m;
            var erWeightedSum = 0.0;

            // portfolio stats
            foreach (var p in Positions)
            {
                var fund = Fund.Get(p.Symbol);
                stockTotal += (decimal)(fund.StockRatio * (double)p.Value);
                domesticStockTotal += (decimal)(fund.StockRatio * fund.DomesticRatio * (double)p.Value);

                bondTotal += (decimal)(fund.BondRatio * (double)p.Value);
                domesticBondTotal += (decimal)(fund.BondRatio * fund.DomesticRatio * (double)p.Value);

                erWeightedSum += fund.ExpenseRatio * (double)p.Value;
            }
            ExpenseRatio = totalValue == 0 ? 0 : erWeightedSum / (double)totalValue;

            Exposures = new List<Exposure>
            {
                new Exposure(
                    AssetClass.Stock,
                    AssetLocation.Domestic,
                    (double)domesticStockTotal / (double)totalValue),
                new Exposure(
                    AssetClass.Stock,
                    AssetLocation.International,
                    (double)(stockTotal - domesticStockTotal) / (double)totalValue),
                new Exposure(
                    AssetClass.Bond,
                    AssetLocation.Domestic,
                    (double)domesticBondTotal / (double)totalValue),
                new Exposure(
                    AssetClass.Bond,
                    AssetLocation.International,
                    (double)(bondTotal - domesticBondTotal) / (double)totalValue),
            };

            // account stats
            var ad = new Dictionary<AccountType, IList<Exposure>>();
            foreach (var a in Accounts)
            {
                // get current exposures for this account type
                if (!ad.TryGetValue(a.Type, out var accountTypeExposures))
                {
                    accountTypeExposures = new List<Exposure>
                    {
                        new Exposure(AssetClass.Stock, AssetLocation.Domestic),
                        new Exposure(AssetClass.Stock, AssetLocation.International),
                        new Exposure(AssetClass.Bond, AssetLocation.Domestic),
                        new Exposure(AssetClass.Bond, AssetLocation.International)
                    };
                    ad.Add(a.Type, accountTypeExposures);
                }

                // calculate each position's contributions to various exposures of interest
                foreach (var p in a.Positions)
                {
                    var fund = Fund.Get(p.Symbol);
                    foreach (var c in Enum.GetValues(typeof(AssetClass)).Cast<AssetClass>())
                    {
                        foreach (var l in Enum.GetValues(typeof(AssetLocation)).Cast<AssetLocation>())
                        {
                            var e = accountTypeExposures.First(x => x.Class == c && x.Location == l);
                            e.Value += (double)p.Value * fund.Ratio(c) * fund.Ratio(l);
                        }
                    }
                }
            }
            ExposuresByAccountType = ad;
        }
    }
}
