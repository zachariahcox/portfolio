using System.Collections.Generic;
using System.IO;
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

        [DataMember(IsRequired =false, EmitDefaultValue =false)]
        public string Exposure { get; set; }

        internal double Ratio(Exposure e) => Ratio(e.Class) * Ratio(e.Location);

        internal double Ratio(AssetClass c) => AssetClass.Stock == c ? StockRatio : BondRatio;

        internal double Ratio(AssetLocation l) => AssetLocation.Domestic == l ? DomesticRatio: InternationalRatio;

        public override string ToString()
        {
            return $"{Symbol}, er: {ExpenseRatio}, sr: {StockRatio}, dr: {DomesticRatio}";
        }

        public static IList<Fund> FromYaml(string yaml)
        {
            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();

            return string.IsNullOrEmpty(yaml)
                ? null
                : deserializer.Deserialize<IList<Fund>>(yaml);
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
    }
}