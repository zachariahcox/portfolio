using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioPicker.App.Strategies
{
    /// <summary>
    ///  Strategy: 
    ///  * accounts prefer funds sponsored by their brokerage
    ///  * roth accounts should prioritize stocks over bonds
    ///  * taxable accounts should prioritize international assets over domestic
    ///  * 401k accounts should prioritize bonds and avoid international assets
    ///  dom stocks -> roth, tax, 401k
    ///  int stocks -> tax, roth, 401k
    ///  dom bonds  -> 401k, roth, tax
    ///  int bonds  -> tax, 401k, roth
    /// </summary>
    public class FourFundStrategy : Strategy
    {
        // basic ratios: 
        private static readonly decimal stocks_ratio = 0.9m;
        private static readonly decimal bonds_ratio = 1m - stocks_ratio;
        private static readonly decimal international_stocks_ratio = 0.4m;
        private static readonly decimal international_bonds_ratio = 0.3m;

        /// <summary>
        /// Produce desired exposures based on ratios and total available money
        /// </summary>
        private static IList<Exposure> ComputeExposures(decimal totalValue)
        {
            var stock_total = totalValue * stocks_ratio;
            var stock_domestic = stock_total * (1m - international_stocks_ratio);
            var stock_international = stock_total * international_stocks_ratio;
            var bonds_total = totalValue * bonds_ratio;
            var bonds_domestic = bonds_total * (1m - international_bonds_ratio);
            var bonds_international = bonds_total * international_bonds_ratio;
            return new List<Exposure>
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
        }

        private static ICollection<ICollection<T>> Permutations<T>(ICollection<T> list)
        {
            var result = new List<ICollection<T>>();

            // If only one possible permutation, add it and return it
            if (list.Count == 1)
            { 
                result.Add(list); 
                return result;
            }
            
            // For each element in that list
            foreach (var element in list)
            { 
                var remainingList = new List<T>(list);
                remainingList.Remove(element); // Get a list containing everything except of chosen element

                // Get all possible sub-permutations
                foreach (var permutation in Permutations<T>(remainingList))
                { 
                    // Add that element
                    permutation.Add(element); 
                    result.Add(permutation);
                }
            }

            return result;
        }

        private static double ComputeExpenseRatio(IList<Order> orders)
        {
            var weighted_sum = orders.Sum(x => x.Fund?.ExpenseRatio * (double)x.Value);
            var weight = orders.Sum(x => (double)x.Value);
            var er = weighted_sum.Value / weight;
            return er;
        }

        private Portfolio GeneratePortfolio(
            IReadOnlyCollection<Account> accounts,
            ICollection<Exposure> exposures)
        {
            // parameters should be readonly
            var accountRemainders = new Dictionary<Account, decimal>();
            foreach (var a in accounts)
            {
                accountRemainders[a] = a.Value;
            }
            var exposureRemainders = new Dictionary<Exposure, decimal>();
            foreach (var e in exposures)
            {
                exposureRemainders[e] = e.Target;
            }

            // BUY ORDERS
            var orders = new List<Order>();
            var warnings = new List<string>();
            var errors = new List<string>();
            foreach (var e in exposures)
            {
                // find accounts with access to the right funds
                var suitableAccounts = accounts
                    .Where(a => accountRemainders[a] > 0m && a.GetFund(e) != null)
                    .OrderByDescending(a => accountRemainders[a]);

                if (!suitableAccounts.Any())
                {
                    var ecName = Enum.GetName(typeof(AssetClass), e.Class);
                    var elName = Enum.GetName(typeof(AssetLocation), e.Location);
                    var m = $"Error: Cannot execute strategy: no account has access to asset type: {ecName}, {elName}";
                    errors.Add(m);
                    continue;
                }

                // buy as much as possible from prefered accounts, in order
                foreach (var t in e.AccountTypesPreference)
                {
                    var efficientAccounts = suitableAccounts
                        .Where(x => x.Type == t)
                        .OrderByDescending(x => x.GetFund(e).Ratio(e));

                    foreach (var a in efficientAccounts)
                    {
                        // pick best fund from account
                        var f = a.GetFund(e);

                        // try to exhaust this exposure with this fund
                        var efficiencyRatio = f.Ratio(e);
                        var purchaseValue = Math.Min(accountRemainders[a], exposureRemainders[e] / (decimal)efficiencyRatio);

                        // create order and reduce remainders
                        if (purchaseValue > 0)
                        {
                            // create order
                            orders.Add(new Order
                            {
                                Account = a,
                                Value = purchaseValue,
                                Fund = f
                            });

                            // reduce account remainders
                            accountRemainders[a] -= purchaseValue;

                            // reduce exposure remainders
                            foreach (var c in (AssetClass[])Enum.GetValues(typeof(AssetClass)))
                            {
                                foreach (var l in (AssetLocation[])Enum.GetValues(typeof(AssetLocation)))
                                {
                                    var exposureValue = (decimal)((double)purchaseValue * f.Ratio(c) * f.Ratio(l));
                                    if (exposureValue > 0m)
                                    {
                                        var _e = exposures.First(x => x.Class == c && x.Location == l);
                                        exposureRemainders[_e] -= exposureValue;
                                    }
                                }
                            }

                            if (exposureRemainders[e] <= 0)
                            {
                                break; // Exposure met: stop looking through accounts
                            }
                        }
                    }

                    if (exposureRemainders[e] <= 0)
                    {
                        break; // Exposure met: stop looking through account types
                    }
                }
            }

            // SCORE THE PORTFOLIO (bigger is better)
            var score = 0.0;
            foreach (var e in exposures)
            {
                var r = exposureRemainders[e];
                if (r == 0)
                    continue;

                warnings.Add($"Warning: Imbalance: {e}, remainder: {r}");
                score += 1.0 - (double)Math.Abs(r) / (double)e.Target;
            }

            var totalValue = 0m;
            foreach (var a in accounts)
            {
                totalValue += a.Value;

                var r = accountRemainders[a];
                if (r == 0m)
                {
                    // cool, it all worked out
                    score += 1.0;
                }
                else if (r > 0m)
                {
                    // how far were we off? 
                    score += 1.0 - (double)r / (double)a.Value;
                    warnings.Add($"Warning: Underdraft: {a}, remainder: {r}");
                }
                else if(r < 0m)
                {
                    errors.Add($"Error: Overdraft: {a}, remainder: {r}");
                }
            }
            
            // compute final score
            score = score / (double)(accounts.Count + exposures.Count);

            // RESULT
            return new Portfolio
            {
                Strategy = this.GetType().Name,
                BuyOrders = orders,
                ExpenseRatio = ComputeExpenseRatio(orders),
                BondPercent = (double)bonds_ratio,
                StockPercent = (double)stocks_ratio,
                TotalValue = totalValue,
                Score = score,
                Warnings = warnings,
                Errors  = errors
            };
        }


        public override Portfolio Perform(
            IReadOnlyCollection<Account> accounts,
            IReadOnlyList<Fund> funds)
        {
            // ANALYZE ACCOUNTS
            var totalValue = 0m;
            foreach (var a in accounts)
            {
                totalValue += a.Value;

                // Allow each account to pre-cache it's preferred list of funds from the available database
                a.SelectFunds(funds);
            }

            // compute final exposures we want to acheive
            var exposures = ComputeExposures(totalValue);

            var portfolios = Permutations(exposures)
                .Select(x => GeneratePortfolio(accounts, x));

            var bestToWorst = portfolios
                .Where(x => x.Errors.Count == 0)       // no errors
                .OrderByDescending(x => x.Score)       // best score
                .ThenByDescending(x => x.ExpenseRatio) // lowest cost
                .ThenBy(x => x.BuyOrders.Count);       // fewest orders

            return bestToWorst.FirstOrDefault();
        }
    }
}
