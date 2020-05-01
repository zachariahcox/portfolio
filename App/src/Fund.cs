﻿using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
        [DataMember(IsRequired = true)]
        public string Symbol { get; set; }

        [DataMember(IsRequired = true)]
        public string Description { get; set; }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Brokerage { get; set; }

        [DataMember(IsRequired = true)]
        public double ExpenseRatio { get; set; } = 0.0;

        [DataMember(IsRequired = false)]
        public virtual double DomesticRatio { get; set; } = 1.0;

        [IgnoreDataMember]
        public virtual double InternationalRatio => 1.0 - DomesticRatio;

        [DataMember(IsRequired = false)]
        public virtual double StockRatio { get; set; } = 1.0;

        [IgnoreDataMember]
        public virtual double BondRatio => 1.0 - StockRatio;

        internal double Ratio(Exposure e)
        {
            return Ratio(e.Class) * Ratio(e.Location);
        }

        internal double Ratio(AssetClass c)
        {
            switch(c)
            {
                case AssetClass.Stock: return StockRatio;
                case AssetClass.Bond: return BondRatio;
                default: return 0.0;
            }
        }

        internal double Ratio(AssetLocation l)
        {
            switch(l)
            {
                case AssetLocation.Domestic: return DomesticRatio;
                case AssetLocation.International: return InternationalRatio;
                default: return 0.0;
            }
        }

        public override string ToString()
        {
            return $"{Symbol}, er: {ExpenseRatio}, sr: {StockRatio}, dr: {DomesticRatio}";
        }

        public static ConcurrentBag<Fund> FromYaml(string yaml)
        {
            if (string.IsNullOrEmpty(yaml))
            {
                return null;
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            Add(deserializer.Deserialize<IList<Fund>>(yaml));

            return Cache;
        }

        public static ConcurrentBag<Fund> LoadDefaultFunds()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "App.src.data.funds.yaml";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .Build();

                var list = deserializer.Deserialize<List<Fund>>(reader.ReadToEnd());
                var result = new ConcurrentBag<Fund>(list);
                return result;
            }
        }

        public static Fund Get(string symbol)
        {
            var result = Cache.FirstOrDefault(x => x.Symbol == symbol);
            if (result == null)
            {
                // found new product apparently?
                if (symbol == Cash.CASH)
                    result = new Cash();
                else 
                {
                    // TODO load stats from service? 
                    result = new Fund
                    {
                        Symbol = symbol,
                        StockRatio = 1,
                        DomesticRatio = 1,
                        ExpenseRatio = 0,
                        Description = "domestic stock?"
                    };
                }
                Cache.Add(result);
            }
            return result;
        }

        public static IEnumerable<Fund> GetFunds(string brokerage)
        {
            // use cache
            if (!CacheByBrokerage.TryGetValue(brokerage, out var fundsAtBrokerage))
            {
                fundsAtBrokerage = Cache?
                    .Where(x => x.Symbol == Cash.CASH
                        || string.Equals(x.Brokerage, 
                                         brokerage, 
                                         StringComparison.OrdinalIgnoreCase))
                    .OrderBy(x => x.Symbol)
                    .ToArray();

                CacheByBrokerage[brokerage] = fundsAtBrokerage;
            }
            return fundsAtBrokerage;
        }

        public static void Add(Fund f)
        {
            if (!Cache.Contains(f))
            {
                Cache.Add(f);
            }
        }

        public static void Add(IEnumerable<Fund> funds)
        {
            if (funds != null)
            {
                foreach (var f in funds)
                {
                    Add(f);
                }
            }
        }

        public static ConcurrentBag<Fund> Cache
        {
            get;
        } = LoadDefaultFunds();

        public static ConcurrentDictionary<string, IList<Fund>> CacheByBrokerage
        {
            get;
        } = new ConcurrentDictionary<string, IList<Fund>>();
    }

    public class Cash : Fund 
    {
        public static string CASH = "CASH";

        public Cash()
        {
            Symbol = CASH;
            ExpenseRatio = 0;
        }

        public override double DomesticRatio => 0;
        public override double InternationalRatio => 0;
        public override double StockRatio => 0;
        public override double BondRatio => 0;
    }
}