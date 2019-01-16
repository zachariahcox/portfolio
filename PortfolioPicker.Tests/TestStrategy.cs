using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using PortfolioPicker;
using PortfolioPicker.App;
using Xunit;

namespace PortfolioPicker.Tests
{
    public class TestStrategy
    {
        [Fact]
        public void JustVanguard()
        {
            var accounts = new List<Account>
            {
                new Account{
                    Name ="roth",
                    Brokerage="Vanguard",
                    AccountType=AccountType.ROTH,
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
                    AccountType=AccountType.TAXABLE,
                    Taxable=true,
                    Value=100
                },
                new Account{
                    Name ="401k",
                    Brokerage="Fidelity",
                    AccountType=AccountType.CORPORATE,
                    Taxable=false,
                    Value=100
                }
            };
            var total_value = accounts.Sum(a => a.Value);
            var p = Picker.Create(accounts, "FourFundStrategy");
            Assert.Throws<Exception>(() => p.Pick());
        }

        [Fact]
        public void InsufficientFunds()
        {
            var accounts = new List<Account>
            {
                new Account{
                    Name ="401k",
                    Brokerage="Fidelity",
                    AccountType=AccountType.CORPORATE,
                    Taxable=false,
                    Value=100
                }
            };

            var total_value = accounts.Sum(a => a.Value);
            var p = Picker.Create(accounts, "FourFundStrategy");
            Assert.Throws<Exception>(() => p.Pick());
        }

        [Fact]
        public void FromJson()
        {
            var accounts = @"
            [{
              'name': 'Roth',
              'brokerage': 'Vanguard',
              'type': 'ROTH',
              'taxable': false,
              'value': 100.0
            }]";
            
            var p = Picker.Create(accounts, "FourFundStrategy");
            var portfolio = p.Pick();
            Assert.Equal(4, portfolio.BuyOrders.Count);
            var actualValue = portfolio.BuyOrders.Sum(o => o.Value);
            Assert.Equal(100, actualValue);
        }

        [Fact]
        public void Complex()
        {
            var accounts = GetAccountData();
            var brokerages = GetFundData();
            var expectedTotal = accounts.Count * 10000m;
            var p = Picker.Create(accounts, brokerages, "FourFundStrategy");
            var portfolio = p.Pick();
            Assert.Equal(12, portfolio.BuyOrders.Count);
            Assert.Equal(1.59, portfolio.ExpenseRatio);
            var actualTotal = portfolio.BuyOrders.Sum(o => o.Value);
            Assert.Equal(expectedTotal, actualTotal);
        }

        private IReadOnlyList<Account> GetAccountData()
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
                    AccountType = type,
                    Taxable = taxable,
                    Value = value
                };
            }

            var rc = new List<Account>();
            foreach (var t in new[] { AccountType.CORPORATE, AccountType.ROTH, AccountType.TAXABLE })
            {
                foreach (var name in new [] { "a", "b", "c"})
                {
                    rc.Add(CreateAccount(name, t, t == AccountType.TAXABLE, 10000m));
                }
            }

            return rc;
        }

        private IReadOnlyDictionary<string, IReadOnlyList<Fund>> GetFundData()
        {
            Fund CreateFund(string symbol, double er, bool stock, bool domestic)
            {
                return new Fund
                {
                    Symbol = symbol,
                    ExpenseRatio = er,
                    Stock = stock,
                    Domestic = domestic
                };
            }

            var fakeFunds = new List<Fund> {
                CreateFund("m", 1, true, true),
                CreateFund("n", 2, true, false),
                CreateFund("o", 3, false, true),
                CreateFund("p", 4, false, false),
            };

            var rc = new Dictionary<string, IReadOnlyList<Fund>>
            {
                {"a", fakeFunds},
                {"b", fakeFunds},
                {"c", fakeFunds},
            };

            return new ReadOnlyDictionary<string, IReadOnlyList<Fund>>(rc);
        }
    }
}
