using System.Collections.Generic;
using System.Linq;
using PortfolioPicker.App;
using PortfolioPicker.App.Strategies;
using Xunit;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace PortfolioPicker.Tests
{
    public class TestStrategy
    {
        Account CreateAccount(
                string brokerage,
                AccountType type,
                decimal value = 100m)
        {
            return new Account
            {
                Brokerage = brokerage,
                Name = $"My {brokerage} account",
                Type = type,
                Value = value
            };
        }

        Fund CreateFund(
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

        private IReadOnlyList<Account> CreateAccounts()
        {
            var rc = new List<Account>();
            foreach (var t in new[] { AccountType.CORPORATE, AccountType.ROTH, AccountType.TAXABLE })
            {
                foreach (var name in new[] { "a", "b", "c" })
                {
                    rc.Add(CreateAccount(name, t, 10000m));
                }
            }

            return rc;
        }

        private IReadOnlyList<Fund> CreateFundMap()
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
            return rc as IReadOnlyList<Fund>;
        }

        [Fact]
        public void JustVanguard()
        {
            var accounts = new List<Account>
            {
                new Account{
                    Name ="roth",
                    Brokerage="Vanguard",
                    Type=AccountType.ROTH,
                    Value=100
                },
            };

            var p = Picker.Create(accounts);
            var portfolio = p.Pick();
            Assert.Equal(4, portfolio.BuyOrders.Count);
            var actualValue = portfolio.BuyOrders.Sum(o => o.Value);
            Assert.Equal(100, actualValue);
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
                    Type=AccountType.TAXABLE,
                    Value=100
                },
                new Account{
                    Name ="401k",
                    Brokerage="Fidelity",
                    Type=AccountType.CORPORATE,
                    Value=100
                }
            };
            var total_value = accounts.Sum(a => a.Value);
            var p = Picker.Create(accounts);
            Assert.Null(p.Pick());
        }

        [Fact]
        public void InsufficientFunds()
        {
            var accounts = new List<Account>
            {
                new Account{
                    Name ="401k",
                    Brokerage="Fidelity",
                    Type=AccountType.CORPORATE,
                    Value=100
                }
            };

            var total_value = accounts.Sum(a => a.Value);
            var p = Picker.Create(accounts);
            Assert.Null(p.Pick());
        }

        [Fact]
        public void AccountsFromYaml()
        {
            var accounts = @"
- name: Roth
  brokerage: Vanguard
  type: ROTH
  value: 100";

            var p = Picker.Create(accounts);
            var portfolio = p.Pick();
            Assert.Equal(4, portfolio.BuyOrders.Count);
            var actualValue = portfolio.BuyOrders.Sum(o => o.Value);
            Assert.Equal(100, actualValue);
        }

        [Fact]
        public void FromYaml()
        {
            var yaml = @"
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
  value: 100
- name: Other
  brokerage: Vanguard
  type: TAXABLE
  value: 100";

            var deserializer = new DeserializerBuilder()
            .WithNamingConvention(new CamelCaseNamingConvention())
            .Build();

            var funds = deserializer.Deserialize<IList<Fund>>(yaml);
            Assert.Equal(2, funds.Count);

            var accounts = deserializer.Deserialize<IList<Account>>(accountsYaml);
            Assert.Equal(2, accounts.Count);
        }

        [Fact]
        public void Complex()
        {
            var accounts = CreateAccounts();
            var brokerages = CreateFundMap();
            var expectedTotal = accounts.Count * 10000m;
            var p = Picker.Create(accounts, brokerages);
            var portfolio = p.Pick();
            Assert.Equal(12, portfolio.BuyOrders.Count);
            Assert.Equal(1.59, portfolio.ExpenseRatio);
            var actualTotal = portfolio.BuyOrders.Sum(o => o.Value);
            Assert.Equal(expectedTotal, actualTotal);
        }

        //[Fact]
        //public void Real()
        //{
        //    var accounts = File.ReadAllText("C:/Users/zacox/Documents/accounts.yaml");
        //    var p = Picker.Create(accounts);
        //    var portfolio = p.Pick();
        //    Assert.Equal(11, portfolio.BuyOrders.Count);
        //}

        [Fact]
        public void OneAccountFourPerfectFunds()
        {
            var dollars = 100m;
            var accounts = new List<Account>{
                CreateAccount("X", AccountType.TAXABLE, dollars)
            };
            var funds = new List<Fund>{
                CreateFund("X", "SD", 0, 1, 1),
                CreateFund("X", "SI", 0, 1, 0),
                CreateFund("X", "BD", 0, 0, 1),
                CreateFund("X", "BI", 0, 0, 0),
            };

            // construct strategy
            var s = new FourFundStrategy
            {
                StockRatio = 0.5m,
                StockDomesticRatio = 0.5m,
                BondsDomesticRatio = 0.5m
            };
            var picker = Picker.Create(accounts, funds, s);
            var p = picker.Pick();

            // funds should be equally split
            Assert.NotNull(p);
            Assert.Equal(1.0, p.Score);
            Assert.Equal(4, p.BuyOrders.Count);
            Assert.Equal(0.25m * dollars, p.BuyOrders.First(x => x.Fund.Symbol == "SD").Value);
            Assert.Equal(0.25m * dollars, p.BuyOrders.First(x => x.Fund.Symbol == "SI").Value);
            Assert.Equal(0.25m * dollars, p.BuyOrders.First(x => x.Fund.Symbol == "BD").Value);
            Assert.Equal(0.25m * dollars, p.BuyOrders.First(x => x.Fund.Symbol == "BI").Value);

            // output percentages should match input requests
            Assert.Equal(0.5, p.StockRatio);
            Assert.Equal(0.5, p.BondRatio);
            Assert.Equal(0.5, p.DomesticRatio);
            Assert.Equal(0.5, p.InternationalRatio);

            // ==================================
            // Change stock ratio
            s.StockRatio = 0.9m;
            s.StockDomesticRatio = 1m;
            s.BondsDomesticRatio = 1m;
            p = picker.Pick();
            Assert.NotNull(p);
            Assert.Equal(1.0, p.Score);
            Assert.Equal(2, p.BuyOrders.Count);
            Assert.Equal(0.9m * dollars, p.BuyOrders.First(x => x.Fund.Symbol == "SD").Value);
            Assert.Equal(0.1m * dollars, p.BuyOrders.First(x => x.Fund.Symbol == "BD").Value);
            Assert.Equal(0.9, p.StockRatio);
            Assert.Equal(0.1, p.BondRatio);
            Assert.Equal(1, p.DomesticRatio);
            Assert.Equal(0, p.InternationalRatio);

            // ==================================
            // Change domestic ratio
            s.StockRatio = 0.5m;
            s.StockDomesticRatio = 0.9m;
            s.BondsDomesticRatio = 0.1m;
            p = picker.Pick();
            Assert.NotNull(p);
            Assert.Equal(1.0, p.Score);
            Assert.Equal(4, p.BuyOrders.Count);
            Assert.Equal(s.StockRatio * s.StockDomesticRatio * dollars, 
                         p.BuyOrders.First(x => x.Fund.Symbol == "SD").Value);
            Assert.Equal(s.StockRatio * s.StockInternationalRatio * dollars,
                         p.BuyOrders.First(x => x.Fund.Symbol == "SI").Value);
            Assert.Equal(s.BondsRatio * s.BondsDomesticRatio * dollars, 
                         p.BuyOrders.First(x => x.Fund.Symbol == "BD").Value);
            Assert.Equal(s.BondsRatio * s.BondsInternationalRatio * dollars,
                         p.BuyOrders.First(x => x.Fund.Symbol == "BI").Value);
            Assert.Equal(0.5, p.StockRatio);
            Assert.Equal(0.5, p.BondRatio);
            Assert.Equal(0.5, p.DomesticRatio);
            Assert.Equal(0.5, p.InternationalRatio);
        }

        [Fact]
        public void OneAccountFourEqualFunds_IgnoreWorseER()
        {
            var accounts = new List<Account>{
                CreateAccount("X", AccountType.TAXABLE, value: 100)
            };
            var funds = new List<Fund>{
                CreateFund("X", "SD", .5, 1, 1), // should be ignored, worse ER
                CreateFund("X", "ZZ_SD", 0, 1, 1), // should be ignored, alphabetically sorted
                CreateFund("X", "SD", 0, 1, 1),
                CreateFund("X", "SI", 0, 1, 0),
                CreateFund("X", "BD", 0, 0, 1),
                CreateFund("X", "BI", 0, 0, 0),
            };

            // construct strategy
            var s = new FourFundStrategy
            {
                StockRatio = 0.5m,
                StockDomesticRatio = 0.5m,
                BondsDomesticRatio = 0.5m
            };
            var picker = Picker.Create(accounts, funds, s);
            var p = picker.Pick();

            // funds should be equally split
            Assert.NotNull(p);
            Assert.Equal(1.0, p.Score); // perfect score
            Assert.Equal(4, p.BuyOrders.Count);
            Assert.Equal(25m, p.BuyOrders.First(x => x.Fund.Symbol == "SD").Value);
            Assert.Equal(25m, p.BuyOrders.First(x => x.Fund.Symbol == "SI").Value);
            Assert.Equal(25m, p.BuyOrders.First(x => x.Fund.Symbol == "BD").Value);
            Assert.Equal(25m, p.BuyOrders.First(x => x.Fund.Symbol == "BI").Value);

            // output percentages should match input requests
            Assert.Equal(0.5, p.StockRatio);
            Assert.Equal(0.5, p.BondRatio);
            Assert.Equal(0.5, p.DomesticRatio);
            Assert.Equal(0.5, p.InternationalRatio);
        }

        [Fact]
        public void ThreeAccountsOneFund()
        {
            var brokerageName = "x";
            var symbolName = "Generic";
            var accounts = new List<Account>{
                CreateAccount(brokerageName, AccountType.TAXABLE, value: 100),
                CreateAccount(brokerageName, AccountType.ROTH, value: 100),
                CreateAccount(brokerageName, AccountType.CORPORATE, value: 100),
            };
            var funds = new List<Fund>{
                CreateFund(brokerageName, symbolName, 0, .5, .5),
            };

            // construct strategy
            var s = new FourFundStrategy
            {
                StockRatio = 0.5m,
                StockDomesticRatio = 0.5m,
                BondsDomesticRatio = 0.5m
            };
            var picker = Picker.Create(accounts, funds, s);
            var p = picker.Pick();

            // assert portfolio correctness
            Assert.NotNull(p);
            Assert.Equal(1.0, p.Score); // perfect score
            Assert.Equal(100 * accounts.Count, p.TotalValue);
            Assert.Equal(accounts.Count, p.BuyOrders.Count);
            foreach (var o in p.BuyOrders)
            {
                // one fund per account, spend all the money there
                Assert.Equal(brokerageName, o.Fund.Brokerage);
                Assert.Equal(symbolName, o.Fund.Symbol);
                Assert.Equal(100m, o.Value); 
            }

            // output percentages should match input requests
            Assert.Equal((double)s.StockRatio, p.StockRatio);
            Assert.Equal((double)s.BondsRatio, p.BondRatio);
            Assert.Equal(0.5, p.DomesticRatio);
            Assert.Equal(0.5, p.InternationalRatio);
        }
    }
}
