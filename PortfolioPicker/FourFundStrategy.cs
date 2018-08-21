using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PortfolioPicker
{
    public class FourFundStrategy : Strategy
    {
        //
        // Strategy: 
        // accounts prefer funds sponsored by their brokerage
        // roth accounts should prioritize stocks over bonds
        // taxable accounts should prioritize international assets over domestic
        // 401k accounts should hold as many bonds as possible and avoid international assets
        //

        //
        // basic ratios: 
        //
        private static readonly decimal stocks_ratio = 0.9m;
        private static readonly decimal bonds_ratio = 1m - stocks_ratio;
        private static readonly decimal international_stocks_ratio = 0.4m;
        private static readonly decimal international_bonds_ratio = 0.3m;
        private readonly IReadOnlyList<Fund> default_funds;
        public FourFundStrategy()
        {
            default_funds = new List<Fund>
            {
                new Fund{
                    description="Vanguard Total Stock Market Index Fund",
                    symbol ="VTSAX",
                    url= "https://investor.vanguard.com/mutual-funds/profile/VTSAX",
                    expense_ratio= 0.04,
                    stock= true,
                    exposure="total",
                    domestic= true
                },
                new Fund{
                    description="Vanguard Total International Stock Index Fund",
                    symbol="VTIAX",
                    url="https://investor.vanguard.com/mutual-funds/profile/VTIAX",
                    expense_ratio=0.11,
                    stock=true,
                    exposure="total",
                    domestic=false
                },
               new Fund{
                   description="Vanguard Total Bond Market Index Fund",
                    symbol="VBTLX",
                    url="https://investor.vanguard.com/mutual-funds/profile/VBTLX",
                    expense_ratio=0.05,
                    stock=false,
                    exposure="total",
                    domestic=true},
                new Fund{ description="Vanguard Total International Bond Index Fund",
                symbol="VTABX",
                url="https://investor.vanguard.com/mutual-funds/profile/VTABX",
                expense_ratio=0.11,
                stock=false,
                exposure="total",
                domestic=false},
            };
        }


        public override IReadOnlyList<Order> Perform(IReadOnlyList<Account> accounts)
        {
            // 
            // aggregate: 
            // for this, we don't actually care how much is in any particular account
            //
            decimal total_value = 0m;
            decimal total_roth = 0m;
            decimal total_taxable = 0m;
            accounts.AsParallel().ForAll((a) =>
            {
                total_value += a.value;

                if (a.type == AccountType.ROTH)
                    total_roth += a.value;

                if (a.taxable)
                    total_taxable += a.value;
            });

            // compute basic ratios
            decimal stock_t = total_value * stocks_ratio;
            decimal stock_d = stock_t * (1m - international_stocks_ratio);
            decimal stock_i = stock_t * international_stocks_ratio;
            decimal bonds_t = total_value * bonds_ratio;
            decimal bonds_d = bonds_t * (1m - international_bonds_ratio);
            decimal bonds_i = bonds_t * international_bonds_ratio;

            //
            // pick funds
            // 
            Fund find_fund(bool domestic, bool stock)
            {
                Fund f = default_funds
                .Where(x => x.domestic == domestic && x.stock == stock)
                .Aggregate((l, r) => l.expense_ratio < r.expense_ratio ? l : r);
                Debug.Assert(f != null);
                return f;
            }
            Fund stock_d_fund = find_fund(domestic: true, stock: true);
            Fund stock_i_fund = find_fund(domestic: false, stock: true);
            Fund bonds_d_fund = find_fund(domestic: true, stock: false);
            Fund bonds_i_fund = find_fund(domestic: false, stock: false);

            //
            // compose buy orders
            //
            List<Order> rc = new List<Order>
            {
                // domestic stock
                new Order(fund: stock_d_fund, value: stock_d),
                new Order(fund: stock_i_fund, value: stock_i),
                new Order(fund: bonds_d_fund, value: bonds_d),
                new Order(fund:  bonds_i_fund, value: bonds_i),
            };
            return rc;
        }
    }
}
