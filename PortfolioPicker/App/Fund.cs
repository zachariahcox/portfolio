using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PortfolioPicker.App
{
    [DataContract]
    public class Fund
    {
        [DataMember(IsRequired =true)]
        public string Symbol { get; set; }

        [DataMember(IsRequired = true)]
        public string Description { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue =false)]
        public string Brokerage { get; set; }

        [DataMember(IsRequired = true)]
        public string Url { get; set; }

        [DataMember(IsRequired = true)]
        public double ExpenseRatio { get; set; } = -1.0;

        [DataMember(IsRequired = false)]
        public double DomesticRatio { get; set; } = 1.0;

        [IgnoreDataMember]
        public double InternationalRatio => 1.0 - DomesticRatio;

        [DataMember(IsRequired = false)]
        public double StockRatio { get; set; } = 1.0;

        [IgnoreDataMember]
        public double BondRatio => 1.0 - StockRatio;

        internal double Ratio(Exposure e) => Ratio(e.Class) * Ratio(e.Location);

        internal double Ratio(AssetClass c) => AssetClass.Stock == c ? StockRatio : BondRatio;

        internal double Ratio(AssetLocation l) => AssetLocation.Domestic == l ? DomesticRatio: InternationalRatio;

        public override string ToString()
        {
            return $"{Symbol}, er: {ExpenseRatio}, sr: {StockRatio}, dr: {DomesticRatio}";
        }

        public static IList<Fund> FromYaml(string yaml)
        {
            if (string.IsNullOrEmpty(yaml))
                return null;

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            foreach (var f in deserializer.Deserialize<IList<Fund>>(yaml))
                Add(f);

            return Cache;
        }

        public static IList<Fund> LoadDefaultFunds()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "PortfolioPicker.App.data.funds.yaml";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .Build();

                return deserializer.Deserialize<List<Fund>>(reader.ReadToEnd());
            }
        }

        public static Fund Get(string symbol)
        {
            var result = Cache.FirstOrDefault(x => x.Symbol == symbol);
            if (result == null)
            {
                // found new product apparently? 
                // TODO load stats from service? 
                result = new Fund
                {
                    Symbol = symbol,
                    StockRatio = 1,
                    DomesticRatio = 1,
                    ExpenseRatio = 0,
                };
                Cache.Add(result);
            }
            return result;
        }

        public static IList<Fund> GetFunds(string brokerage)
        {
            // use cache
            if (!CacheByBrokerage.TryGetValue(brokerage, out var fundsAtBrokerage))
            {
                fundsAtBrokerage = Cache?
                    .Where(x => string.Equals(x.Brokerage, brokerage, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.Symbol)
                    .ToList();

                CacheByBrokerage[brokerage] = fundsAtBrokerage;
            }
            return fundsAtBrokerage;
        }

        public static void Add(Fund f)
        {
            if (!Cache.Contains(f))
                Cache.Add(f);
        }

        public static void AddRange(IEnumerable<Fund> funds)
        {
            if (funds != null)
            {
                foreach (var f in funds)
                {
                    Add(f);
                }
            }
        }

        public static IList<Fund> Cache 
        { 
            get; 
        } = LoadDefaultFunds();

        public static Dictionary<string, IList<Fund>> CacheByBrokerage 
        {
            get; 
        } = new Dictionary<string, IList<Fund>>();
    }
}