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
            AccountTypesPreference = new[] { AccountType.TAXABLE, AccountType.CORPORATE, AccountType.ROTH }
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

        static double ComputeExpenseRatio(
            IReadOnlyCollection<Account> accounts,
            IReadOnlyCollection<Fund> funds)
        {
            var positions = accounts.SelectMany(x => x.Positions).ToList();
            var weighted_sum = positions.Sum(p => funds.FirstOrDefault(f => f.Symbol == p.Symbol)?.ExpenseRatio * (double)p.Value);
            var weight = positions.Sum(x => (double)x.Value);
            var er = weighted_sum.Value / weight;
            return er;
        }

        private Dictionary<string, IReadOnlyList<Fund>> _fundsByBrokerage = new Dictionary<string, IReadOnlyList<Fund>>();
        private IReadOnlyList<Fund> GetFunds(
            string brokerage, 
            IReadOnlyCollection<Fund> funds)
        {
            // use cache
            if (!_fundsByBrokerage.TryGetValue(brokerage, out var fundsAtBrokerage))
            {
                fundsAtBrokerage = funds?
                    .Where(x => string.Equals(x.Brokerage, brokerage, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.Symbol)
                    .ToList() as IReadOnlyList<Fund>;

                _fundsByBrokerage[brokerage] = fundsAtBrokerage;
            }
            return fundsAtBrokerage;
        }

        /// <summary>
        /// Pick the best fund meeting the requirements from the list available to this account. 
        /// A "better" fund has better ratios for the target exposure, or has the lowest expense ratio. 
        /// </summary>
        internal Fund GetFund(
            Exposure e,
            IReadOnlyCollection<Fund> funds)
        {
            var best = default(Fund);
            var bestCoverage = -1.0;
            foreach (var f in funds)
            {
                var coverageForThisExposure = f.Ratio(e);
                if (coverageForThisExposure == 0.0)
                    continue;

                if (best == null)
                {
                    best = f;
                    bestCoverage = coverageForThisExposure;
                }
                else if (coverageForThisExposure > bestCoverage || f.ExpenseRatio < best.ExpenseRatio)
                {
                    best = f;
                }
            }
            return best;
        }


        private Portfolio GeneratePortfolio(
            IReadOnlyCollection<Account> accounts,
            IReadOnlyCollection<Fund> funds,
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

            // POSITIONS
            var positions = new Dictionary<Account, List<Position>>();
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
                    .Where(a => GetFund(e, GetFunds(a.Brokerage, funds)) != null)
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
                        .OrderByDescending(x => GetFund(e, GetFunds(x.Brokerage, funds)).Ratio(e));

                    foreach (var a in efficientAccounts)
                    {
                        // pick best fund from account
                        var f = GetFund(e, GetFunds(a.Brokerage, funds));

                        // try to exhaust this exposure with this fund
                        var percentOfThisFundThatAppliesToThisExposureType = f.Ratio(e);
                        var purchaseValue = Math.Min(accountRemainders[a], exposureRemainders[e] / (decimal)percentOfThisFundThatAppliesToThisExposureType);

                        // create position and reduce remainders
                        if (purchaseValue > 0)
                        {
                            // create position
                            var position = new Position
                            {
                                Value = purchaseValue,
                                Fund = f
                            };
                            if (!positions.TryGetValue(a, out var currentPositions))
                            {
                                positions.Add(a, new List<Position> { position });
                            }
                            else
                            {
                                currentPositions.Add(position);
                            }

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
            var newAccounts = new List<Account>();
            foreach(var (a, lp) in positions)
            {
                var newAccount = a.Clone();
                newAccount.Positions = lp.Select(x => x.Reference()).ToList();
                newAccounts.Add(newAccount);
            }

            return new Portfolio
            {
                Accounts = newAccounts,
                Score = score,
                TotalValue = totalValue,
                Strategy = this.GetType().Name,
                Warnings = warnings,
                Errors = errors,

                // aggregate stats
                ExpenseRatio = ComputeExpenseRatio(newAccounts, funds),
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
            // compute final exposures we want to acheive
            var totalValue = accounts.Sum(x => x.Value);
            var exposures = ComputeExposures(totalValue);

            // compute all possible orders of exposure priorities
            var portfolios = Permutations(exposures)
                .Select(x => GeneratePortfolio(accounts, funds, x));

            var bestToWorst = portfolios
                .Where(x => x.Errors.Count == 0)       // no errors
                .OrderByDescending(x => x.Score)       // best score
                .ThenByDescending(x => x.ExpenseRatio) // lowest cost
                .ThenBy(x => x.NumberOfPositions);     // fewest positions

            return bestToWorst.FirstOrDefault();
        }
    }
}
