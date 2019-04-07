using System.Collections.Generic;
using System.Linq;

namespace PortfolioPicker.App
{
    public class Portfolio
    {
        public string Strategy { get; set; }

        public IReadOnlyList<Order> BuyOrders { get; set; }

        public IList<string> Warnings { get; set; }

        public IList<string> Errors { get; set; }

        public double Score { get; set; }

        // Stats
        public decimal TotalValue { get; set; }

        public double ExpenseRatio { get; set; } = 0.0;

        public double BondRatio { get; set; } = 0.0;

        public double StockRatio { get; set; } = 0.0;

        public double DomesticRatio { get; set; }

        public double InternationalRatio { get; set; }

        public IList<string> ToMarkdownLines()
        {
            var lines = new List<string>();
            void Draw(params object[] values)
            {
                lines.Add("|" + string.Join("|", values) + "|");
            }
            Draw("stat", "value");
            Draw("---", "---");
            Draw(nameof(TotalValue), string.Format("{0:c}", TotalValue));
            Draw(nameof(ExpenseRatio), ExpenseRatio);
            Draw(nameof(BondRatio), string.Format("{0:0.00}", BondRatio));
            Draw(nameof(StockRatio), string.Format("{0:0.00}", StockRatio));
            Draw(nameof(DomesticRatio), string.Format("{0:0.00}", DomesticRatio));
            Draw(nameof(InternationalRatio), string.Format("{0:0.00}", InternationalRatio));
            Draw(nameof(Strategy), Strategy);
            lines.Add("");

            Draw("account", "fund", "value");
            Draw("---", "---", "---");
            foreach(var o in BuyOrders
                .OrderBy(x => x.Account.Name)
                .ThenBy(x => x.Fund.Symbol)
                .ThenBy(x => x.Value))
            {
                Draw(o.Account.Name, o.Fund.Symbol, string.Format("{0:c}", o.Value));
            }
            return lines;
        }
    }
}
