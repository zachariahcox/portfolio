using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Threading;
using System.Linq;
using System.IO;

namespace PortfolioPicker.App
{
    public class Picker
    {
        /// <summary>
        /// follow a strategy to produce positions
        /// </summary>
        public static RebalancedPortfolio Rebalance(
            Portfolio portfolio,
            double stockRatio,
            double domesticStockRatio,
            double domesticBondRatio)
        {
            // compute target portfolio's exposure to various investment types
            var targetRatios = ComputeTargetRatios(stockRatio, domesticStockRatio, domesticBondRatio);
            
            // in which order should we pick from available accounts?
            var orderedAccounts = portfolio.Accounts.OrderBy(x => x.Type).ToArray();
            
            // state
            var generateTotal = 
                Factorial(targetRatios.Count) * 
                Math.Pow(Factorial(_allAccountTypes.Count), targetRatios.Where(x => x.Value > 0).Count());
            var logPercentage = Math.Round(generateTotal / 20);
            var startGeneration = DateTime.Now;
            var generateSum = TimeSpan.Zero;
            var generateCount = 0;
            var portfolioPermutations = new ConcurrentDictionary<int, RebalancedPortfolio>();

            void DoWork(ICollection<Exposure> trp)
            {
                foreach (var sdt in GetPermutations(trp.First(x => x.Class == AssetClass.Stock && x.Location == AssetLocation.Domestic).Value))
                foreach (var sit in GetPermutations(trp.First(x => x.Class == AssetClass.Stock && x.Location == AssetLocation.International).Value))
                foreach (var bdt in GetPermutations(trp.First(x => x.Class == AssetClass.Bond && x.Location == AssetLocation.Domestic).Value))
                foreach (var bit in GetPermutations(trp.First(x => x.Class == AssetClass.Bond && x.Location == AssetLocation.International).Value))
                {   
                    var accountTypePreferences = new List<ExposureAccountTypePreference>
                    {
                        new ExposureAccountTypePreference(AssetClass.Stock, AssetLocation.Domestic, sdt),
                        new ExposureAccountTypePreference(AssetClass.Stock, AssetLocation.International, sit),
                        new ExposureAccountTypePreference(AssetClass.Bond, AssetLocation.Domestic, bdt),
                        new ExposureAccountTypePreference(AssetClass.Bond, AssetLocation.International, bit),
                    };

                    var t = DateTime.Now;
                    var g = GeneratePortfolio(portfolio, orderedAccounts, trp, accountTypePreferences);
                    generateSum += DateTime.Now - t; // this races but it's not critical
                    var newCount = Interlocked.Increment(ref generateCount);

                    // uniquify produced portfolios with an ID key
                    portfolioPermutations.TryAdd(g.DescriptorKey, g);

                    // report progress
                    if (newCount % logPercentage == 0.0)
                    {
                        Console.WriteLine(string.Format("{0:n0}%, {1:n0} portfolios / sec.", 
                            100.0 * newCount / generateTotal,
                            1.0 / (generateSum / newCount).TotalSeconds));
                    }
                }
            }

            Console.WriteLine(string.Format("Start: generate {0:n0} portfolios.", generateTotal));
            var pq = Permutations(targetRatios).AsParallel();
            pq.ForAll(trp => DoWork(trp));
            var results = pq.ToArray(); // force synchronize
            var finishTime = DateTime.Now - startGeneration;
            
            // take best portfolios that are at least better than we were
            var originalScore = portfolio.GetScore(targetRatios);
            var portfolios = portfolioPermutations.Values
                .Where(x => x.Errors.Count == 0)  // no errors
                .Where(x => x.Score > originalScore)
                .OrderByDescending(x => x.Score - x.OrdersScore)  // highest score
                .Take(100)
                .ToArray();

            // final log
            Console.WriteLine(
            string.Format("Generated {0:n0} unique portfolios out of {1:n0} attempts in {2} seconds.\n{3:n0} score higher than the original.",
                portfolioPermutations.Count,
                generateTotal,
                Math.Round(finishTime.TotalSeconds),
                portfolios.Count()
                ));

            // debug
            var i = 0;
            foreach(var p in portfolios)
            {
                p.Save(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                    "work/build/test", 
                    i.ToString()));
                ++i;
            }

            // take the best
            return portfolios.FirstOrDefault();
        }

