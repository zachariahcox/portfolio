using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace PortfolioPicker
{
    public class FourFund : Strategy
    {
        // accounts prefer funds sponsored by their brokerage
        // roth accounts should prioritize stocks over bonds
        // taxable accounts should prioritize international assets over domestic
        // 401k accounts should hold as many bonds as possible and avoid international assets

        //
        // basic ratios: 
        //
        readonly static decimal stocks_ratio = 0.9m;
        readonly static decimal bonds_ratio = 1m - stocks_ratio;
        readonly static decimal international_stocks_ratio = 0.4m;
        readonly static decimal international_bonds_ratio = 0.3m;

        public override IReadOnlyList<Order> Perform()
        {
            // 
            // aggregate: 
            // for this, we don't actually care how much is in any particular account
            //
            var total_value = 0m;
            var total_roth = 0m;
            var total_taxable = 0m;
            Data.GetAccounts().AsParallel().ForAll((a) =>
            {
                total_value += a.value;

                if (a.type == AccountType.ROTH)
                    total_roth += a.value;

                if (a.taxable)
                    total_taxable += a.value;
            });

            // compute basic ratios
            var stock_t = total_value * stocks_ratio;
            var stock_d = stock_t * (1m - international_stocks_ratio);
            var stock_i = stock_t * international_stocks_ratio;
            var bonds_t = total_value * bonds_ratio;
            var bonds_d = bonds_t * (1m - international_bonds_ratio);
            var bonds_i = bonds_t * international_bonds_ratio;

            //
            // pick some funds
            // 
            Fund find_fund(bool domestic, bool stock)
            {
                var f = Data.GetFunds()
                .Where(x => x.domestic == domestic && x.stock == stock)
                .Aggregate((l, r) => l.expense_ratio < r.expense_ratio ? l : r);
                Debug.Assert(f != null);
                return f;
            }
            var stock_d_fund = find_fund(domestic: true, stock: true);
            var stock_i_fund = find_fund(domestic: false, stock: true);
            var bonds_d_fund = find_fund(domestic: true, stock: false);
            var bonds_i_fund = find_fund(domestic: false, stock: false);


            //
            // compose buy orders
            //
            var rc = new List<Order>
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
