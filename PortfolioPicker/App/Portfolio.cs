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

        public double Score { get; set; }

        public decimal TotalValue { get; set; }

        public double ExpenseRatio { get; set; }

        public ExposureRatios ExposureRatios {get; set; }

        public IList<Order> Orders { get; set; }

        public IList<string> Warnings { get; set; }

        public IList<string> Errors { get; set; }

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

        public IList<string> ToMarkdownLines()
        {
            var lines = new List<string>();
            void Draw(params object[] values)
            {
                lines.Add("|" + string.Join("|", values) + "|");
            }

            string Url(string _s)
            {
                return $"[{_s}](https://finance.yahoo.com/quote/{_s}?p={_s})";
            }

            // TITLE
            lines.Add("# portfolio");

            // STATS
            lines.Add("## stats");
            Draw("stat", "value");
            Draw("---", "---");
            Draw("date", System.DateTime.Now.ToString("MM/dd/yyyy"));
            Draw("total value of assets", string.Format("{0:c}", TotalValue));
            Draw("expense ratio", string.Format("{0:0.0000}", ExpenseRatio));
            Draw("percent stocks", string.Format("{0:0.0}%", 100.0 * ExposureRatios.StockRatio));
            Draw("percent bonds", string.Format("{0:0.0}%", 100.0 * ExposureRatios.BondRatio));
            lines.Add("");

            // COMPOSITION
            lines.Add("## composition");
            Draw("class", "location", "percentage");
            Draw("---", "---", "---:");
            Draw("stock", "domestic", 
                string.Format("{0:0}%", 100.0 * ExposureRatios.DomesticStockRatio * ExposureRatios.StockRatio));
            Draw("stock", "international", 
                string.Format("{0:0}%", 100.0 * (1.0 - ExposureRatios.DomesticStockRatio) * ExposureRatios.StockRatio));
            Draw("bonds", "domestic", 
                string.Format("{0:0}%", 100.0 * ExposureRatios.DomesticBondRatio * ExposureRatios.BondRatio));
            Draw("bonds", "international", 
                string.Format("{0:0}%", 100.0*(1.0 - ExposureRatios.DomesticBondRatio) * ExposureRatios.BondRatio));
            lines.Add("");

            // POSIITONS
            if (Positions?.Any() == true)
            {
                lines.Add("## positions");
                Draw("account", "symbol", "value");
                Draw("---", "---", "---:");
                foreach (var a in Accounts.OrderBy(x => x.Name))
                {
                    foreach (var p in a.Positions)
                    {
                        Draw(a.Name, Url(p.Symbol), string.Format("{0:c}", p.Value));
                    }
                }
            }

            // ORDERS
            if (Orders?.Any() == true)
            {
                lines.Add("## orders");
                Draw("account", "action", "symbol", "value");
                Draw("---", "---", "---", "---:");
                foreach(var o in Orders
                    .OrderBy(x => x.AccountName)
                    .ThenBy(x => x.Action)
                    .ThenBy(x => x.Symbol))
                {
                    Draw(o.AccountName, o.Action, Url(o.Symbol), string.Format("{0:c}", o.Value));
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

            ExposureRatios = new ExposureRatios
            {
                StockRatio = totalValue == 0
                    ? 0
                    : (double)stockTotal / (double)totalValue,
                DomesticStockRatio = stockTotal == 0
                    ? 0
                    : (double)domesticStockTotal / (double)stockTotal,
                DomesticBondRatio = bondTotal == 0
                    ? 0
                    : (double)domesticBondTotal / (double)bondTotal,
            };
        }
    }

    public class ExposureRatios
    {
        public double StockRatio { get; set; }

        public double DomesticStockRatio { get; set; }

        public double DomesticBondRatio {get; set; }

        public double BondRatio => Math.Round(1.0 - StockRatio, 5);

        public double InternationalStockRatio => Math.Round(1.0 - DomesticStockRatio, 5);

        public double InternationalBondRatio => Math.Round(1.0 - DomesticBondRatio, 5);
    }
}
