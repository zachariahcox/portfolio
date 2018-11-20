using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioPicker.Strategies
{
    internal class DataRow
    {
        public AccountType type;
        public string brokerage;
        public IList<Fund> funds;
    }
    public class FourFundStrategy : Strategy
    {
        // Strategy: 
        // accounts prefer funds sponsored by their brokerage
        // roth accounts should prioritize stocks over bonds
        // taxable accounts should prioritize international assets over domestic
        // 401k accounts should hold as many bonds as possible and avoid international assets

        // basic ratios: 
        private static readonly decimal stocks_ratio = 0.9m;
        private static readonly decimal bonds_ratio = 1m - stocks_ratio;
        private static readonly decimal international_stocks_ratio = 0.4m;
        private static readonly decimal international_bonds_ratio = 0.3m;

        public override Portfolio Perform(
            IReadOnlyList<Account> accounts,
            IReadOnlyDictionary<string, IReadOnlyCollection<Fund>> funds)
        {
            // compute just the totals of everything 
            // for this, we don't actually care how much is in any particular account
            var total_value = 0m;
            var total_roth = 0m;
            var total_taxable = 0m;



            accounts.AsParallel().ForAll((a) =>
            {
                total_value += a.value;

                if (a.type == AccountType.ROTH)
                    total_roth += a.value;

                if (a.taxable)
                    total_taxable += a.value;
            });

            // compute overall exposures we want to acheive
            var stock_total = total_value * stocks_ratio;
            var stock_domestic = stock_total * (1m - international_stocks_ratio);
            var stock_international = stock_total * international_stocks_ratio;
            var bonds_total = total_value * bonds_ratio;
            var bonds_domestic = bonds_total * (1m - international_bonds_ratio);
            var bonds_international = bonds_total * international_bonds_ratio;
            var exposures = new Dictionary<bool, Dictionary<bool, decimal>>
            {
                { true, new Dictionary<bool, decimal>
                    {
                        {false, stock_international},
                        {true, stock_domestic },
                    } },
                {false, new Dictionary<bool, decimal>
                {
                    {false, bonds_international},
                    {true, bonds_domestic },
                } }
            };
            // pick funds
            Fund find_fund(
                    string brokerage,
                    bool domestic,
                    bool stock)
            {
                // return the fund that meets the criteria with the lowest expense ratio
                if (funds.TryGetValue(brokerage, out var brokerageFunds))
                {
                    return brokerageFunds
                            .Where(x => x.domestic == domestic && x.stock == stock)
                            .OrderBy(x => x.expense_ratio)
                            .FirstOrDefault();
                }
                return null;
            }


            decimal ComputeValue(
                Account a,
                bool domestic,
                bool stock)
            {
                if (domestic)
                {
                    if (stock)
                    {
                        stock_domestic -= a.value;
                        return stock_domestic;
                    }
                    else
                    {
                        bonds_domestic -= a.value;
                        return bonds_domestic;
                    }
                }
                else
                {
                    if (stock)
                    {
                        stock_international -= a.value;
                        return stock_international;
                    }
                    else
                    {
                        bonds_international -= a.value;
                        return bonds_international;
                    }
                }
            }


            var buy_orders = new List<Order>();
            foreach (var a in accounts)
            {
                foreach (var domestic in new[] { true, false })
                {
                    foreach (var stock in new[] { true, false })
                    {
                        var fund = find_fund(brokerage: a.brokerage, domestic: domestic, stock: stock);
                        if (fund != null)
                        {
                            buy_orders.Add(new Order
                            {
                                account = a,
                                fund = fund,
                                value = ComputeValue(a, domestic, stock),
                            });
                        }
                    }
                }
            }


            decimal buy(ref decimal account_balance, decimal allotment)
            {
                var purchase_value = Math.Min(account_balance, allotment);
                account_balance -= purchase_value;
                allotment -= purchase_value;
                return purchase_value;
            }
            foreach (var a in accounts)
            {
                switch (a.type)
                {
                    case AccountType.CORPORATE:
                        foreach (var stock in new[] { false, true })
                            foreach (var domestic in new[] { true, false })
                            {
                                var b = buy(ref a.value, exposures[stock][domestic]);
                                if (b > 0)
                                {
                                    var f = find_fund(a.brokerage, domestic, stock);
                                    buy_orders.Add(new Order
                                    {
                                        account = a,
                                        value = b,
                                        fund = f
                                    });
                                }
                            }
                        
                        break;
                    case AccountType.ROTH:
                        break;
                    case AccountType.INVESTMENT:
                        break;
                }
            }

            // compute total er
            var weighted_sum = buy_orders.Sum(x => x.fund.expense_ratio * (double)x.value);
            var weight = buy_orders.Sum(x => (double)x.value);
            var er = weighted_sum / weight;

            // return
            return new Portfolio
            {
                buy_orders = buy_orders,
                total_expense_ratio = er
            };
        }
    }
}
