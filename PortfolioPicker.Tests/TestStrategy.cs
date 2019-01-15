using System;
using System.Collections.Generic;
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

            var p = new Picker(accounts, "FourFundStrategy");
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
            var p = new Picker(accounts, "FourFundStrategy");
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
            var p = new Picker(accounts, "FourFundStrategy");
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
            
            var p = new Picker(accounts, "FourFundStrategy");
            var portfolio = p.Pick();
            Assert.Equal(4, portfolio.BuyOrders.Count);
            var actualValue = portfolio.BuyOrders.Sum(o => o.Value);
            Assert.Equal(100, actualValue);
        }
    }
}
