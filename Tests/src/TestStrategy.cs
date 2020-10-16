using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Text.Json;
using PortfolioPicker.App;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PortfolioPicker.Tests
{
    public class TestStrategy
    {
        private static SecurityCache _sc;

        public static SecurityCache GetSecurityCache()
        {
            if (_sc is null)
            {
                _sc = new SecurityCache();
                _sc.Add(Security.LoadDefaults());
            }
            return _sc;
        }

        private string GetDataFilePath(string relativePath)
        {
            var codeBaseUrl = new Uri(Assembly.GetExecutingAssembly().CodeBase);
            var codeBasePath = Uri.UnescapeDataString(codeBaseUrl.AbsolutePath);
            var dirPath = Path.GetDirectoryName(codeBasePath);
            dirPath = Path.GetDirectoryName(dirPath);
            dirPath = Path.GetDirectoryName(dirPath);
            dirPath = Path.GetDirectoryName(dirPath);
            return Path.Combine(dirPath, relativePath);
        }

        private Account CreateAccount(
            string brokerage,
            AccountType type,
            string symbol = "VTSAX",
            double value = 100.0)
        {
            return new Account
            {
                Brokerage = brokerage,
                Name = $"My {brokerage} account",
                Type = type,
                Positions = new List<Position>
                {
                    new Position
                    {
                        Symbol = symbol,
                        Value = value
                    }
                }
            };
        }

        private Security CreateSecurity(
            string brokerage,
            string symbol,
            double er,
            double stock = 1.0,
            double domestic = 1.0)
        {
            return new Security
            {
                Symbol = symbol,
                Brokerage = brokerage,
                Description = $"{symbol}@{brokerage} ({er})",
                ExpenseRatio = er,
                StockRatio = stock,
                DomesticRatio = domestic
            };
        }


        [Fact]
        public void JustVanguard()
        {
            var accounts = new List<Account>
            {
                new Account
                {
                    Name ="roth",
                    Brokerage="Vanguard",
                    Type=AccountType.ROTH,
                    Positions = new List<Position>
                    {
                        new Position
                        {
                            Symbol = "FZROX",
                            Value = 100
                        }
                    }
                },
            };

            var portfolio = Picker.Rebalance(new Portfolio(accounts, GetSecurityCache()), .9, .6, .7, iterationLimit: 1, threadLimit: 1);
            Assert.Equal(4, portfolio.NumberOfPositions);
            Assert.Equal(100, portfolio.TotalValue);
        }

        [Fact]
        public void RebalanceDoesNotChangeTotalValue()
        {
            var yaml = @"
- name: Roth
  brokerage: Vanguard
  type: ROTH
  positions:
    - symbol: VTSAX
      value: 100
    - symbol: VTIAX
      value: 200
      hold: true";

            var o = Portfolio.FromYaml(yaml, GetSecurityCache());
            var portfolio = Picker.Rebalance(o,  0, 1, 1, iterationLimit: 1, threadLimit: 1);
            var actualValue = portfolio.Positions.Sum(o => o.Value);
            Assert.Equal(300, actualValue);
        }
        
        [Fact]
        public void YamlSerialization()
        {
            var expected = @"- name: roth
  brokerage: vanguard
  type: ROTH
  positions:
  - symbol: vtsax
    value: 100
  - symbol: vtiax
    value: 200
    hold: true
".Replace("\r\n", "\n");

            var p = Portfolio.FromYaml(expected);
            var actual = p.ToYaml();
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MarkdownSerialization()
        {
            var yaml = File.ReadAllText(GetDataFilePath("src/MarkdownSerialization/portfolio.yml"));
            var p = Portfolio.FromYaml(yaml);
            var expectedFile = GetDataFilePath("src/MarkdownSerialization/load.md");
            var actual = string.Join("\n", p.ToMarkdown());
            
            // uncomment to update
            // File.WriteAllText(expectedFile, actual);
            
            var expected = File.ReadAllText(expectedFile).Replace("\r\n", "\n");
            Assert.Equal(expected, actual);
        }

        // [Fact]
        // public void MarkdownSerialization_rebalance()
        // {
        //     var yaml = File.ReadAllText(GetDataFilePath("MarkdownSerialization/portfolio.yml"));
        //     var original = Portfolio.FromYaml(yaml);
        //     var rebalance = Picker.Rebalance(original, .5, .5, .5);
        //     var expectedFile = GetDataFilePath("MarkdownSerialization/rebalance.md");
        //     var actual = string.Join("\n", rebalance.ToMarkdown(reference: original));
        //     // uncomment to update
        //     // File.WriteAllText(expectedFile, actual);
        //     var expected = File.ReadAllText(expectedFile);
        //     Assert.Equal(expected, actual);
        // }

        [Fact]
        public void FromYaml()
        {
            var fundsYaml = @"
- description: Vanguard Total Stock Market Index Fund
  symbol: VTSAX
  brokerage: Vanguard
  expenseRatio: 0.04
  stockRatio: 1
  domesticRatio: 1

- description: Vanguard Total International Stock Index Fund
  symbol: VTIAX
  brokerage: Vanguard
  expenseRatio: 0.11
  stockRatio: 1
  domesticRatio: 0";

            var accountsYaml = @"
- name: Roth
  brokerage: Vanguard
  type: ROTH
  positions:
  - symbol: VTSAX
    value: 100
- name: Other
  brokerage: Vanguard
  type: BROKERAGE
  positions:
  - symbol: VTSAX
    value: 100";

            var deserializer = new DeserializerBuilder()
            .WithNamingConvention(new CamelCaseNamingConvention())
            .Build();

            var funds = deserializer.Deserialize<IList<Security>>(fundsYaml);
            Assert.Equal(2, funds.Count);

            var accounts = deserializer.Deserialize<IList<Account>>(accountsYaml);
            Assert.Equal(2, accounts.Count);

            var p = Portfolio.FromYaml(accountsYaml);
            Assert.NotNull(p);
        }

        [Fact]
        public void FromYaml_duplicate_position()
        {
            // this yaml has two positions with the same symbol
            var accountsYaml = @"
- name: test account
  brokerage: Vanguard
  type: ROTH
  positions:
  - symbol: VTSAX
    value: 100
  - symbol: VTSAX
    value: 100 ";
            Assert.ThrowsAny<Exception>(() => Portfolio.FromYaml(accountsYaml));
        }

        [Fact]
        public void Complex()
        {
            var brokerages = new []{"a", "b", "c"};
            var accounts = new List<Account>();
            var value = 10000;
            foreach(var b in brokerages)
            foreach (var t in AccountTypes.ALL)
                accounts.Add(CreateAccount(b, t, symbol: Cash.CASH, value: value));

            var symbols = new []{"m", "n"};
            var expenseRatio = 0.05;
            var securities = new List<Security>();
            foreach (var b in brokerages)
            foreach (var s in symbols)
                securities.Add(CreateSecurity(b, s, expenseRatio, 1, 1));
            
            var sc = GetSecurityCache();
            sc.Add(securities);

            var expectedTotal = accounts.Count * value;
            var portfolio = Picker.Rebalance(new Portfolio(accounts, sc), 1, 1, 1, iterationLimit: 1, threadLimit: 1);
            Assert.Equal(
                brokerages.Length * AccountTypes.ALL.Length, 
                portfolio.NumberOfPositions);
            Assert.Equal(expenseRatio, portfolio.ExpenseRatio);
            Assert.Equal(expectedTotal, portfolio.TotalValue);
        }

        [Fact]
        public void OneAccountFourPerfectSecuritys()
        {
            var brokerage = "OneAccountFourPerfectSecuritys";
            var dollars = 100m;
            var accounts = new List<Account>{
                CreateAccount(brokerage, AccountType.BROKERAGE, value: (double)dollars)
            };
            var funds = new List<Security>{
                CreateSecurity(brokerage, "sd", 0, 1, 1),
                CreateSecurity(brokerage, "si", 0, 1, 0),
                CreateSecurity(brokerage, "bd", 0, 0, 1),
                CreateSecurity(brokerage, "bi", 0, 0, 0),
            };

            var sc = new SecurityCache();
            sc.Add(funds);
            var original = new Portfolio(accounts, sc);
            var p = Picker.Rebalance(original, .5, .5, .5, iterationLimit: 1, threadLimit: 1);

            // funds should be equally split
            Assert.NotNull(p);
            Assert.Equal(0.925, p.Score.Total); // less than one due to account being always brokerage
            Assert.Equal(4, p.NumberOfPositions);
            Assert.Equal(0.25m * dollars, (decimal)p.Positions.First(x => x.Symbol == "sd").Value);
            Assert.Equal(0.25m * dollars, (decimal)p.Positions.First(x => x.Symbol == "si").Value);
            Assert.Equal(0.25m * dollars, (decimal)p.Positions.First(x => x.Symbol == "bd").Value);
            Assert.Equal(0.25m * dollars, (decimal)p.Positions.First(x => x.Symbol == "bi").Value);

            // output percentages should match input requests
            Assert.Equal(50, p.PercentOfPortfolio(AssetClass.Stock));
            Assert.Equal(50, p.PercentOfPortfolio(AssetClass.Bond));
            Assert.Equal(25, p.PercentOfPortfolio(AssetClass.Stock, AssetLocation.Domestic));
            Assert.Equal(25, p.PercentOfPortfolio(AssetClass.Bond, AssetLocation.Domestic));

            // ==================================
            // Change stock ratio
            p = Picker.Rebalance(original, .9, 1, 1, iterationLimit: 1, threadLimit: 1);
            Assert.NotNull(p);
            Assert.Equal(0.775, p.Score.Total); // less than one due to account being always brokerage
            Assert.Equal(2, p.NumberOfPositions);
            Assert.Equal(90m, (decimal)p.Positions.First(x => x.Symbol == "sd").Value);
            Assert.Equal(10m, (decimal)p.Positions.First(x => x.Symbol == "bd").Value);
            Assert.Equal(90m, (decimal)p.PercentOfPortfolio(AssetClass.Stock));
            Assert.Equal(10m, (decimal)p.PercentOfPortfolio(AssetClass.Bond));
            Assert.Equal(90m, (decimal)p.PercentOfPortfolio(AssetClass.Stock, AssetLocation.Domestic));
            Assert.Equal(10m, (decimal)p.PercentOfPortfolio(AssetClass.Bond, AssetLocation.Domestic));

            // ==================================
            // Change domestic ratio
            p = Picker.Rebalance(original, .5, .9, .1, iterationLimit: 1, threadLimit: 1);
            Assert.NotNull(p);
            Assert.Equal(0.925, p.Score.Total); // less than one due to account being always brokerage
            Assert.Equal(4, p.NumberOfPositions);
            Assert.Equal(.5m * 90m, (decimal)p.Positions.First(x => x.Symbol == "sd").Value);
            Assert.Equal(.5m * 10m, (decimal)p.Positions.First(x => x.Symbol == "si").Value);
            Assert.Equal(.5m * 10m, (decimal)p.Positions.First(x => x.Symbol == "bd").Value);
            Assert.Equal(.5m * 90m, (decimal)p.Positions.First(x => x.Symbol == "bi").Value);
            Assert.Equal(50, p.PercentOfPortfolio(AssetClass.Stock));
            Assert.Equal(50, p.PercentOfPortfolio(AssetClass.Bond));
            Assert.Equal(45, p.PercentOfPortfolio(AssetClass.Stock, AssetLocation.Domestic));
            Assert.Equal(5, p.PercentOfPortfolio(AssetClass.Bond, AssetLocation.Domestic));
        }

        [Fact]
        public void OneAccountFourEqualSecuritys_IgnoreWorseER()
        {
            var brokerageName = "OneAccountFourEqualSecuritys_IgnoreWorseER";
            var accounts = new List<Account>{
                CreateAccount(brokerageName, AccountType.BROKERAGE, value: 100)
            };
            var funds = new List<Security>{
                CreateSecurity(brokerageName, "sd", .5, 1, 1), // should be ignored, worse ER
                CreateSecurity(brokerageName, "zzsd", 0, 1, 1), // should be ignored, alphabetically sorted
                CreateSecurity(brokerageName, "sd", 0, 1, 1),
                CreateSecurity(brokerageName, "si", 0, 1, 0),
                CreateSecurity(brokerageName, "bd", 0, 0, 1),
                CreateSecurity(brokerageName, "bi", 0, 0, 0),
            };

            var sc = new SecurityCache();
            sc.Add(funds);

            var original = new Portfolio(accounts, sc);
            var p = Picker.Rebalance(original, .5, .5, .5, iterationLimit: 1, threadLimit: 1);

            // funds should be equally split
            Assert.NotNull(p);
            Assert.Equal(0.925, p.Score.Total);
            Assert.Equal(4, p.NumberOfPositions);
            Assert.Equal(25, p.Positions.First(x => x.Symbol == "sd").Value);
            Assert.Equal(25, p.Positions.First(x => x.Symbol == "si").Value);
            Assert.Equal(25, p.Positions.First(x => x.Symbol == "bd").Value);
            Assert.Equal(25, p.Positions.First(x => x.Symbol == "bi").Value);

            // output percentages should match input requests
            Assert.Equal(50, p.PercentOfPortfolio(AssetClass.Stock));
            Assert.Equal(50, p.PercentOfPortfolio(AssetClass.Bond));
            Assert.Equal(50, p.PercentOfPortfolio(AssetLocation.Domestic));
            Assert.Equal(50, p.PercentOfPortfolio(AssetLocation.Domestic));
        }

        [Fact]
        public void ThreeAccountsOneSecurity()
        {
            var b = "ThreeAccountsOneSecurity";
            var symbolName = "generic";
            var accounts = new List<Account>{
                CreateAccount(b, AccountType.BROKERAGE, value: 100),
                CreateAccount(b, AccountType.ROTH, value: 100),
                CreateAccount(b, AccountType.IRA, value: 100),
            };
            var funds = new List<Security>{
                CreateSecurity(b, symbolName, 0, 0.5, 0.5),
            };

            var sc = new SecurityCache();
            sc.Add(funds);

            var original = new Portfolio(accounts, sc);
            var p = Picker.Rebalance(original, 0.5, 0.5, 0.5, iterationLimit: 1, threadLimit: 1);

            // assert portfolio correctness
            Assert.NotNull(p);
            foreach (var o in p.Positions)
            {
                // one fund per account, spend all the money there
                Assert.Equal(symbolName, o.Symbol);
                Assert.Equal(100, o.Value);
            }
            Assert.Equal(100 * accounts.Count, p.TotalValue);
            Assert.Equal(accounts.Count, p.NumberOfPositions);

            Assert.InRange(p.Score.Total, .75, 1); 
            
            // output percentages should match input requests
            Assert.Equal(50, p.PercentOfPortfolio(AssetClass.Stock));
            Assert.Equal(50, p.PercentOfPortfolio(AssetClass.Bond));
            Assert.Equal(50, p.PercentOfPortfolio(AssetLocation.Domestic));
            Assert.Equal(50, p.PercentOfPortfolio(AssetLocation.International));

            foreach (var c in AssetClasses.ALL)
            foreach (var l in AssetLocations.ALL)
            {
                if (c == AssetClass.None || l == AssetLocation.None)
                    continue; // tested above

                Assert.Equal(25, p.PercentOfPortfolio(c, l));
            }
        }

        [Fact]
        public void ReduceTax()
        {
            var b = "ReduceTax";
            var symbolName = "Generic";
            var accounts = new List<Account>{
                CreateAccount(b, AccountType.BROKERAGE, value: 100),
                CreateAccount(b, AccountType.ROTH, value: 100),
                CreateAccount(b, AccountType.IRA, value: 100),
            };
            var funds = new List<Security>{
                CreateSecurity(b, symbolName, 0, 0.5, 1),
                CreateSecurity(b, symbolName, 0, 0, 1),
            };
            var sc = new SecurityCache();
            sc.Add(funds);
            
            var original = new Portfolio(accounts, sc);
            var p = Picker.Rebalance(original, 0.5, 0.5, 0.5, iterationLimit: 1, threadLimit: 1);

            var targetRatios = Picker.ComputeTargetRatios(.5, .5, .5);
            var os = original.GetScore(Score.GetScoreWeight, targetRatios);
        }

        [Fact]
        public void TestGoogleSheet(){
            var json = File.ReadAllText(GetDataFilePath("src/googlesheetexport/portfolio.json"));
            var p = Portfolio.FromGoogleSheet(json);
            Assert.NotNull(p);

            var gs = GoogleSheetPortfolio.FromJson(json);
            Assert.NotNull(gs.Accounts);
            Assert.NotNull(gs.Securities);
            Assert.NotNull(gs.RebalanceParameters);
        }

        [Fact]
        public void TestReportObject(){
            var b = "ReduceTax";
            var accounts = new List<Account>{
                CreateAccount(b, AccountType.BROKERAGE, value: 100),
                CreateAccount(b, AccountType.ROTH, value: 100),
                CreateAccount(b, AccountType.IRA, value: 100),
            };
            var funds = new List<Security>{
                CreateSecurity(b, ".5s_1d", er: 0, stock: 0.5, domestic: 1),
                CreateSecurity(b, "0s_.5d", er: 0, stock: 0, domestic: .5),
            };
            var sc = new SecurityCache();
            sc.Add(funds);
            var original = new Portfolio(accounts, sc);
            var rebalanced = Picker.Rebalance(original, .5, .5, .5, 1, 1);
            var report = rebalanced.ToReport();
            Assert.NotNull(report);
            var s = JsonSerializer.Serialize(report);
            Assert.NotNull(s);
        }
    }
}
