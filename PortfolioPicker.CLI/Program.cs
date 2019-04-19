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
                Environment.Exit(1);
            }

            var src = args[0];
            if (!File.Exists(src))
            {
                Console.Write($"{src} is not an accounts file");
                Environment.Exit(1);
            }
            
            var outputDir = args[1];
            if (!Directory.Exists(outputDir))
            {
                Console.Write($"{outputDir} is not a directory.");
                Environment.Exit(1);
            }

            var data = File.ReadAllText(src);
            var p = Picker.Create(
                accountsYaml: data, 
                fundsYaml: null, 
                strategyName: "FourFundStrategy");
            var portfolio = p.Rebalance();

            var now = System.DateTime.Now.ToString("MM_dd_yyyy");
            var outputFile = Path.Combine(outputDir, $"portfolioRebalance_{now}.md");
            File.WriteAllLines(outputFile, portfolio.ToMarkdownLines());
        }
    }
}
