using System;

namespace PortfolioPicker.App
{
    public class Position
    {
        public Guid Id { get; private set; }

        public string Symbol { get; set; }

        public decimal Value { get; set; }

        public bool Hold { get; set; }

        public override string ToString() => $"{Symbol}@{Value}";
    }
}
