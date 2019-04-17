using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using PortfolioPicker.App.Strategies;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PortfolioPicker.App
{
    public class Picker
    {
        public Portfolio Portfolio { get; private set; }

        public Strategy Strategy { get; private set; }

        static public Picker Create(
            IReadOnlyList<Account> accounts,
            IReadOnlyList<Fund> funds = null,
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
            return this.Strategy.Rebalance(Portfolio);
        }

        private Picker(
            Portfolio portfolio,
            Strategy strategy)
        {
            this.Portfolio = portfolio;
            this.Strategy = strategy ?? new FourFundStrategy();
        }
    }
}
