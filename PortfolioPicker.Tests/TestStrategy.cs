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
        public void MixedJson()
        {
            var accounts = @"
                [
                    {
                      'name': 'SAS 401k',
                      'brokerage': 'SAS',
                      'type': 'CORPORATE',
                      'taxable': false,
                      'value': 100000.0
                    },
                    {
                      'name': 'Zach Roth',
                      'brokerage': 'Vanguard',
                      'type': 'ROTH',
                      'taxable': false,
                      'value': 100000.0
                    },
                    {
                      'name': 'Llael Roth',
                      'brokerage': 'Vanguard',
                      'type': 'ROTH',
                      'taxable': false,
                      'value': 100000.0
                    },
                    {
                      'name': 'Llael Investment',
                      'brokerage': 'Vanguard',
                      'type': 'INVESTMENT',
                      'taxable': true,
                      'value': 100000.0
                    },
                    {
                      'name': 'Zach Investment',
                      'brokerage': 'Fidelity',
                      'type': 'INVESTMENT',
                      'taxable': true,
                      'value': 100000.0
                    }
                ]";

            var expectedTotal = 500000m;
            var p = Picker.Create(accounts, "FourFundStrategy");
            var portfolio = p.Pick();
            Assert.Equal(8, portfolio.BuyOrders.Count);
            var actualTotal = portfolio.BuyOrders.Sum(o => o.Value);
            Assert.Equal(expectedTotal, actualTotal);
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
                CreateFund("a", 1, true, true),
                CreateFund("b", 2, true, false),
                CreateFund("c", 3, false, true),
                CreateFund("d", 4, false, false),
            };

            var rc = new Dictionary<string, IReadOnlyList<Fund>>
            {
                {"Vanguard", fakeFunds},
                {"Fidelity", fakeFunds},
                {"SAS", fakeFunds},
            };

            return new ReadOnlyDictionary<string, IReadOnlyList<Fund>>(rc);
        }
    }
}
