using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PortfolioPicker.App
{
    [DataContract]
    public class Security
    {
        [DataMember(IsRequired = true)]
        public string Symbol { 
            get => _symbol;
            set => _symbol = value is null ? null : value.ToLower().Trim();
        }

        [DataMember(IsRequired=false, EmitDefaultValue=false)]
        public string SymbolMap { 
            get => _symbolMap;
            set => _symbolMap = value is null ? null : value.ToLower().Trim();
        }

        [DataMember(IsRequired = true)]
        public string Description { 
            get => _description;
            set => _description = value is null ? null : value.ToLower().Trim();
            }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Brokerage {
            get => _brokerage;
            set => _brokerage = value is null ? null : value.ToLower().Trim();
            }

        [DataMember(IsRequired = false, EmitDefaultValue = false)]
        public string Url { get; set; }

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

        public static IList<Security> FromYaml(string yaml)
        {
            if (string.IsNullOrEmpty(yaml))
            {
                return null;
            }

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            return deserializer.Deserialize<IList<Security>>(yaml);
        }

        public static ConcurrentBag<Security> LoadDefaults()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "App.src.data.funds.yaml";
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            using (var reader = new StreamReader(stream))
            {
                var deserializer = new DeserializerBuilder()
                    .WithNamingConvention(new CamelCaseNamingConvention())
                    .Build();

                var list = deserializer.Deserialize<List<Security>>(reader.ReadToEnd());
                return new ConcurrentBag<Security>(list);
            }
        }

        [IgnoreDataMember]
        private string _symbol;
        [IgnoreDataMember]
        private string _symbolMap;
        [IgnoreDataMember]
        private string _description;
        [IgnoreDataMember]
        private string _brokerage;
    }

    public class Cash : Security 
    {
        public static string CASH = "cash";

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