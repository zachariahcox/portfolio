using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PortfolioPicker.App
{
    public class Picker
    {
        public IReadOnlyList<Account> Accounts { get; private set; }

        public IReadOnlyList<Fund> Funds { get; private set; }

        public Strategy Strategy { get; private set; }

        static public Picker Create(
            IReadOnlyList<Account> accounts,
            string strategyName)
        {
            return Create(accounts, null, strategyName);
        }

        static public Picker Create(
            string accountsYaml, 
            string strategyName)
        {
            return Create(accountsYaml, null, strategyName);
        }

        static public Picker Create(
            IReadOnlyList<Account> accounts,
            IReadOnlyList<Fund> funds,
            string strategyName)
        {
            return new Picker(accounts, funds, strategyName);
        }

        static public Picker Create(
            string accountsYaml,
            string fundsYaml,
            string strategyName)
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

            return new Picker(
                accounts: accounts as IReadOnlyList<Account>,
                funds: funds as IReadOnlyList<Fund>,
                strategyName: strategyName);
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
            string strategyName)
        {
            this.Accounts = accounts.OrderBy(a => a.Name).ToList();
            this.Funds = funds ?? LoadDefaultFunds();

            // load strategy
            strategyName = strategyName ?? "FourFundStrategy";
            var strategyType = Type.GetType("PortfolioPicker.App.Strategies." + strategyName);
            this.Strategy = Activator.CreateInstance(strategyType) as Strategy;
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
