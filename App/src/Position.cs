using System;

namespace PortfolioPicker.App
{
    public class Position
    {
        public string Symbol { get; set; }

        public double Value { get; set; }

        public bool Hold { get; set; }

        public override string ToString() => $"{Symbol}@{Value}";
    }
}
