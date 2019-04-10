using System;
using System.Collections.Generic;

namespace PortfolioPicker.App
{
    public class Position
    {
        public Guid Id { get; private set; }

        public Fund Fund { get; set; }

        public decimal Value { get; set; } = 0m;

        public Position(
            Fund fund = null, 
            decimal value = 0m)
        {
            this.Fund = fund;
            this.Value = value;
            this.Id = Guid.NewGuid();
        }

        public PositionReference Reference()
        {
            return new PositionReference
            {
                Symbol = this.Fund.Symbol,
                Value = this.Value
            };
        }
    }

    public class PositionReference
    {
        public string Symbol { get; set; }
        
        public decimal Value { get; set; }

        public bool Hold { get; set; }
    }
}
