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

        public static IReadOnlyDictionary<string, IReadOnlyList<Fund>> Funds { get; private set; }

        public Strategy Strategy { get; private set; }

        public Picker(
            IReadOnlyList<Account> accounts, 
            string strategyName)
        {
            this.Accounts = accounts;

            // load strategy
            if (string.IsNullOrEmpty(strategyName))
            {
                strategyName = "FourFundStrategy";
            }
            var strategyType = Type.GetType("PortfolioPicker.App.Strategies." + strategyName);
            this.Strategy = Activator.CreateInstance(strategyType) as Strategy;

            // ensure loaded funds data
            if (Funds == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "PortfolioPicker.App.data.funds.json";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream))
                {
                    var d = JsonConvert.DeserializeObject<IDictionary<string, IReadOnlyList<Fund>>>(reader.ReadToEnd());
                    Funds = d as IReadOnlyDictionary<string, IReadOnlyList<Fund>>;
                }
            }
        }

        public Picker(string accountsData, string strategyName)
            : this(JsonConvert.DeserializeObject<IList<Account>>(accountsData) as IReadOnlyList<Account>,
                   strategyName)
        {
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
    }
}
