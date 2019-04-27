using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PortfolioPicker.App
{
    public class Portfolio
    {
        private void ComputeStats()
        {
            var totalValue = TotalValue = _positions.Sum(x => x.Value);
            var stockTotal = 0m;
            var domesticStockTotal = 0m;
            var bondTotal = 0m;
            var domesticBondTotal = 0m;
            var weightedSum = 0.0;
            foreach(var p in _positions)
            {
                var fund = Fund.Get(p.Symbol);
                stockTotal += (decimal)(fund.StockRatio * (double)p.Value);
                domesticStockTotal += (decimal)(fund.StockRatio * fund.DomesticRatio * (double)p.Value);

                bondTotal += (decimal)(fund.BondRatio * (double)p.Value);
                domesticBondTotal += (decimal)(fund.BondRatio * fund.DomesticRatio * (double)p.Value);

                weightedSum += fund.ExpenseRatio * (double)p.Value;
            }

            BondRatio = totalValue == 0
                ? 0
                : (double)(bondTotal) / (double)totalValue;
            DomesticBondRatio = totalValue == 0
                ? 0
                : bondTotal == 0? 0: (double)(domesticBondTotal) / (double)bondTotal;
            StockRatio = totalValue == 0
                ? 0
                : (double)(stockTotal) / (double)totalValue;
            DomesticStockRatio = stockTotal == 0
                ? 0
                : (double)(domesticStockTotal) / (double)stockTotal;
            ExpenseRatio = totalValue == 0
                ? 
                0: weightedSum / (double)totalValue;
        }

        public IList<Account> Accounts 
        { 
            get => _accounts;
            set 
            {
                if (value is null)
                {
                    _accounts = null;
                    _positions = null;
                }
                else
                {
                    _accounts = value.OrderBy(x => x.Name).ToList();
                    _positions = value.SelectMany(x => x.Positions).ToList();
                    ComputeStats();
                }
            } 
        }

        public string Strategy { get; set; }

        public double Score { get; set; }

        public decimal TotalValue { get; set; }

        public double ExpenseRatio { get; set; }

        // These ratios are duplicated from Strategy
        public double StockRatio { get; set; }
        public double DomesticStockRatio { get; set; }
        public double BondRatio { get; set; }
        public double DomesticBondRatio {get; set;}

        [DataMember(EmitDefaultValue = false)]
        public IList<Order> Orders { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public IList<string> Warnings { get; set; }

        [DataMember(EmitDefaultValue = false)]
        public IList<string> Errors { get; set; }

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
            var name = Strategy == null 
                ? "Custom portfolio"
                : Strategy + " portfolio";
            lines.Add("# " + name);

            // STATS
            lines.Add("## stats");
            Draw("stat", "value");
            Draw("---", "---");
            Draw("date", System.DateTime.Now.ToString("MM/dd/yyyy"));
            Draw(nameof(TotalValue), string.Format("{0:c}", TotalValue));
            Draw(nameof(ExpenseRatio), string.Format("{0:0.0000}", ExpenseRatio));
            Draw(nameof(StockRatio), string.Format("{0:0.00}", StockRatio));
            Draw(nameof(BondRatio), string.Format("{0:0.00}", BondRatio));
            lines.Add("");

            lines.Add("## composition");
            Draw("class", "location", "percentage");
            Draw("---", "---", "---:");
            Draw("stock", "domestic", 
                string.Format("{0:0}%", 100.0 * DomesticStockRatio * StockRatio));
            Draw("stock", "international", 
                string.Format("{0:0}%", 100.0 * (1.0 - DomesticStockRatio) * StockRatio));
            Draw("bonds", "domestic", 
                string.Format("{0:0}%", 100.0 * DomesticBondRatio * BondRatio));
            Draw("bonds", "international", 
                string.Format("{0:0}%", 100.0*(1.0 - DomesticBondRatio) * BondRatio));
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

        [IgnoreDataMember]
        private IList<Account> _accounts;

        [IgnoreDataMember]
        private IList<Position> _positions;

        [IgnoreDataMember]
        public IList<Position> Positions => _positions;

        [IgnoreDataMember]
        public int NumberOfPositions => _positions.Count;
    }
}
