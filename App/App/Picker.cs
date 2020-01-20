using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioPicker.App
{
    public class Picker
    {
        // public static Picker Create(
        //     IList<Account> accounts,
        //     IList<Fund> funds = null)
        // {
        //     var portfolio = new Portfolio { Accounts = accounts };
        //     Fund.Add(funds);
        //     return new Picker
        //     {
        //         Portfolio = portfolio
        //     };
        // }

        // public static Picker Create(
        //     string portfolioYaml = null,
        //     string fundsYaml = null)
        // {
        //     Fund.FromYaml(fundsYaml);
        //     return new Picker
        //     {
        //         Portfolio = Portfolio.FromYaml(portfolioYaml)
        //     };
        // }

        /// <summary>
        /// follow a strategy to produce positions
        /// </summary>
        public static RebalancedPortfolio Rebalance(
            Portfolio portfolio,
            double stockRatio,
            double domesticStockRatio,
            double domesticBondRatio)
        {
            // compute all possible orders of exposure priorities
            //   and return the product with the highest score
            var targetRatios = ComputeTargetRatios(stockRatio, domesticStockRatio, domesticBondRatio);
            var exposures = ComputeExposures(targetRatios, portfolio.TotalValue);
            var result = Permutations(exposures)
                .Select(x => GeneratePortfolio(portfolio, x))
                .Where(x => x.Errors.Count == 0)  // no errors
                .OrderByDescending(x => x.Score)  // highest score
                .ThenBy(x => x.ExpenseRatio)      // lowest cost
                .ThenBy(x => x.NumberOfPositions) // fewest positions
                .FirstOrDefault();                // take the best
            if (result == null)
            {
                return null;
            }

            result.TargetExposureRatios = targetRatios;
            result.Orders = ComputeOrders(portfolio, result);
            return result;
        }

        /// <summary>
        /// produce orders required to move from original portfolio to new one
        /// </summary>
        private static IList<Order> ComputeOrders(
            Portfolio original,
            Portfolio balanced)
        {
            var orders = new List<Order>();
            var accounts = original.Accounts.Union(balanced.Accounts);

            foreach (var a in accounts)
            {
                var newA = balanced.Accounts.FirstOrDefault(x => x == a);
                var oldA = original.Accounts.FirstOrDefault(x => x == a);

                if (newA is null)
                {
                    // sell all
                    orders.AddRange(oldA.Positions.Select(x => Order.Create(a.Name, x.Symbol, -x.Value)));
                }
                else if (oldA is null)
                {
                    // buy all
                    orders.AddRange(newA.Positions.Select(x => Order.Create(a.Name, x.Symbol, x.Value)));
                }
                else
                {
                    // modify position
                    var symbols = new HashSet<string>();
                    foreach (var p in oldA.Positions) { symbols.Add(p.Symbol); }
                    foreach (var p in newA.Positions) { symbols.Add(p.Symbol); }
                    foreach (var s in symbols)
                    {
                        var newP = newA.Positions.FirstOrDefault(x => x.Symbol == s);
                        var oldP = oldA.Positions.FirstOrDefault(x => x.Symbol == s);
                        var difference = (newP == null ? 0.0 : newP.Value) - (oldP == null ? 0.0 : oldP.Value);
                        orders.Add(Order.Create(a.Name, s, difference));
                    }
                }
            }

            return orders.Where(x => x != null).ToList();
        }

        /// <summary>
        /// Given a list, return a list of all possible permutations.
        /// </summary>
        public static ICollection<ICollection<T>> Permutations<T>(ICollection<T> list)
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

        /// <summary>
        /// create list of <class>Exposure</class>s given three stats.
        /// </summary>
        private static IList<Exposure> ComputeTargetRatios(
            double stockRatio,
            double domesticStockRatio,
            double domesticBondRatio)
        {
            return new List<Exposure>
            {
                new Exposure(
                    AssetClass.Stock,
                    AssetLocation.Domestic,
                    stockRatio * domesticStockRatio),
                new Exposure(
                    AssetClass.Stock,
                    AssetLocation.International,
                    stockRatio * (1.0 - domesticStockRatio)),
                new Exposure(
                    AssetClass.Bond,
                    AssetLocation.Domestic,
                    (1.0 - stockRatio) * domesticBondRatio),
                new Exposure(
                    AssetClass.Bond,
                    AssetLocation.International,
                    (1.0 - stockRatio) * (1.0 - domesticBondRatio)),
            };
        }

        private static IList<Exposure> ComputeExposures(
            IList<Exposure> ratios,
            double totalValue)
        {
            return ratios.Select(x => new Exposure(
                x.Class,
                x.Location,
                totalValue * x.Value))
                .ToList();
        }

        /// <summary>
        /// Pick the best fund meeting the requirements from the list available to this account. 
        /// A "better" fund has better ratios for the target exposure, or has the lowest expense ratio. 
        /// </summary>
        private static Fund PickBestFund(
            Exposure e,
            ICollection<Fund> funds)
        {
            var best = default(Fund);
            var bestCoverage = double.NegativeInfinity;
            foreach (var f in funds)
            {
                var coverageForThisExposure = f.Ratio(e);
                if (coverageForThisExposure == 0.0
                || coverageForThisExposure < bestCoverage)
                {
                    continue;
                }

                if (best == null
                || coverageForThisExposure > bestCoverage
                || f.ExpenseRatio < best.ExpenseRatio)
                {
                    best = f;
                    bestCoverage = coverageForThisExposure;
                }
            }
            return best;
        }

        private static RebalancedPortfolio GeneratePortfolio(
            Portfolio portfolio,
            ICollection<Exposure> exposures)
        {
            // setup bookkeeping
            var positions = new List<(Account, Position)>();
            var warnings = new List<string>();
            var errors = new List<string>();
            var accountRemainders = new Dictionary<Account, double>();
            var exposureRemainders = new Dictionary<Exposure, double>();

            // function to allocate some resources
            void Buy(
                Account a, 
                double value, 
                string symbol = null, 
                Fund fund = null, 
                bool hold = false)
            {
                // resolve fund
                fund = fund ?? Fund.Get(symbol);

                // check if we already have a position for this
                var pair = positions.Find(x => x.Item1 == a && x.Item2.Symbol == fund.Symbol);
                var p = pair.Item2;
                if (p is null)
                {
                    // create new position
                    p = new Position
                    {
                        Symbol = fund.Symbol,
                        Value = value,
                        Hold = hold
                    };
                    positions.Add((a, p));
                } 
                else 
                {
                    // increase previous position
                    p.Value += value;
                }

                // reduce account remainders
                accountRemainders[a] -= value;

                // reduce exposure remainders
                foreach (var c in Enum.GetValues(typeof(AssetClass)).Cast<AssetClass>())
                {
                    foreach (var l in Enum.GetValues(typeof(AssetLocation)).Cast<AssetLocation>())
                    {
                        var exposureValue = value * fund.Ratio(c) * fund.Ratio(l);
                        if (exposureValue > 0.0)
                        {
                            var _e = exposures.First(x => x.Class == c && x.Location == l);
                            exposureRemainders[_e] -= exposureValue;
                        }
                    }
                }
            }

            // initialize remainders 
            foreach (var e in exposures)
            {
                exposureRemainders[e] = e.Value;
            }
            foreach (var a in portfolio.Accounts)
            {
                accountRemainders[a] = a.Value;

                // pre "buy" all positions we are asked to hold
                foreach (var p in a.Positions.Where(x => x.Hold))
                {
                    Buy(a, p.Value, symbol: p.Symbol, hold: true);
                }
            }

            // produce optimized positions
            // exposures are looped-through in exactly the order provided to us
            foreach (var e in exposures)
            {
                // Do we still need to meet this exposure target?
                if (exposureRemainders[e] <= 0)
                {
                    continue;
                }

                // find accounts with access to the right funds
                var suitableAccounts = portfolio.Accounts
                    .Where(a => accountRemainders[a] > 0.0)
                    .Where(a => PickBestFund(e, Fund.GetFunds(a.Brokerage)) != null)
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
                foreach (var t in Exposure.GetPreferences(e.Class, e.Location))
                {
                    var efficientAccounts = suitableAccounts
                        .Where(x => x.Type == t)
                        .OrderByDescending(x => PickBestFund(e, Fund.GetFunds(x.Brokerage)).Ratio(e));

                    foreach (var a in efficientAccounts)
                    {
                        // pick best fund from account
                        var f = PickBestFund(e, Fund.GetFunds(a.Brokerage));

                        // try to exhaust this exposure with this fund
                        var percentOfThisFundThatAppliesToThisExposureType = f.Ratio(e);
                        var purchaseValue = Math.Min(accountRemainders[a], exposureRemainders[e] / percentOfThisFundThatAppliesToThisExposureType);

                        // create position and reduce remainders
                        if (purchaseValue > 0)
                        {
                            // buy
                            Buy(a, purchaseValue, fund: f);

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

            // convert any remainders into cash positions
            foreach (var pair in accountRemainders)
            {
                var a = pair.Key;
                var r = pair.Value;
                if (r > 0)
                {
                    var p = new Position
                    {
                        Symbol = "CASH",
                        Value = r
                    };
                    positions.Add((a, p));
                }
                else if (r < 0.0)
                {
                    errors.Add($"Error: Overdraft: {a}, remainder: {r}");
                }
            }

            // create portfolio
            var newAccounts = positions.GroupBy(
                x => x.Item1,
                x => x.Item2,
                (key, g) =>
                {
                    var newAccount = key.Clone();
                    newAccount.Positions = g.ToList();
                    return newAccount;
                })
                .ToList();

            // save results
            var rebalanced = new RebalancedPortfolio(newAccounts)
            {
                Warnings = warnings,
                Errors = errors,
            };

            // score portfolio
            rebalanced.Score = rebalanced.GetScore(exposures);

            return rebalanced;
        }
    }
}
