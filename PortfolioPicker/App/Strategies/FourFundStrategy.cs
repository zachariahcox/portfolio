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
        internal Exposure SD { get; } = new Exposure
        {
            Class = AssetClass.Stock,
            Location = AssetLocation.Domestic,
            AccountTypesPreference = new[] { AccountType.ROTH, AccountType.TAXABLE, AccountType.CORPORATE },
        };

        internal Exposure SI { get; } = new Exposure
        {
            Class = AssetClass.Stock,
            Location = AssetLocation.International,
            AccountTypesPreference = new[] { AccountType.TAXABLE, AccountType.ROTH, AccountType.CORPORATE }
        };

        internal Exposure BD { get; } = new Exposure
        {
            Class = AssetClass.Bond,
            Location = AssetLocation.Domestic,
            AccountTypesPreference = new[] { AccountType.CORPORATE, AccountType.ROTH, AccountType.TAXABLE }
        };

        internal Exposure BI { get; } = new Exposure
        {
            Class = AssetClass.Bond,
            Location = AssetLocation.International,
            AccountTypesPreference = new[]{AccountType.TAXABLE,  AccountType.CORPORATE, AccountType.ROTH
    }
        };

        /// <summary>
        /// Produce desired exposures based on ratios and total available money
        /// </summary>
        IList<Exposure> ComputeExposures(decimal totalValue)
        {
            var totalStock = totalValue * this.StockRatio;
            var totalBonds = totalValue * this.BondsRatio;
            SD.Target = totalStock * this.StockDomesticRatio;
            SI.Target = totalStock * this.StockInternationalRatio;
            BD.Target = totalBonds * this.BondsDomesticRatio;
            BI.Target = totalBonds * this.BondsInternationalRatio;
            return new List<Exposure> { SD, SI, BD, BI };
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

        static double ComputeExpenseRatio(IList<Order> orders)
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
                // Do we still need to meet this exposure target?
                if (exposureRemainders[e] <= 0)
                {
                    continue;
                }

                // find accounts with access to the right funds
                var suitableAccounts = accounts
                    .Where(a => accountRemainders[a] > 0m)
                    .Where(a => a.GetFund(e) != null)
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
                        var percentOfThisFundThatAppliesToThisExposureType = f.Ratio(e);
                        var purchaseValue = Math.Min(accountRemainders[a], exposureRemainders[e] / (decimal)percentOfThisFundThatAppliesToThisExposureType);

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
            var bestScorePerCategory = 1.0;
            var bestTotalScore = (double)(accounts.Count + exposures.Count);
            foreach (var e in exposures)
            {
                var r = exposureRemainders[e];
                if (r == 0)
                {
                    score += bestScorePerCategory;
                    continue;
                }

                warnings.Add($"Warning: Imbalance: {e}, remainder: {r}");
                score += bestScorePerCategory - (double)Math.Abs(r) / (double)e.Target;
            }

            var totalValue = 0m;
            foreach (var a in accounts)
            {
                totalValue += a.Value;

                var r = accountRemainders[a];
                if (r == 0m)
                {
                    // cool, it all worked out
                    score += bestScorePerCategory;
                }
                else if (r > 0m)
                {
                    // how far were we off? 
                    score += bestScorePerCategory - (double)r / (double)a.Value;
                    warnings.Add($"Warning: Underdraft: {a}, remainder: {r}");
                }
                else if (r < 0m)
                {
                    errors.Add($"Error: Overdraft: {a}, remainder: {r}");
                }
            }

            // compute final score
            score = score / bestTotalScore;

            // RESULT
            return new Portfolio
            {
                Score = score,
                TotalValue = totalValue,
                Strategy = this.GetType().Name,
                BuyOrders = orders,
                Warnings = warnings,
                Errors = errors,

                // aggregate stats
                ExpenseRatio = ComputeExpenseRatio(orders),
                BondRatio = ComputePercentage(exposureRemainders, BD, BI, totalValue),
                StockRatio = ComputePercentage(exposureRemainders, SD, SI, totalValue),
                DomesticRatio = ComputePercentage(exposureRemainders, SD, BD, totalValue),
                InternationalRatio = ComputePercentage(exposureRemainders, SI, BI, totalValue),
            };
        }

        double ComputePercentage(
            IDictionary<Exposure, decimal> remainders, 
            Exposure a, 
            Exposure b,
            decimal total)
        {
            return (double)((a.Target - remainders[a] + b.Target - remainders[b]) / total);
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
