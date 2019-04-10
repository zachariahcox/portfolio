using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PortfolioPicker.App
{
    public class Portfolio
    {
        private IList<Account> _accounts;
        private IList<PositionReference> _positions;

        public IList<Account> Accounts 
        { 
            get => _accounts;
            set 
            {
                _accounts = value;
                if (value != null)
                {
                    _positions = value.SelectMany(x => x.Positions).ToList();
                }
                else
                {
                    _positions = null;
                }
            } 
        }

        public IList<PositionReference> Positions => _positions;
        
        public int NumberOfPositions => _positions.Count;

        public string Strategy { get; set; }

        public IList<string> Warnings { get; set; }

        public IList<string> Errors { get; set; }

        public double Score { get; set; }

        public decimal TotalValue { get; set; }

        public double ExpenseRatio { get; set; } = 0.0;

        public double BondRatio { get; set; } = 0.0;

        public double StockRatio { get; set; } = 0.0;

        public double DomesticRatio { get; set; }

        public double InternationalRatio { get; set; }

        // SERIALIZATION

        public IList<string> ToMarkdownLines()
        {
            var lines = new List<string>();
            void Draw(params object[] values)
            {
                lines.Add("|" + string.Join("|", values) + "|");
            }

            lines.Add("# portfolio");
            lines.Add("## stats");
            Draw("stat", "value");
            Draw("---", "---");
            Draw("date", System.DateTime.Now.ToString("MM / dd / yyyy"));
            Draw(nameof(TotalValue), string.Format("{0:c}", TotalValue));
            Draw(nameof(ExpenseRatio), string.Format("{0:0.0000}", ExpenseRatio));
            Draw(nameof(BondRatio), string.Format("{0:0.00}", BondRatio));
            Draw(nameof(StockRatio), string.Format("{0:0.00}", StockRatio));
            Draw(nameof(DomesticRatio), string.Format("{0:0.00}", DomesticRatio));
            Draw(nameof(InternationalRatio), string.Format("{0:0.00}", InternationalRatio));
            Draw(nameof(Strategy), Strategy);
            lines.Add("");

            lines.Add("## positions");
            Draw("account", "fund", "value");
            Draw("---", "---", "---");
            foreach(var a in Accounts.OrderBy(x => x.Name))
            {
                var name = a.Name;
                foreach(var p in a.Positions)
                {
                    Draw(name, p.Symbol, string.Format("{0:c}", p.Value));
                }
            }
            return lines;
        }

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
    }
}
