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
            double domesticBondRatio, 
            int iterationLimit = -1,
            int threadLimit = -1,
            bool reportProgress = false,
            string debugOutputDirectory = default // where to drop the best 100 for debugging
        )
        {
            // compute target portfolio's exposure to various investment types
            // this is the raw list of values without any priority ordering.
            var targetRatios = ComputeTargetRatios(stockRatio, domesticStockRatio, domesticBondRatio);
            
            // compute original score with these ratios
            portfolio.Score = portfolio.GetScore(Score.GetScoreWeight, targetRatios);

            // in which order should we pick from available accounts?
            var orderedAccounts = portfolio.Accounts.OrderBy(x => x.Type).ToArray();
            
            // state
            var generateTotal = Factorial(targetRatios.Count) * Math.Pow(Factorial(AccountTypes.ALL.Length), targetRatios.Where(x => x.Value > 0).Count());
            if (iterationLimit != -1) 
                generateTotal = Math.Max(1, Math.Min(iterationLimit, generateTotal));
            var degreeOfParallelism = Environment.ProcessorCount;
            if (threadLimit != -1) 
                degreeOfParallelism = Math.Max(1, Math.Min(threadLimit, degreeOfParallelism));

            var iterationsPerReport = Math.Round(generateTotal / 20);
            var generateTotalTime = TimeSpan.Zero;
            var generateCount = 0;
            var portfolioPermutations = new ConcurrentDictionary<int, RebalancedPortfolio>();

            // the ratio permutations are processed in parallel
            // In each thread, this 
            void DoWork(ICollection<Exposure> targetRatioPermutation)
            {
                foreach (var sdt in AccountTypePermutations(targetRatioPermutation.First(x => x.Class == AssetClass.Stock && x.Location == AssetLocation.Domestic).Value))
                foreach (var sit in AccountTypePermutations(targetRatioPermutation.First(x => x.Class == AssetClass.Stock && x.Location == AssetLocation.International).Value))
                foreach (var bdt in AccountTypePermutations(targetRatioPermutation.First(x => x.Class == AssetClass.Bond  && x.Location == AssetLocation.Domestic).Value))
                foreach (var bit in AccountTypePermutations(targetRatioPermutation.First(x => x.Class == AssetClass.Bond  && x.Location == AssetLocation.International).Value))
                {   
                    // early bailout
                    if (iterationLimit >= 0 && generateCount >= iterationLimit)
                        return;

                    // create unique preference set
                    var accountTypePreferences = new List<ExposureAccountTypePreference>
                    {
                        new ExposureAccountTypePreference(AssetClass.Stock, AssetLocation.Domestic, sdt),
                        new ExposureAccountTypePreference(AssetClass.Stock, AssetLocation.International, sit),
                        new ExposureAccountTypePreference(AssetClass.Bond, AssetLocation.Domestic, bdt),
                        new ExposureAccountTypePreference(AssetClass.Bond, AssetLocation.International, bit),
                    };

                    // create portfolio
                    var t = DateTime.Now;
                    var p = GeneratePortfolio(
                        portfolio, 
                        orderedAccounts, 
                        targetRatioPermutation, 
                        accountTypePreferences);
                    generateTotalTime += DateTime.Now - t; // this races but it's not critical
                    var newCount = Interlocked.Increment(ref generateCount);
                    portfolioPermutations.TryAdd(p.DescriptorKey, p);

                    // report progress
                    if (reportProgress && newCount % iterationsPerReport == 0.0)
                    {
                        Console.WriteLine(string.Format("{0:n0}%, {1:n0} portfolios / sec.", 
                            100.0 * newCount / generateTotal,
                            1.0 / (generateTotalTime / newCount).TotalSeconds));
                    }
                }
            }

            Console.WriteLine(string.Format("Start: generate {0:n0} portfolios.", generateTotal));
            var startGenerationTime = DateTime.Now;
            var pq = Permutations(targetRatios)
                .AsParallel()
                .WithDegreeOfParallelism(degreeOfParallelism);
            pq.ForAll(trp => DoWork(trp));
            var results = pq.ToArray(); // force synchronize
            var duration = DateTime.Now - startGenerationTime;
            
            // take the best unique portfolios
            const int N = 50;
            var topN = portfolioPermutations.Values
                .Where(x => x.Errors.Count == 0)
                .OrderByDescending(x => x.Score.Total)
                .Take(N)
                .ToArray();

            // we mostly only care about the ones better than the input
            var originalTotalScore = portfolio.Score.Total;
            var rawScoreIsBetter = topN
                .Where(x => x.Score.Total > originalTotalScore)
                .Count();
            var worthRebalancing = topN
                .Where(x => x.Score.RebalanceTotal > originalTotalScore)
                .Count();
                
            // final log
            Console.WriteLine(
                string.Format("Generated {0:n0} unique portfolios out of {1:n0} attempts in {2} seconds.\n{3:n0} score higher than the original.\n{4} are probably worth the tax implications of rebalancing.",
                    portfolioPermutations.Count,
                    generateTotal,
                    Math.Round(duration.TotalSeconds),
                    rawScoreIsBetter,
                    worthRebalancing
                    ));

            // save best if asked
            if (!string.IsNullOrWhiteSpace(debugOutputDirectory))
            {
                var i = 0;
                foreach(var p in topN)
                {
                    p.Save(Path.Combine(debugOutputDirectory, i.ToString()));
                    ++i;
                }
            }

            // take the best even though it might not be better than the input?
            return topN.FirstOrDefault();
        }

        private static RebalancedPortfolio GeneratePortfolio(
            Portfolio original, 
            IEnumerable<Account> accounts,
            ICollection<Exposure> prioritizedTargetExposureRatios, 
            ICollection<ExposureAccountTypePreference> accountTypePreferences)
        {
            // setup bookkeeping
            var sc = original.AvailableSecurities;
            var errors = new List<string>();
            var positionsByAccount = new Dictionary<Account, IList<Position>>();
            var accountRemainders = new Dictionary<Account, double>();
            var exposureRemainders = new Dictionary<Exposure, double>();
            var exposures = prioritizedTargetExposureRatios
                .Select(x => new Exposure(x.Class, x.Location, original.TotalValue * x.Value))
                .ToArray();

            // function to allocate some resources
            void Buy(
                Account a, 
                double value, 
                string symbol = null, 
                Security security = null, 
                bool hold = false)
            {
                if (value <= 0)
                    return;

                // resolve fund
                security = security ?? sc.Get(symbol);

                // check if we already have a position for this
                if(!positionsByAccount.TryGetValue(a, out var positions))
                {
                    positions = new List<Position>();
                    positionsByAccount[a] = positions;
                }
                var p = positions.FirstOrDefault(x => x.Symbol == security.Symbol);
                if (p is null)
                {
                    // add new position
                    positions.Add(new Position
                    {
                        Symbol = security.Symbol,
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

                // reduce exposure remainders (funds can be exposed to multiple asset types)
                foreach (var c in AssetClasses.ALL)
                foreach (var l in AssetLocations.ALL)
                {
                    var exposureValue = value * security.Ratio(c) * security.Ratio(l);
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

                // buy as much as possible from preferred accounts, in order
                foreach (var t in accountTypePreferences.First(x => x.Class == e.Class && x.Location == e.Location).Preferences)
                {
                    foreach(var a in accounts)
                    {
                        // ignore accounts of the wrong type
                        if (a.Type != t)
                            continue;
                        
                        // ignore empty accounts
                        var remainder = accountRemainders[a];
                        if (remainder <= 0.0)
                            continue;

                        // pick best fund from account
                        var s = PickBestSecurity(e, sc.GetSecurityGroup(a.Brokerage));
                        if (s is null)
                            continue; // this account doesn't have any option

                        // try to exhaust this exposure with this fund
                        var purchaseValue = Math.Min(remainder, exposureRemainders[e] / s.Ratio(e));

                        // add position and reduce remainders
                        Buy(a, purchaseValue, security: s);

                        // Exposure met: stop looking through accounts
                        if (exposureRemainders[e] <= 0)
                            break; 
                    }

                    // Exposure met: stop looking through account types
                    if (exposureRemainders[e] <= 0)
                        break; 
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
                        Symbol = Cash.CASH,
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
            var newAccounts = new List<Account>(positionsByAccount.Count);
            foreach(var p in positionsByAccount)
            {
                var a = p.Key.Clone();
                a.Positions = p.Value;
                newAccounts.Add(a);
            }

            // create new portfolio
            var rebalanced = new RebalancedPortfolio(
                accounts: newAccounts, 
                original : original,
                targetExposureRatios: prioritizedTargetExposureRatios,
                errors: errors);            
            return rebalanced;
        }

        /// <summary>
        /// Pick the best security meeting the requirements from the list available to this account. 
        /// A "better" security has better ratios for the target exposure, or has the lowest expense ratio. 
        /// Typically single-asset class mutual funds have the lowest expense ratios anyway.
        /// </summary>
        private static Security PickBestSecurity(Exposure e, IEnumerable<Security> securities)
        {
            var best = default(Security);
            var bestCoverage = double.NegativeInfinity;
            foreach (var s in securities)
            {
                var coverageForThisExposure = s.Ratio(e);
                if (coverageForThisExposure == 0.0
                || coverageForThisExposure < bestCoverage)
                {
                    continue;
                }

                if (best == null
                || coverageForThisExposure > bestCoverage
                || s.ExpenseRatio < best.ExpenseRatio)
                {
                    best = s;
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

        private static ICollection<ICollection<AccountType>> AccountTypePermutations(double value)
        {
            // if the value assigned is going to be zero anyway, don't bother permuting
            return value <= 0.0
                ? new List<ICollection<AccountType>>(){AccountTypes.ALL} 
                : Permutations(AccountTypes.ALL);
        }

        /// <summary>
        /// Given a list, return a list of all possible permutations.
        /// </summary>
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

        private static double Factorial(int number)
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
