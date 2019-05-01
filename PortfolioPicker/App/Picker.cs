using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioPicker.App
{
    public class Picker
    {
        private Portfolio Portfolio { get; set; }

        static public Picker Create(
            IList<Account> accounts,
            IList<Fund> funds = null)
        {
            var portfolio = new Portfolio { Accounts = accounts };
            Fund.AddRange(funds);
            return new Picker
            {
                Portfolio = portfolio
            };
        }

        static public Picker Create(
            string accountsYaml = null,
            string fundsYaml = null)
        {
            // add custom funds
            Fund.FromYaml(fundsYaml);
            return new Picker
            {
                Portfolio = Portfolio.FromYaml(accountsYaml)
            };
        }

        /// <summary>
        /// follow a strategy to produce positions
        /// </summary>
        public Portfolio Rebalance(
            double stockRatio,
            double domesticStockRatio,
            double domesticBondRatio)
        {
            // compute all possible orders of exposure priorities
            //   and return the product with the highest score
            var targetRatios = new ExposureRatios
            {
                StockRatio = stockRatio,
                DomesticStockRatio = domesticStockRatio,
                DomesticBondRatio = domesticBondRatio
            };
            var exposures = ComputeExposures(targetRatios, Portfolio.TotalValue);
            var result = Permutations(exposures)
                .Select(x => GeneratePortfolio(Portfolio.Accounts, x))
                .Where(x => x.Errors.Count == 0)       // no errors
                .OrderByDescending(x => x.Score)       // best score
                .ThenByDescending(x => x.ExpenseRatio) // lowest cost
                .ThenBy(x => x.NumberOfPositions)      // fewest positions
                .FirstOrDefault();                     // take the best

            if (result == null)
                return null;

            result.Orders = ComputeOrders(Portfolio, result);
            return result;
        }

        public IList<Order> ComputeOrders(
            Portfolio original,
            Portfolio balanced)
        {
            var orders = new List<Order>();
            var accounts = original.Accounts.Union(balanced.Accounts);

            foreach(var a in accounts)
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
                    foreach(var s in symbols)
                    {
                        var newP = newA.Positions.FirstOrDefault(x => x.Symbol == s);
                        var oldP = oldA.Positions.FirstOrDefault(x => x.Symbol == s);
                        var difference = (newP == null ? 0m : newP.Value) - (oldP == null ? 0m : oldP.Value);
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
        /// Based on the target exposures and total money, compute target dollar-value per exposure type.
        ///  Strategy: 
        ///  * accounts prefer funds sponsored by their brokerage
        ///    * Helps avoid fees?
        ///
        ///  * roth accounts should prioritize stocks over bonds
        ///    * Growth is not taxable, prioritize high-growth potential products.
        ///
        ///  * taxable accounts should prioritize international assets over domestic
        ///    * foreign income tax credit
        /// 
        ///  * 401k accounts should prioritize bonds and avoid international assets
        ///    Because growth is taxable, prioritize low-growth products
        ///
        /// This basically works out to the following exposure-to-account type prioritization list:
        ///  dom stocks -> roth, tax, 401k
        ///  int stocks -> tax, roth, 401k
        ///  dom bonds  -> 401k, roth, tax
        ///  int bonds  -> tax, 401k, roth
        /// </summary>
        private IList<Exposure> ComputeExposures(
            ExposureRatios target,
            decimal totalValue)
        {
            var totalStock = totalValue * (decimal)target.StockRatio;
            var totalBonds = totalValue * (decimal)target.BondRatio;

            var SD = new Exposure
            {
                Class = AssetClass.Stock,
                Location = AssetLocation.Domestic,
                Target = totalStock * (decimal)target.DomesticStockRatio,
                AccountTypesPreference = new[] {
                    AccountType.ROTH,
                    AccountType.TAXABLE,
                    AccountType.CORPORATE
                },
            };

            var SI = new Exposure
            {
                Class = AssetClass.Stock,
                Location = AssetLocation.International,
                Target = totalStock * (decimal)target.InternationalStockRatio,
                AccountTypesPreference = new[] {
                    AccountType.TAXABLE,
                    AccountType.ROTH,
                    AccountType.CORPORATE
                },
            };

            var BD = new Exposure
            {
                Class = AssetClass.Bond,
                Location = AssetLocation.Domestic,
                Target = totalBonds * (decimal)target.DomesticBondRatio,
                AccountTypesPreference = new[] {
                    AccountType.CORPORATE,
                    AccountType.TAXABLE,
                    AccountType.ROTH,
                },
            };

            var BI = new Exposure
            {
                Class = AssetClass.Bond,
                Location = AssetLocation.International,
                Target = totalBonds * (decimal)target.InternationalBondRatio,
                AccountTypesPreference = new[] {
                    AccountType.TAXABLE,
                    AccountType.CORPORATE,
                    AccountType.ROTH
                },
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
                foreach (var p in a.Positions.Where(x => x.Hold))
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
            score /= bestTotalScore;

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
                Warnings = warnings,
                Errors = errors,
            };
        }
    }
}
