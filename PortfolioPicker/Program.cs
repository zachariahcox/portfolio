﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace PortfolioPicker
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            // load configuration file
            if (args.Length < 1)
            {
                Console.WriteLine("ERROR: no data file provided. Please provide a the path to the data json file.");
                Environment.Exit(1);
            }
            var data = Data.Load(args[0]);
#if DEBUG
            data.Print();
#endif
            // load funds data
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "PortfolioPicker.data.funds.json";
            IReadOnlyDictionary<string, IReadOnlyCollection<Fund>> fundsByBrokerage = null;
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                fundsByBrokerage = JsonConvert.DeserializeObject<IReadOnlyDictionary<string, IReadOnlyCollection<Fund>>>(reader.ReadToEnd());
            }

            // Load strategy
            var strategy_name = args.Length < 2 ? "FourFundStrategy" : args[1];
            var strategy_type = Type.GetType("PortfolioPicker.Strategies." + strategy_name);
            var strategy = (Strategy)Activator.CreateInstance(strategy_type);

            // follow a strategy to produce buy orders
            var portfolio = strategy.Perform(data.accounts, funds: fundsByBrokerage);
            Console.WriteLine("Buy Orders:");
            foreach (var o in portfolio.buy_orders)
                Console.WriteLine("\t" + o);
        }
    }
}
