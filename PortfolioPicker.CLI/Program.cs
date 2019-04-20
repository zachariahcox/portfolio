using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using PortfolioPicker.App;

namespace PortfolioPicker.CLI
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication
            {
                Name = "PortfolioPicker",
                Description = "Portfolio balance suggestion engine."
            };

            app.HelpOption(inherited: true);

            app.Command("load", cmd => {
                
                cmd.Description = "Creates current portfolio report";
                var source = cmd.Argument("source", "Path to source file")
                    .IsRequired()
                    .Accepts(v => v.ExistingFile());
                
                var outputDir = cmd.Argument("output", "Path to output directory")
                    .IsRequired().Accepts(v => v.ExistingDirectory());

                cmd.OnExecute(() =>
                {
                    var data = File.ReadAllText(source.Value);
                    var p = Portfolio.FromYaml(data);
                    var now = DateTime.Now.ToString("MM_dd_yyyy");
                    var outputFile = Path.Combine(outputDir.Value, $"portfolio_{now}.md");
                    File.WriteAllLines(outputFile, p.ToMarkdownLines());
                });
            });

            app.Command("rebalance", cmd =>
            {
                cmd.Description = "Attempts to balance portfolio based on a strategy";
                var source = cmd.Argument("source", "Path to source file")
                    .IsRequired()
                    .Accepts(v => v.ExistingFile());

                var outputDir = cmd.Argument("output", "Path to output directory")
                    .IsRequired().Accepts(v => v.ExistingDirectory());

                cmd.OnExecute(() =>
                {
                    var data = File.ReadAllText(source.Value);
                    var p = Picker.Create(
                        accountsYaml: data,
                        fundsYaml: null,
                        strategyName: "FourFundStrategy");
                    var portfolio = p.Rebalance();

                    var now = DateTime.Now.ToString("MM_dd_yyyy");
                    var outputFile = Path.Combine(outputDir.Value, $"portfolioRebalance_{now}.md");
                    File.WriteAllLines(outputFile, portfolio.ToMarkdownLines());
                });
            });

            // handle default case
            app.OnExecute(() =>
            {
                Console.WriteLine("Specify a subcommand");
                app.ShowHelp();
                return 1;
            });
            return app.Execute(args);
        }
    }
}
