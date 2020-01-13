using System;
using System.Collections.Generic;
using System.Linq;
using PortfolioPicker.App;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PortfolioPicker.Tests
{
    public class TestStrategy
    {
        private Account CreateAccount(
                string brokerage,
                AccountType type,
                string symbol = "VTSAX",
                decimal value = 100m)
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

        /// <summary>
        /// returns 3 accounts
        /// </summary>
        private IList<Account> CreateAccounts()
        {
            var rc = new List<Account>();
            foreach (var t in new[] { AccountType.IRA, AccountType.ROTH, AccountType.BROKERAGE })
            {
                foreach (var name in new[] { "a", "b", "c" })
                {
                    rc.Add(CreateAccount(name, t, value: 10000m));
                }
            }

            return rc;
        }

        private IList<Fund> CreateFundMap()
        {
            List<Fund> makeList(string b)
            {
                return new List<Fund> {
                CreateFund(b, "m", 1, 1, 1),
                CreateFund(b, "n", 2, 1, 0),
                CreateFund(b, "o", 3, 0, 1),
                CreateFund(b, "p", 4, 0, 0),
                };
            }

            var rc = new List<Fund>();
            rc.AddRange(makeList("a"));
            rc.AddRange(makeList("b"));
            rc.AddRange(makeList("c"));
            return rc;
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

            var p = Picker.Create(accounts);
            var portfolio = p.Rebalance(.9, .6, .7);
            Assert.Equal(4, portfolio.NumberOfPositions);
            Assert.Equal(100, portfolio.TotalValue);
        }

        [Fact]
        public void JustFidelity()
        {
            // fidelity does not have access to bond products
            var accounts = new List<Account>
            {
                new Account{
                    Name ="taxable",
                    Brokerage="Fidelity",
                    Type=AccountType.BROKERAGE,
                    Positions = new List<Position>
                    {
                        new Position
                        {
                            Symbol = "FZROX",
                            Value = 100
                        }
                    }
                },
                new Account{
                    Name ="401k",
                    Brokerage="Fidelity",
                    Type=AccountType.IRA,
                    Positions = new List<Position>
                    {
                        new Position
                        {
                            Symbol = "FXNAX",
                            Value = 100
                        }
                    }
                }
            };
            var total_value = accounts
                .SelectMany(x => x.Positions)
                .Sum(x => x.Value);
            var p = Picker.Create(accounts);
            Assert.Null(p.Rebalance(.9, .6, .7));
        }

        [Fact]
        public void InsufficientFunds()
        {
            var accounts = new List<Account>
            {
                new Account{
                    Name ="401k",
                    Brokerage="Fidelity",
                    Type=AccountType.IRA,
                    Positions = new List<Position>
                    {
                        new Position
                        {
                            Symbol = "FXNAX",
                            Value = 100
                        }
                    }
                }
            };

            var p = Picker.Create(accounts);
            Assert.Null(p.Rebalance(.9, .6, .7));
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

            var p = Picker.Create(yaml);
            var portfolio = p.Rebalance(.9, .6, .7);
            Assert.Equal(4, portfolio.NumberOfPositions);
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
            var yaml = @"- name: Roth
  brokerage: Vanguard
  type: ROTH
  positions:
  - symbol: VTSAX
    value: 100
  - symbol: VTIAX
    value: 200
    hold: true
";
            var p = Portfolio.FromYaml(yaml);
            var expected = @"## highlights
|stat|value|
|---|---|
|total value of assets|$300.00|
|total expense ratio|0.0867|
|percent of stocks are domestic|33.3%|
|percent of bonds are domestic|0.0%|

## position composition
|class|location|percentage|value|
|---|---|---:|---:|
|stock|*|100.0%|$300.00|
|stock|domestic|33.3%|$100.00|
|stock|international|66.7%|$200.00|
|bond|*|0.0%|$0.00|
|bond|domestic|0.0%|$0.00|
|bond|international|0.0%|$0.00|

## account composition
|class|location|brokerage|ira|roth|
|---|---|---:|---:|---:|
|stock|domestic|0.0%|0.0%|33.3%|
|stock|international|0.0%|0.0%|66.7%|
|bond|domestic|0.0%|0.0%|0.0%|
|bond|international|0.0%|0.0%|0.0%|

## positions
|account|symbol|value|description|
|---|---|---:|---|
|Roth|[VTIAX](https://finance.yahoo.com/quote/VTIAX?p=VTIAX)|$200.00|Vanguard Total International Stock Index Fund|
|Roth|[VTSAX](https://finance.yahoo.com/quote/VTSAX?p=VTSAX)|$100.00|Vanguard Total Stock Market Index Fund|";

            var actual = string.Join(Environment.NewLine, p.ToMarkdown());
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void FromYaml()
        {
            var fundsYaml = @"
- description: Vanguard Total Stock Market Index Fund
  symbol: VTSAX
  brokerage: Vanguard
  url: https://investor.vanguard.com/mutual-funds/profile/VTSAX
  expenseRatio: 0.04
  stockRatio: 1
  domesticRatio: 1

- description: Vanguard Total International Stock Index Fund
  symbol: VTIAX
  brokerage: Vanguard
  url: https://investor.vanguard.com/mutual-funds/profile/VTIAX
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
            var accounts = CreateAccounts();
            var brokerages = CreateFundMap();
            var expectedTotal = accounts.Count * 10000m;
            var p = Picker.Create(accounts, brokerages);
            var portfolio = p.Rebalance(.9, .6, .7);
            Assert.Equal(12, portfolio.NumberOfPositions);
            Assert.Equal(1.59, portfolio.ExpenseRatio);
            Assert.Equal(expectedTotal, portfolio.TotalValue);
        }

        [Fact]
        public void OneAccountFourPerfectFunds()
        {
            var brokerage = "OneAccountFourPerfectFunds";
            var dollars = 100m;
            var accounts = new List<Account>{
                CreateAccount(brokerage, AccountType.BROKERAGE, value: dollars)
            };
            var funds = new List<Fund>{
                CreateFund(brokerage, "SD", 0, 1, 1),
                CreateFund(brokerage, "SI", 0, 1, 0),
                CreateFund(brokerage, "BD", 0, 0, 1),
                CreateFund(brokerage, "BI", 0, 0, 0),
            };

            var picker = Picker.Create(accounts, funds);
            var p = picker.Rebalance(.5, .5, .5);

            // funds should be equally split
            Assert.NotNull(p);
            Assert.Equal(1.0, p.Score);
            Assert.Equal(4, p.NumberOfPositions);
            Assert.Equal(0.25m * dollars, p.Positions.First(x => x.Symbol == "SD").Value);
            Assert.Equal(0.25m * dollars, p.Positions.First(x => x.Symbol == "SI").Value);
            Assert.Equal(0.25m * dollars, p.Positions.First(x => x.Symbol == "BD").Value);
            Assert.Equal(0.25m * dollars, p.Positions.First(x => x.Symbol == "BI").Value);

            // output percentages should match input requests
            Assert.Equal(50, p.Exposures.Percent(AssetClass.Stock));
            Assert.Equal(50, p.Exposures.Percent(AssetClass.Bond));
            Assert.Equal(25, p.Exposures.Percent(AssetClass.Stock, AssetLocation.Domestic));
            Assert.Equal(25, p.Exposures.Percent(AssetClass.Bond, AssetLocation.Domestic));

            // ==================================
            // Change stock ratio
            p = picker.Rebalance(.9, 1, 1);
            Assert.NotNull(p);
            Assert.Equal(1.0, p.Score);
            Assert.Equal(2, p.NumberOfPositions);
            Assert.Equal(0.9m * dollars, p.Positions.First(x => x.Symbol == "SD").Value);
            Assert.Equal(0.1m * dollars, p.Positions.First(x => x.Symbol == "BD").Value);
            Assert.Equal(90, p.Exposures.Percent(AssetClass.Stock));
            Assert.Equal(10, p.Exposures.Percent(AssetClass.Bond));
            Assert.Equal(90, p.Exposures.Percent(AssetClass.Stock, AssetLocation.Domestic));
            Assert.Equal(10, p.Exposures.Percent(AssetClass.Bond, AssetLocation.Domestic));

            // ==================================
            // Change domestic ratio
            p = picker.Rebalance(.5, .9, .1);
            Assert.NotNull(p);
            Assert.Equal(1.0, p.Score);
            Assert.Equal(4, p.Positions.Count);
            Assert.Equal(.5m * .9m * dollars,
                         p.Positions.First(x => x.Symbol == "SD").Value);
            Assert.Equal(.5m * .1m * dollars,
                         p.Positions.First(x => x.Symbol == "SI").Value);
            Assert.Equal(.5m * .1m * dollars,
                         p.Positions.First(x => x.Symbol == "BD").Value);
            Assert.Equal(.5m * .9m * dollars,
                         p.Positions.First(x => x.Symbol == "BI").Value);
            Assert.Equal(50, p.Exposures.Percent(AssetClass.Stock));
            Assert.Equal(50, p.Exposures.Percent(AssetClass.Bond));
            Assert.Equal(45, p.Exposures.Percent(AssetClass.Stock, AssetLocation.Domestic));
            Assert.Equal(5, p.Exposures.Percent(AssetClass.Bond, AssetLocation.Domestic));
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

            var picker = Picker.Create(accounts, funds);
            var p = picker.Rebalance(.5, .5, .5);

            // funds should be equally split
            Assert.NotNull(p);
            Assert.Equal(1.0, p.Score); // perfect score
            Assert.Equal(4, p.Positions.Count);
            Assert.Equal(25m, p.Positions.First(x => x.Symbol == "SD").Value);
            Assert.Equal(25m, p.Positions.First(x => x.Symbol == "SI").Value);
            Assert.Equal(25m, p.Positions.First(x => x.Symbol == "BD").Value);
            Assert.Equal(25m, p.Positions.First(x => x.Symbol == "BI").Value);

            // output percentages should match input requests
            Assert.Equal(50, p.Exposures.Percent(AssetClass.Stock));
            Assert.Equal(50, p.Exposures.Percent(AssetClass.Bond));
            Assert.Equal(50, p.Exposures.Percent(AssetLocation.Domestic));
            Assert.Equal(50, p.Exposures.Percent(AssetLocation.Domestic));
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

            var picker = Picker.Create(accounts, funds);
            var p = picker.Rebalance(0.5, 0.5, 0.5);

            // assert portfolio correctness
            Assert.NotNull(p);
            Assert.Equal(1.0, p.Score); // perfect score
            Assert.Equal(100 * accounts.Count, p.TotalValue);
            Assert.Equal(accounts.Count, p.Positions.Count);
            foreach (var o in p.Positions)
            {
                // one fund per account, spend all the money there
                Assert.Equal(symbolName, o.Symbol);
                Assert.Equal(100m, o.Value);
            }

            // output percentages should match input requests
            foreach (var c in Enum.GetValues(typeof(AssetClass)).Cast<AssetClass>())
                Assert.Equal(50, p.Exposures.Percent(c));

            foreach (var l in Enum.GetValues(typeof(AssetLocation)).Cast<AssetLocation>())
                Assert.Equal(50, p.Exposures.Percent(l));

            foreach (var c in Enum.GetValues(typeof(AssetClass)).Cast<AssetClass>())
            {
                foreach (var l in Enum.GetValues(typeof(AssetLocation)).Cast<AssetLocation>())
                {
                    Assert.Equal(25, p.Exposures.Percent(c, l));
                }
            }
        }
    }
}
