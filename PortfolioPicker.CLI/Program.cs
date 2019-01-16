using System;
using System.IO;
using PortfolioPicker.App;

namespace PortfolioPicker.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            var data = File.ReadAllText(args[0]);
            var p = Picker.Create(data, "FourFundStrategy");
            var portfolio = p.Pick();
            Console.Write(portfolio.ToString());
        }
    }
}
