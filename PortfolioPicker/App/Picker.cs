using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PortfolioPicker.App
{
    public class Picker
    {
        public IReadOnlyList<Account> Accounts { get; private set; }

        public static IDictionary<string, IList<Fund>> Funds { get; private set; }

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
                    Funds = JsonConvert.DeserializeObject<IDictionary<string, IList<Fund>>>(reader.ReadToEnd());
                }
            }
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
