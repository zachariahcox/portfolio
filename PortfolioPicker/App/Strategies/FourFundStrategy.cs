using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioPicker.App.Strategies
{
    public class FourFundStrategy : Strategy
    {
        // Strategy: 
        // * accounts prefer funds sponsored by their brokerage
        // * roth accounts should prioritize stocks over bonds
        // * taxable accounts should prioritize international assets over domestic
        // * 401k accounts should prioritize bonds and avoid international assets
        //
        // dom stocks -> roth, tax, 401k
        // int stocks -> tax, roth, 401k
        // dom bonds  -> 401k, roth, tax
        // int bonds  -> tax, 401k, roth



        // basic ratios: 
        private static readonly decimal stocks_ratio = 0.9m;
        private static readonly decimal bonds_ratio = 1m - stocks_ratio;
        private static readonly decimal international_stocks_ratio = 0.4m;
        private static readonly decimal international_bonds_ratio = 0.3m;

        public override Portfolio Perform(
            IReadOnlyCollection<Account> accounts,
            IReadOnlyDictionary<string, IReadOnlyList<Fund>> funds)
        {
            // compute just the totals of everything 
            // for this, we don't actually care how much is in any particular account
            var total_value = 0m;
            var total_roth = 0m;
            var total_taxable = 0m;
            accounts.AsParallel().ForAll((a) =>
            {
                total_value += a.Value;

                if (a.AccountType == AccountType.ROTH)
                    total_roth += a.Value;

                if (a.Taxable)
                    total_taxable += a.Value;

                a.ResolveFunds(funds);
            });

            // compute overall exposures we want to acheive
            var stock_total = total_value * stocks_ratio;
            var stock_domestic = stock_total * (1m - international_stocks_ratio);
            var stock_international = stock_total * international_stocks_ratio;
            var bonds_total = total_value * bonds_ratio;
            var bonds_domestic = bonds_total * (1m - international_bonds_ratio);
            var bonds_international = bonds_total * international_bonds_ratio;
            var exposures = new List<Exposure>
            {
                new Exposure(
                    AssetClass.Stock, 
                    AssetLocation.Domestic, 
                    stock_domestic,
                    new[]{AccountType.ROTH,  AccountType.TAXABLE, AccountType.CORPORATE}),
                new Exposure(
                    AssetClass.Stock, 
                    AssetLocation.International, 
                    stock_international,
                    new[]{AccountType.TAXABLE,  AccountType.ROTH, AccountType.CORPORATE}),
                new Exposure(
                    AssetClass.Bond, 
                    AssetLocation.Domestic, 
                    bonds_domestic,
                    new[]{AccountType.CORPORATE,  AccountType.ROTH, AccountType.TAXABLE}),
                new Exposure(
                    AssetClass.Bond, 
                    AssetLocation.International, 
                    bonds_international,
                    new[]{AccountType.TAXABLE,  AccountType.CORPORATE, AccountType.ROTH}),
            };

            // PRODUCE BUY ORDERS
            var orders = new List<Order>();
            foreach (var e in exposures)
            {
                var ec = e.Class;
                var el = e.Location;

                // find accounts with access to the right funds
                var suitableAccounts = accounts.Where(a => a.GetFund(ec, el) != null).ToList();
                if (suitableAccounts.Count == 0)
                {
                    var ecName = Enum.GetName(ec.GetType(), ec);
                    var elName = Enum.GetName(el.GetType(), el);
                    var m = $"Error: Cannot execute strategy: no account has access to asset type: {ecName}, {elName}";
                    throw new Exception(m);
                }

                // buy as much as possible from prefered accounts, in order
                var remainder = e.Value;
                foreach(var t in e.AccountTypesPreference)
                {
                    foreach(var a in accounts.Where(a => a.AccountType == t))
                    {
                        var f = a.GetFund(ec, el);
                        var purchaseValue = Math.Min(a.Value, remainder);
                        if(purchaseValue > 0)
                        {
                            orders.Add(new Order
                            {
                                Account = a,
                                Value = purchaseValue,
                                Fund = f
                            });

                            // update remainders and balances
                            a.Value -= purchaseValue;
                            remainder -= purchaseValue;
                            if (remainder <= 0)
                            {
                                break; // stop looking through accounts
                            }
                        }
                    }

                    if (remainder <= 0)
                    {
                        break; // stop looking through account types
                    }
                }

                // ensure that we allocated all funds
                if (remainder > 0)
                {
                    var ecName = Enum.GetName(ec.GetType(), ec);
                    var elName = Enum.GetName(el.GetType(), el);
                    var m = $"Error: could not spend all money alloted to asset type: {ecName}, {elName}";
                    throw new Exception(m);
                }
            }

            // compute total er
            var weighted_sum = orders.Sum(x => x.Fund?.ExpenseRatio * (double)x.Value);
            var weight = orders.Sum(x => (double)x.Value);
            var er = weighted_sum.Value / weight;

            // return
            return new Portfolio
            {
                BuyOrders = orders,
                ExpenseRatio = er
            };
        }
    }
}
