using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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

        public IList<ExposureRatio> ExposureRatios { get; private set; }

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
            var lines = new List<string>
            {
                // TITLE
                $"# portfolio",

                // STATS
                "## stats",
                Row("stat", "value"),
                Row("---", "---"),
                Row("total value of assets", string.Format("${0:n2}", TotalValue)),
                Row("total expense ratio", string.Format("{0:0.0000}", ExpenseRatio)),
                Row("percent stocks", string.Format("{0:0.0}%", ExposureRatios.Percent(AssetClass.Stock))),
                Row("percent bonds", string.Format("{0:0.0}%", ExposureRatios.Percent(AssetClass.Bond))),
                "",

                // COMPOSITION
                "## composition (percent of total in various buckets)",
                Row("class", "location", "percentage"),
                Row("---", "---", "---:")
            };
            foreach (var er in ExposureRatios)
            {
                lines.Add(Row(
                    er.Class.ToString().ToLower(),
                    er.Location.ToString().ToLower(),
                    string.Format("{0:0}%", 100 * er.Ratio)));
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
            var weightedSum = 0.0;
            foreach (var p in Positions)
            {
                var fund = Fund.Get(p.Symbol);
                stockTotal += (decimal)(fund.StockRatio * (double)p.Value);
                domesticStockTotal += (decimal)(fund.StockRatio * fund.DomesticRatio * (double)p.Value);

                bondTotal += (decimal)(fund.BondRatio * (double)p.Value);
                domesticBondTotal += (decimal)(fund.BondRatio * fund.DomesticRatio * (double)p.Value);

                weightedSum += fund.ExpenseRatio * (double)p.Value;
            }

            ExpenseRatio = totalValue == 0
                ? 0 
                : weightedSum / (double)totalValue;

            ExposureRatios = new List<ExposureRatio>
            {
                new ExposureRatio(
                    AssetClass.Stock,
                    AssetLocation.Domestic,
                    (double)domesticStockTotal / (double)totalValue),
                new ExposureRatio(
                    AssetClass.Stock,
                    AssetLocation.International,
                    (double)(stockTotal - domesticStockTotal) / (double)totalValue),
                new ExposureRatio(
                    AssetClass.Bond,
                    AssetLocation.Domestic,
                    (double)domesticBondTotal / (double)totalValue),
                new ExposureRatio(
                    AssetClass.Bond,
                    AssetLocation.International,
                    (double)(bondTotal - domesticBondTotal) / (double)totalValue),
            };
        }
    }
}
