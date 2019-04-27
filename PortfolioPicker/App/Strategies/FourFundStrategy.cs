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
        public override Portfolio Rebalance(Portfolio p)
        {
            // compute final exposures we want to acheive
            var accounts = p.Accounts;
            var totalValue = p.TotalValue;
            var exposures = ComputeExposures(totalValue);

            // compute all possible orders of exposure priorities
            //   and return the product with the highest score
            return Permutations(exposures)
                .Select(x => GeneratePortfolio(accounts, x))
                .Where(x => x.Errors.Count == 0)       // no errors
                .OrderByDescending(x => x.Score)       // best score
                .ThenByDescending(x => x.ExpenseRatio) // lowest cost
                .ThenBy(x => x.NumberOfPositions)      // fewest positions
                .FirstOrDefault();                     // take the best
        }

         /// <summary>
        /// Produce desired exposures based on ratios and total available money
        /// </summary>
        private IList<Exposure> ComputeExposures(decimal totalValue)
        {
            var totalStock = totalValue * this.StockRatio;
            var totalBonds = totalValue * this.BondsRatio;

            var SD = new Exposure
            {
                Class = AssetClass.Stock,
                Location = AssetLocation.Domestic,
                AccountTypesPreference = new[] { AccountType.ROTH, AccountType.TAXABLE, AccountType.CORPORATE },
                Target = totalStock * this.StockDomesticRatio
            };

            var SI = new Exposure
            {
                Class = AssetClass.Stock,
                Location = AssetLocation.International,
                AccountTypesPreference = new[] { AccountType.TAXABLE, AccountType.ROTH, AccountType.CORPORATE },
                Target = totalStock * this.StockInternationalRatio
            };

            var BD = new Exposure
            {
                Class = AssetClass.Bond,
                Location = AssetLocation.Domestic,
                AccountTypesPreference = new[] { AccountType.CORPORATE, AccountType.ROTH, AccountType.TAXABLE },
                Target = totalBonds * this.BondsDomesticRatio
            };

            var BI = new Exposure
            {
                Class = AssetClass.Bond,
                Location = AssetLocation.International,
                AccountTypesPreference = new[] { AccountType.TAXABLE, AccountType.CORPORATE, AccountType.ROTH },
                Target = totalBonds * this.BondsInternationalRatio
            };
            
            return new List<Exposure> { SD, SI, BD, BI };
        }
        
        /// <summary>
        /// Pick the best fund meeting the requirements from the list available to this account. 
        /// A "better" fund has better ratios for the target exposure, or has the lowest expense ratio. 
        /// </summary>
        private Fund PickBestFund(
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

        private Portfolio GeneratePortfolio(
            ICollection<Account> accounts,
            ICollection<Exposure> exposures)
        {
            // setup bookkeeping
            var positions = new List<(Account, Position)>();
            var warnings = new List<string>();
            var errors = new List<string>();
            var accountRemainders = new Dictionary<Account, decimal>();
            var exposureRemainders = new Dictionary<Exposure, decimal>();

            // function to allocate some resources
            void Buy(Account a, decimal value, string symbol = null, Fund fund = null)
            {
                // resolve fund
                fund = fund ?? Fund.Get(symbol);

                // create position
                positions.Add((
                    a,
                    new Position
                    {
                        Symbol = fund.Symbol,
                        Value = value,
                    }
                ));

                // reduce account remainders
                accountRemainders[a] -= value;

                // reduce exposure remainders
                foreach (var c in Enum.GetValues(typeof(AssetClass)).Cast<AssetClass>())
                {
                    foreach (var l in Enum.GetValues(typeof(AssetLocation)).Cast<AssetLocation>())
                    {
                        var exposureValue = (decimal)((double)value * fund.Ratio(c) * fund.Ratio(l));
                        if (exposureValue > 0m)
                        {
                            var _e = exposures.First(x => x.Class == c && x.Location == l);
                            exposureRemainders[_e] -= exposureValue;
                        }
                    }
                }
            }

            foreach (var e in exposures)
            {
                exposureRemainders[e] = e.Target;
            }
            foreach (var a in accounts)
            {
                accountRemainders[a] = a.Value;
                
                // pre "buy" all positions we are asked to hold
                foreach(var p in a.Positions.Where(x => x.Hold))
                {
                    Buy(a, p.Value, symbol: p.Symbol);
                }
            }

            // produce optimized positions
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
                foreach (var t in e.AccountTypesPreference)
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
                        var purchaseValue = Math.Min(accountRemainders[a], exposureRemainders[e] / (decimal)percentOfThisFundThatAppliesToThisExposureType);

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

            return new Portfolio
            {
                Accounts = newAccounts,
                Score = score,
                Strategy = this.GetType().Name,
                Warnings = warnings,
                Errors = errors,
            };
        }
    }
}
