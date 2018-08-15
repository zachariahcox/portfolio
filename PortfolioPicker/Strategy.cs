﻿using System;
using System.Collections.Generic;
using System.Text;

namespace PortfolioPicker
{
    public class Order
    {
        public Account account;
        public Fund fund;
        public decimal value;

        public Order(Account account = null, Fund fund = null, decimal value = 0m)
        {
            this.account = account;
            this.fund = fund;
            this.value = value;
        }
        public override string ToString()
        {
            return String.Join(", ", new List<String> {
                "Order",
                (account != null ? account.name : "null"),
                (fund != null ? fund.symbol : "null"),
                String.Format("{0:c}", Convert.ToInt32(value))});
        }
    }
    public abstract class Strategy
    {
        public abstract IReadOnlyList<Order> Perform();
        public static IReadOnlyList<Order> Perform<T>() where T : Strategy, new()
        {
            return new T().Perform();
        }
    }
}
