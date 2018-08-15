using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioPicker
{
    class Data
    {
        //
        // static members for most of the application to reference. 
        //
        private static Data loadedData;

        public static IReadOnlyList<Fund> GetBrokerageDefault(string name)
        {
            var brokerage_name = name.ToLower();
            IReadOnlyList<string> found_value = null; // Wow! unfortunate output parameters!
            if (loadedData.brokerage_defaults.TryGetValue(brokerage_name, out found_value))
                return GetFunds(found_value);
            return new List<Fund>();
        }
        public static IReadOnlyList<Fund> GetFunds(IReadOnlyList<string> symbols = null)
        {
            if(symbols != null)
                return loadedData.funds.Where(f=> symbols.Contains(f.symbol)).ToList();
            return loadedData.funds;
        }
        public static IReadOnlyList<Account> GetAccounts()
        { 
            return loadedData.accounts;
        }
        public static void Load(string fully_qualified_path)
        {
            if (!System.IO.File.Exists(fully_qualified_path))
            {
                Console.WriteLine(fully_qualified_path + " does not exist.");
                Environment.Exit(1);
            }
            using (var r = System.IO.File.OpenText(fully_qualified_path))
            {
                Data.loadedData = JsonConvert.DeserializeObject<Data>(r.ReadToEnd());
            }
        }
        public static void Print()
        {
            // Debug parsing
            foreach (var f in Data.GetFunds())
                Console.WriteLine(f);
            foreach (var a in Data.GetAccounts())
                Console.WriteLine(a);
        }

        // 
        // populated by json parser
        //
        public IReadOnlyList<Fund> funds;
        public IReadOnlyDictionary<string, IReadOnlyList<string>> brokerage_defaults;
        public IReadOnlyList<Account> accounts;
    }
}
