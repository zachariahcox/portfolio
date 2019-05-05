using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
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

        public decimal TotalValue { get; set; }

        public double ExpenseRatio { get; set; }

        public IList<ExposureRatio> ExposureRatios {get; set; }

        public IList<Position> Positions { get; set; }

        public int NumberOfPositions => Positions.Count;

        public string ToYaml()
        {
            var serializer = new SerializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();
            return serializer.Serialize(this.Accounts);
        }

        public static Portfolio FromYaml(string yaml)
        {
            if (string.IsNullOrEmpty(yaml))
            {
                return null;
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            return new Portfolio 
            { 
                Accounts = deserializer.Deserialize<IList<Account>>(yaml)
            };
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
            var lines = new List<string>();

            // TITLE
            lines.Add($"# portfolio");

            // STATS
            lines.Add("## stats");
            lines.Add(Row("stat", "value"));
            lines.Add(Row("---", "---"));
            lines.Add(Row("total value of assets", string.Format("{0:c}", TotalValue)));
            lines.Add(Row("expense ratio", string.Format("{0:0.0000}", ExpenseRatio)));
            lines.Add(Row("percent stocks", string.Format("{0:0.0}%", ExposureRatios.Percent(AssetClass.Stock))));
            lines.Add(Row("percent bonds", string.Format("{0:0.0}%", ExposureRatios.Percent(AssetClass.Bond))));
            lines.Add("");

            // COMPOSITION
            lines.Add("## composition");
            lines.Add(Row( "class", "location", "percentage"));
            lines.Add(Row( "---", "---", "---:"));
            foreach(var er in ExposureRatios)
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
                        lines.Add(Row(a.Name, Url(p.Symbol), string.Format("{0:c}", p.Value, f.Description)));
                    }
                }
            }
            
            return lines;
        }

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
                ?
                0 : weightedSum / (double)totalValue;

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
