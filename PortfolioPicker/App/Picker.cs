using System;
using System.Collections.Generic;
using System.Linq;
using PortfolioPicker.App.Strategies;

namespace PortfolioPicker.App
{
    public class Picker
    {
        public Portfolio Portfolio { get; private set; }

        public Strategy Strategy { get; private set; }

        static public Picker Create(
            IList<Account> accounts,
            IList<Fund> funds = null,
            Strategy strategy = null)
        {
            var portfolio = new Portfolio { Accounts = accounts };
            strategy = strategy ?? new FourFundStrategy();
            strategy.Funds = funds;
            return new Picker(portfolio, strategy);
        }

        static public Picker Create(
            string accountsYaml = null,
            string fundsYaml = null,
            string strategyName = null)
        {
            var portfolio = Portfolio.FromYaml(accountsYaml);

            // load strategy
            strategyName = strategyName ?? "FourFundStrategy";
            var strategyType = Type.GetType("PortfolioPicker.App.Strategies." + strategyName);
            var strategy = Activator.CreateInstance(strategyType) as Strategy;
            strategy.Funds = Fund.FromYaml(fundsYaml);

            return new Picker(portfolio, strategy);
        }

        /// <summary>
        /// follow a strategy to produce positions
        /// </summary>
        public Portfolio Rebalance()
        {
            var result = Strategy.Rebalance(Portfolio);
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
                    foreach (var p in newA.Positions) { symbols.Add(p.Symbol);}
                    foreach (var p in oldA.Positions) { symbols.Add(p.Symbol); }
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

        private Picker(
            Portfolio portfolio,
            Strategy strategy)
        {
            this.Portfolio = portfolio;
            this.Strategy = strategy ?? new FourFundStrategy();
        }
    }

    public class Order
    {
        public static Order Create(
            string accountName, 
            string symbol, 
            decimal value)
        {
            if (value == 0m)
            {
                return null;
            }
            return new Order
            {
                AccountName = accountName,
                Symbol = symbol,
                Value = Math.Abs(value),
                Action = value < 0 ? "sell" : "buy",
            };
        }

        public string AccountName { get; set; }

        public string Symbol { get; set; }
        
        public decimal Value { get; set; }

        public string Action { get; set; }
    }
}
