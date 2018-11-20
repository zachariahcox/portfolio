using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PortfolioPicker
{
    public class Data
    {
        public IReadOnlyDictionary<string, IReadOnlyList<string>> brokerage_defaults;
        public IReadOnlyList<Account> accounts;

        public IReadOnlyList<Fund> GetBrokerageDefault(string name)
        {
            //var brokerage_name = name.ToLower();
            //if (brokerage_defaults.TryGetValue(brokerage_name, out var found_value))
            //    return GetFunds(found_value);
            return new List<Fund>();
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

            Data rc = null;
            using (var r = System.IO.File.OpenText(fully_qualified_path))
            {
                rc = JsonConvert.DeserializeObject<Data>(r.ReadToEnd());
            }

            // resolve funds referenced by accounts
            //foreach (var a in rc.accounts)
            //    a.ResolveFunds(rc);
            return rc;
        }
        public void Print()
        {
            // Debug parsing
            foreach (var a in GetAccounts())
                Console.WriteLine(a);
        }
    }
}
