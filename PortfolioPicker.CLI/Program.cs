using System;
using System.IO;
using PortfolioPicker.App;

namespace PortfolioPicker.CLI
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 2)
            {
                Console.Write("Please provide path to accounts file and a path to output file.");
                return;
            }

            var data = File.ReadAllText(args[0]);
            var p = Picker.Create(
                accountsYaml: data, 
                fundsYaml: null, 
                strategyName: "FourFundStrategy");
            var portfolio = p.Pick();
            File.WriteAllLines(args[1], portfolio.ToMarkdownLines());
        }
    }
}
