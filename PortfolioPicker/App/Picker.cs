using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reflection;

namespace PortfolioPicker.App
{
    public class Picker
    {
        public IReadOnlyList<Account> Accounts { get; private set; }

        /// <summary>
        /// map of brokerage name to list of approved funds
        /// </summary>
        public IReadOnlyDictionary<string, IReadOnlyList<Fund>> Funds { get; private set; }

        public Strategy Strategy { get; private set; }

        static public Picker Create(
            IReadOnlyList<Account> accounts,
            string strategyName)
        {
            return Create(accounts, null, strategyName);
        }

        static public Picker Create(string accountsJson, string strategyName)
        {
            return Create(accountsJson, null, strategyName);
        }

        static public Picker Create(
            IReadOnlyList<Account> accounts,
            IReadOnlyDictionary<string, IReadOnlyList<Fund>> brokerageToFundMap,
            string strategyName)
        {
            return new Picker(accounts, brokerageToFundMap, strategyName);
        }

        static public Picker Create(
            string accountsJson,
            string brokerageToFundMapJson,
            string strategyName)
        {
            var map = string.IsNullOrEmpty(brokerageToFundMapJson)
                ? null
                : JsonConvert.DeserializeObject<IReadOnlyDictionary<string, IReadOnlyList<Fund>>>(brokerageToFundMapJson);

            return new Picker(
                accounts: JsonConvert.DeserializeObject<IReadOnlyList<Account>>(accountsJson),
                brokerageToFundMap: map,
                strategyName: strategyName);
        }

        public Portfolio Pick()
        {
            // follow a strategy to produce buy orders
            var portfolio = this.Strategy.Perform(Accounts, Funds);
            Console.WriteLine("Buy Orders:");
            foreach (var o in portfolio.BuyOrders)
                Console.WriteLine("\t" + o);
            return portfolio;
        }

        private Picker(
            IReadOnlyList<Account> accounts, 
            IReadOnlyDictionary<string, IReadOnlyList<Fund>> brokerageToFundMap,
            string strategyName)
        {
            this.Accounts = accounts.OrderBy(a => a.Name).ToList();
            this.Funds = brokerageToFundMap ?? LoadDefaultFunds();

            // load strategy
            if (string.IsNullOrEmpty(strategyName))
            {
                strategyName = "FourFundStrategy";
            }
            var strategyType = Type.GetType("PortfolioPicker.App.Strategies." + strategyName);
            this.Strategy = Activator.CreateInstance(strategyType) as Strategy;
        }

        static private IReadOnlyDictionary<string, IReadOnlyList<Fund>> LoadDefaultFunds()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "PortfolioPicker.App.data.funds.json";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                var d = JsonConvert.DeserializeObject<IDictionary<string, List<Fund>>>(reader.ReadToEnd());
                return d.ToDictionary(
                    p => p.Key,
                    p => {
                            // stabilize fund order
                            p.Value.Sort((x, y) => x.Symbol.CompareTo(y.Symbol));
                        return p.Value as IReadOnlyList<Fund>;
                    });
            }
        }
    }
}
