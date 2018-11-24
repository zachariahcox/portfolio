using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace PortfolioPicker
{
    public class FundsByBrokerageMap : Dictionary<string, List<Fund>> { }

    public class Data
    {
        public IReadOnlyList<Account> Accounts { get; set; }

        public static Data Load(string fully_qualified_path)
        {
            if (!File.Exists(fully_qualified_path))
            {
                Console.WriteLine(fully_qualified_path + " does not exist.");
                Environment.Exit(1);
            }

            Data rc = null;
            using (var r = File.OpenText(fully_qualified_path))
            {
                rc = JsonConvert.DeserializeObject<Data>(r.ReadToEnd());
            }

            return rc;
        }

        private static FundsByBrokerageMap _funds;
        public static FundsByBrokerageMap Funds()
        {
            if (_funds == null)
            {
                // load funds data
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "PortfolioPicker.data.funds.json";
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream))
                {
                    _funds = JsonConvert.DeserializeObject<FundsByBrokerageMap>(reader.ReadToEnd());
                }
            }
            return _funds;
        }

        public void Print()
        {
            foreach (var a in Accounts)
            {
                Console.WriteLine(a);
            }
        }
    }
}
