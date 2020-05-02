using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Reflection;
using PortfolioPicker.App;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PortfolioPicker.Tests
{
    public class TestStrategy
    {
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

        private Fund CreateFund(
            string brokerage,
            string symbol,
            double er,
            double stock = 1.0,
            double domestic = 1.0)
        {
            return new Fund
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

            var portfolio = Picker.Rebalance(new Portfolio(accounts), .9, .6, .7, iterationLimit: 1, threadLimit: 1);
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

            var portfolio = Picker.Rebalance(Portfolio.FromYaml(yaml), 0, 1, 1, iterationLimit: 1, threadLimit: 1);
            var actualValue = portfolio.Positions.Sum(o => o.Value);
            Assert.Equal(300, actualValue);
        }
        
        [Fact]
        public void YamlSerialization()
        {
            var expected = @"- name: Roth
  brokerage: Vanguard
  type: ROTH
  positions:
  - symbol: VTSAX
    value: 100
  - symbol: VTIAX
    value: 200
    hold: true
";
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
            var expected = File.ReadAllText(expectedFile);
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

            var funds = deserializer.Deserialize<IList<Fund>>(fundsYaml);
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
            var funds = new List<Fund>();
            foreach (var b in brokerages)
            foreach (var s in symbols)
                funds.Add(CreateFund(b, s, expenseRatio, 1, 1));
            Fund.Add(funds);

            var expectedTotal = accounts.Count * value;
            var portfolio = Picker.Rebalance(new Portfolio(accounts), 1, 1, 1, iterationLimit: 1, threadLimit: 1);
            Assert.Equal(
                brokerages.Length * AccountTypes.ALL.Length, 
                portfolio.NumberOfPositions);
            Assert.Equal(expenseRatio, portfolio.ExpenseRatio);
            Assert.Equal(expectedTotal, portfolio.TotalValue);
        }

        [Fact]
        public void OneAccountFourPerfectFunds()
        {
            var brokerage = "OneAccountFourPerfectFunds";
            var dollars = 100m;
            var accounts = new List<Account>{
                CreateAccount(brokerage, AccountType.BROKERAGE, value: (double)dollars)
            };
            var funds = new List<Fund>{
                CreateFund(brokerage, "SD", 0, 1, 1),
                CreateFund(brokerage, "SI", 0, 1, 0),
                CreateFund(brokerage, "BD", 0, 0, 1),
                CreateFund(brokerage, "BI", 0, 0, 0),
            };

            Fund.Add(funds);
            var original = new Portfolio(accounts);
            var p = Picker.Rebalance(original, .5, .5, .5, iterationLimit: 1, threadLimit: 1);

            // funds should be equally split
            Assert.NotNull(p);
            Assert.InRange(p.Score, .8, .9); // less than one due to account being always brokerage
            Assert.Equal(4, p.NumberOfPositions);
            Assert.Equal(0.25m * dollars, (decimal)p.Positions.First(x => x.Symbol == "SD").Value);
            Assert.Equal(0.25m * dollars, (decimal)p.Positions.First(x => x.Symbol == "SI").Value);
            Assert.Equal(0.25m * dollars, (decimal)p.Positions.First(x => x.Symbol == "BD").Value);
            Assert.Equal(0.25m * dollars, (decimal)p.Positions.First(x => x.Symbol == "BI").Value);

            // output percentages should match input requests
            Assert.Equal(50, p.PercentOfPortfolio(AssetClass.Stock));
            Assert.Equal(50, p.PercentOfPortfolio(AssetClass.Bond));
            Assert.Equal(25, p.PercentOfPortfolio(AssetClass.Stock, AssetLocation.Domestic));
            Assert.Equal(25, p.PercentOfPortfolio(AssetClass.Bond, AssetLocation.Domestic));

            // ==================================
            // Change stock ratio
            p = Picker.Rebalance(original, .9, 1, 1, iterationLimit: 1, threadLimit: 1);
            Assert.NotNull(p);
            Assert.InRange(p.Score, .8, .9); // less than one due to account being always brokerage
            Assert.Equal(2, p.NumberOfPositions);
            Assert.Equal(90m, (decimal)p.Positions.First(x => x.Symbol == "SD").Value);
            Assert.Equal(10m, (decimal)p.Positions.First(x => x.Symbol == "BD").Value);
            Assert.Equal(90m, (decimal)p.PercentOfPortfolio(AssetClass.Stock));
            Assert.Equal(10m, (decimal)p.PercentOfPortfolio(AssetClass.Bond));
            Assert.Equal(90m, (decimal)p.PercentOfPortfolio(AssetClass.Stock, AssetLocation.Domestic));
            Assert.Equal(10m, (decimal)p.PercentOfPortfolio(AssetClass.Bond, AssetLocation.Domestic));

            // ==================================
            // Change domestic ratio
            p = Picker.Rebalance(original, .5, .9, .1, iterationLimit: 1, threadLimit: 1);
            Assert.NotNull(p);
            Assert.InRange(p.Score, .8, .9); // less than one due to account being always brokerage
            Assert.Equal(4, p.NumberOfPositions);
            Assert.Equal(.5m * 90m,
                         (decimal)p.Positions.First(x => x.Symbol == "SD").Value);
            Assert.Equal(.5m * 10m,
                         (decimal)p.Positions.First(x => x.Symbol == "SI").Value);
            Assert.Equal(.5m * 10m,
                         (decimal)p.Positions.First(x => x.Symbol == "BD").Value);
            Assert.Equal(.5m * 90m,
                         (decimal)p.Positions.First(x => x.Symbol == "BI").Value);
            Assert.Equal(50, p.PercentOfPortfolio(AssetClass.Stock));
            Assert.Equal(50, p.PercentOfPortfolio(AssetClass.Bond));
            Assert.Equal(45, p.PercentOfPortfolio(AssetClass.Stock, AssetLocation.Domestic));
            Assert.Equal(5, p.PercentOfPortfolio(AssetClass.Bond, AssetLocation.Domestic));
        }

        [Fact]
        public void OneAccountFourEqualFunds_IgnoreWorseER()
        {
            var brokerageName = "OneAccountFourEqualFunds_IgnoreWorseER";
            var accounts = new List<Account>{
                CreateAccount(brokerageName, AccountType.BROKERAGE, value: 100)
            };
            var funds = new List<Fund>{
                CreateFund(brokerageName, "SD", .5, 1, 1), // should be ignored, worse ER
                CreateFund(brokerageName, "ZZ_SD", 0, 1, 1), // should be ignored, alphabetically sorted
                CreateFund(brokerageName, "SD", 0, 1, 1),
                CreateFund(brokerageName, "SI", 0, 1, 0),
                CreateFund(brokerageName, "BD", 0, 0, 1),
                CreateFund(brokerageName, "BI", 0, 0, 0),
            };

            Fund.Add(funds);
            var original = new Portfolio(accounts);
            var p = Picker.Rebalance(original, .5, .5, .5, iterationLimit: 1, threadLimit: 1);

            // funds should be equally split
            Assert.NotNull(p);
            Assert.InRange(p.Score, .88, .9); // less than one due to account being always brokerage
            Assert.Equal(4, p.NumberOfPositions);
            Assert.Equal(25, p.Positions.First(x => x.Symbol == "SD").Value);
            Assert.Equal(25, p.Positions.First(x => x.Symbol == "SI").Value);
            Assert.Equal(25, p.Positions.First(x => x.Symbol == "BD").Value);
            Assert.Equal(25, p.Positions.First(x => x.Symbol == "BI").Value);

            // output percentages should match input requests
            Assert.Equal(50, p.PercentOfPortfolio(AssetClass.Stock));
            Assert.Equal(50, p.PercentOfPortfolio(AssetClass.Bond));
            Assert.Equal(50, p.PercentOfPortfolio(AssetLocation.Domestic));
            Assert.Equal(50, p.PercentOfPortfolio(AssetLocation.Domestic));
        }

        [Fact]
        public void ThreeAccountsOneFund()
        {
            var b = "ThreeAccountsOneFund";
            var symbolName = "Generic";
            var accounts = new List<Account>{
                CreateAccount(b, AccountType.BROKERAGE, value: 100),
                CreateAccount(b, AccountType.ROTH, value: 100),
                CreateAccount(b, AccountType.IRA, value: 100),
            };
            var funds = new List<Fund>{
                CreateFund(b, symbolName, 0, 0.5, 0.5),
            };

            Fund.Add(funds);
            var original = new Portfolio(accounts);
            var p = Picker.Rebalance(original, 0.5, 0.5, 0.5, iterationLimit: 1, threadLimit: 1);

            // assert portfolio correctness
            Assert.NotNull(p);
            Assert.InRange(p.Score, .77, .8); // less than 1 due to fund always bleeding exposures
            Assert.Equal(100 * accounts.Count, p.TotalValue);
            Assert.Equal(accounts.Count, p.NumberOfPositions);
            foreach (var o in p.Positions)
            {
                // one fund per account, spend all the money there
                Assert.Equal(symbolName, o.Symbol);
                Assert.Equal(100, o.Value);
            }

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
            var funds = new List<Fund>{
                CreateFund(b, symbolName, 0, 0.5, 1),
                CreateFund(b, symbolName, 0, 0, 1),
            };
            Fund.Add(funds);
            
            var original = new Portfolio(accounts);
            var p = Picker.Rebalance(original, 0.5, 0.5, 0.5, iterationLimit: 1, threadLimit: 1);

            var targetRatios = Picker.ComputeTargetRatios(.5, .5, .5);
            var os = original.GetScore(Picker.GetScoreWeight, targetRatios);
        }
    }
}
