using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioPicker
{
    public class Data
    {
        public IReadOnlyList<Fund> funds;
        public IReadOnlyDictionary<string, IReadOnlyList<string>> brokerage_defaults;
        public IReadOnlyList<Account> accounts;

        public IReadOnlyList<Fund> GetBrokerageDefault(string name)
        {
            var brokerage_name = name.ToLower();
            if (brokerage_defaults.TryGetValue(brokerage_name, 
                                                          out IReadOnlyList<string> found_value))
                return GetFunds(found_value);
            return new List<Fund>();
        }
        public IReadOnlyList<Fund> GetFunds(IReadOnlyList<string> symbols = null)
        {
            if(symbols != null)
                return funds.Where(f=> symbols.Contains(f.symbol)).ToList();
            return funds;
        }
        public IReadOnlyList<Account> GetAccounts()
        { 
            return accounts;
        }
        public static Data Load(string fully_qualified_path)
        {
            if (!System.IO.File.Exists(fully_qualified_path))
            {
                Console.WriteLine(fully_qualified_path + " does not exist.");
                Environment.Exit(1);
            }
            using (var r = System.IO.File.OpenText(fully_qualified_path))
            {
                Data rc = JsonConvert.DeserializeObject<Data>(r.ReadToEnd());

                //
                // resolve funds referenced by accounts
                //
                foreach (var a in rc.accounts)
                    a.ResolveFunds(rc);
                return rc;
            }
        }
        public void Print()
        {
            // Debug parsing
            foreach (var f in GetFunds())
                Console.WriteLine(f);
            foreach (var a in GetAccounts())
                Console.WriteLine(a);
        }
    }
}