        private static RebalancedPortfolio GeneratePortfolio(
            Portfolio portfolio, 
            IEnumerable<Account> accounts,
            ICollection<Exposure> targetExposureRatios, 
            ICollection<ExposureAccountTypePreference> accountTypePreferences)
        {
            // setup bookkeeping
            var warnings = new List<string>();
            var errors = new List<string>();
            var positionsByAccount = new Dictionary<Account, IList<Position>>();
            var accountRemainders = new Dictionary<Account, double>();
            var exposureRemainders = new Dictionary<Exposure, double>();
            var exposures = targetExposureRatios
                .Select(x => new Exposure(x.Class, x.Location, portfolio.TotalValue * x.Value))
                .ToArray();

            // function to allocate some resources
            void Buy(
                Account a, 
                double value, 
                string symbol = null, 
                Fund fund = null, 
                bool hold = false)
            {
                if (value <= 0)
                    return;

                // resolve fund
                fund = fund ?? Fund.Get(symbol);

                // check if we already have a position for this
                if(!positionsByAccount.TryGetValue(a, out var positions))
                {
                    positions = new List<Position>();
                    positionsByAccount[a] = positions;
                }
                var p = positions.FirstOrDefault(x => x.Symbol == fund.Symbol);
                if (p is null)
                {
                    // add new position
                    positions.Add(new Position
                    {
                        Symbol = fund.Symbol,
                        Value = value,
                        Hold = hold
                    });
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
                foreach (var l in Enum.GetValues(typeof(AssetLocation)).Cast<AssetLocation>())
                {
                    var exposureValue = value * fund.Ratio(c) * fund.Ratio(l);
                    if (exposureValue > 0.0)
                    {
                        var e = exposures.First(x => x.Class == c && x.Location == l);
                        exposureRemainders[e] -= exposureValue;
                    }
                }
            }

            // initialize remainders 
            foreach (var e in exposures)
                exposureRemainders[e] = e.Value;
            foreach (var a in accounts)
            {
                accountRemainders[a] = a.Value;
                // pre "buy" all positions we are asked to hold
                foreach (var p in a.Positions.Where(x => x.Hold).ToArray())
                    Buy(a, p.Value, symbol: p.Symbol, hold: true);
            }

            // produce optimized positions
            // exposures are looped-through in exactly the order provided to us
            foreach (var e in exposures)
            {
                // Do we still need to meet this exposure target?
                if (exposureRemainders[e] <= 0)
                    continue;

                // buy as much as possible from prefered accounts, in order
                foreach (var t in accountTypePreferences.First(x => x.Class == e.Class && x.Location == e.Location).Preferences)
                {
                    // which accounts will work? 
                    var suitableAccounts = accounts
                        .Where(a => a.Type == t && accountRemainders[a] > 0.0)
                        .ToArray();
                
                    foreach (var a in suitableAccounts)
                    {
                        // pick best fund from account
                        var f = PickBestFund(e, Fund.GetFunds(a.Brokerage));
                        if (f is null)
                            continue; // this account doesn't have any option

                        // try to exhaust this exposure with this fund
                        var purchaseValue = Math.Min(accountRemainders[a], 
                            exposureRemainders[e] / f.Ratio(e));

                        // add position and reduce remainders
                        Buy(a, purchaseValue, fund: f);
                        if (exposureRemainders[e] <= 0)
                            break; // Exposure met: stop looking through accounts
                    }

                    if (exposureRemainders[e] <= 0)
                        break; // Exposure met: stop looking through account types
                }
            }

            // convert any remainders into cash positions
            foreach (var a in accountRemainders.Keys)
            {
                var r = accountRemainders[a];
                if (r > 0)
                {
                    var cash = new Position
                    {
                        Symbol = "CASH",
                        Value = r
                    };

                    if (positionsByAccount.TryGetValue(a, out var positions))
                        positions.Add(cash);
                    else
                        positionsByAccount.Add(a, new List<Position>{cash});
                }
                else if (r < 0.0)
                {
                    errors.Add($"Error: Overdraft: {a}, remainder: {r}");
                }
            }

            // create new accounts
            var newAccounts = positionsByAccount.Select(x => {
                var rc = x.Key.Clone(); 
                rc.Positions = x.Value; 
                return rc;}).ToList();

            // create portfolio
            var rebalanced = new RebalancedPortfolio(newAccounts)
            {
                Warnings = warnings,
                Errors = errors,
                Original = portfolio,
                TargetExposureRatios = targetExposureRatios
            };
            rebalanced.Orders = Portfolio.ComputeOrders(portfolio, rebalanced);
            rebalanced.OrdersScore = rebalanced.GetOrdersScore();
            rebalanced.Score = rebalanced.GetScore(targetExposureRatios);
            return rebalanced;
        }

        /// <summary>
        /// Pick the best fund meeting the requirements from the list available to this account. 
        /// A "better" fund has better ratios for the target exposure, or has the lowest expense ratio. 
        /// Typically single-asset class mutual funds have the lowest expense ratios anyway.
        /// </summary>
        private static Fund PickBestFund(
            Exposure e,
            IEnumerable<Fund> funds)
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

        /// <summary>
        /// create list of <class>Exposure</class>s given three stats.
        /// </summary>
        public static IList<Exposure> ComputeTargetRatios(
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

        private static IList<AccountType> _allAccountTypes = Enum.GetValues(typeof(AccountType)).Cast<AccountType>().ToList();
        private static ICollection<ICollection<AccountType>> GetPermutations(double value)
        {
            // if the value assigned is going to be zero anyway, don't bother permuting
            return value <= 0.0
                ? new List<ICollection<AccountType>>(){_allAccountTypes} 
                : Permutations(_allAccountTypes);
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

        public static double Factorial(int number)
        {
            double result = 1;
            while (number != 1)
            {
                result = result * number;
                number = number - 1;
            }
            return result;
        }
    }
}
