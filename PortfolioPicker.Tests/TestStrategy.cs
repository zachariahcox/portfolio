using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PortfolioPicker.App;
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
                bool taxable,
                decimal value
                )
        {
            return new Account
            {
                Brokerage = brokerage,
                Name = $"My {brokerage} account",
                Type = type,
                Taxable = taxable,
                Value = value
            };
        }

        Fund CreateFund(
            string symbol,
            string brokerage,
            double er,
            double stock = 1.0,
            double domestic = 1.0,
            bool targeted = false)
        {
            return new Fund
            {
                Symbol = symbol,
                Brokerage = brokerage,
                Description = $"{symbol}@{brokerage} ({er})",
                ExpenseRatio = er,
                StockRatio = stock,
                DomesticRatio = domestic,
                TargetDate = targeted
            };
        }

        private IReadOnlyList<Account> CreateAccounts()
        {
            var rc = new List<Account>();
            foreach (var t in new[] { AccountType.CORPORATE, AccountType.ROTH, AccountType.TAXABLE })
            {
                foreach (var name in new[] { "a", "b", "c" })
                {
                    rc.Add(CreateAccount(name, t, t == AccountType.TAXABLE, 10000m));
                }
            }

            return rc;
        }

        private IReadOnlyList<Fund> CreateFundMap()
        {
            List<Fund> makeList(string b)
            {
                return new List<Fund> {
                CreateFund("m", b, 1, 1, 1),
                CreateFund("n", b, 2, 1, 0),
                CreateFund("o", b, 3, 0, 1),
                CreateFund("p", b, 4, 0, 0),
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
                    Taxable=false,
                    Value=100
                },
            };

            var p = Picker.Create(accounts, "FourFundStrategy");
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
                    Taxable=true,
                    Value=100
                },
                new Account{
                    Name ="401k",
                    Brokerage="Fidelity",
                    Type=AccountType.CORPORATE,
                    Taxable=false,
                    Value=100
                }
            };
            var total_value = accounts.Sum(a => a.Value);
            var p = Picker.Create(accounts, "FourFundStrategy");
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
                    Taxable=false,
                    Value=100
                }
            };

            var total_value = accounts.Sum(a => a.Value);
            var p = Picker.Create(accounts, "FourFundStrategy");
            Assert.Null(p.Pick());
        }

        [Fact]
        public void AccountsFromYaml()
        {
            var accounts = @"
- name: Roth
  brokerage: Vanguard
  type: ROTH
  taxable: false
  value: 100";

            var p = Picker.Create(accounts, "FourFundStrategy");
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
  taxable: false
  value: 100
- name: Other
  brokerage: Vanguard
  type: TAXABLE
  taxable: false
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
            var p = Picker.Create(accounts, brokerages, "FourFundStrategy");
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
        //    var p = Picker.Create(accounts, "FourFundStrategy");
        //    var portfolio = p.Pick();
        //    Assert.Equal(11, portfolio.BuyOrders.Count);
        //}
    }
}
