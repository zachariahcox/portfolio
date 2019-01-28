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
        public Strategy Strategy { get; private set; }

        public IReadOnlyList<Account> Accounts { get; private set; }

        public IReadOnlyList<Fund> Funds { get; private set; }

        static public Picker Create(
            IReadOnlyList<Account> accounts,
            IReadOnlyList<Fund> funds = null,
            Strategy strategy = null)
        {
            return new Picker(accounts, funds, strategy);
        }

        static public Picker Create(
            string accountsYaml = null,
            string fundsYaml = null,
            string strategyName = null)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            var accounts = string.IsNullOrEmpty(accountsYaml)
                ? null
                : deserializer.Deserialize<IList<Account>>(accountsYaml);

            var funds = string.IsNullOrEmpty(fundsYaml)
                ? null
                : deserializer.Deserialize<IList<Fund>>(fundsYaml);

            // load strategy
            strategyName = strategyName ?? "FourFundStrategy";
            var strategyType = Type.GetType("PortfolioPicker.App.Strategies." + strategyName);
            var strategy = Activator.CreateInstance(strategyType) as Strategy;

            return Create(
                accounts: accounts as IReadOnlyList<Account>,
                funds: funds as IReadOnlyList<Fund>,
                strategy: strategy);
        }

        /// <summary>
        /// follow a strategy to produce buy orders
        /// </summary>
        public Portfolio Pick()
        {
            return this.Strategy.Perform(Accounts, Funds);
        }

        private Picker(
            IReadOnlyList<Account> accounts,
            IReadOnlyList<Fund> funds,
            Strategy strategy)
        {
            this.Accounts = accounts.OrderBy(a => a.Name).ToList();
            this.Funds = funds ?? LoadDefaultFunds();
            this.Strategy = strategy ?? new FourFundStrategy();
        }

        static private IReadOnlyList<Fund> LoadDefaultFunds()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "PortfolioPicker.App.data.funds.yaml";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

                var funds = deserializer.Deserialize<List<Fund>>(reader.ReadToEnd());
                funds.Sort((x, y) => x.Symbol.CompareTo(y.Symbol));
                return funds as IReadOnlyList<Fund>;
            }
        }
    }
}
