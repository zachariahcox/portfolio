using System;
using System.IO;
using McMaster.Extensions.CommandLineUtils;
using PortfolioPicker.App;

namespace PortfolioPicker.CLI
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "PortfolioPicker",
                Description = "Portfolio balance suggestion engine."
            };

            app.HelpOption(inherited: true);
            app.OnExecute(() =>
            {
                Console.WriteLine("Specify a subcommand");
                app.ShowHelp();
                return 1;
            });

            app.Command("load", cmd =>
            {
                cmd.Description = "Creates a report for a given portfolio.";

                var portfolioPath = cmd.Argument("currentPortfolio", "Path to portfolio file.")
                    .Accepts(v => v.ExistingFile())
                    .IsRequired();

                var outputDir = cmd.Option(
                    template: "-o|--output <directory>",
                    description: "Path to output directory. Defaults to directory containing accounts file.",
                    optionType: CommandOptionType.SingleValue)
                    .Accepts(v => v.LegalFilePath());

                cmd.OnExecute(() =>
                {
                    var data = File.ReadAllText(portfolioPath.Value);
                    var portfolio = Portfolio.FromYaml(data);
                    var portfolioFile = new FileInfo(portfolioPath.Value);
                    var d = outputDir.HasValue()
                        ? outputDir.Value()
                        : portfolioFile.DirectoryName;

                    var basename = Path.GetFileNameWithoutExtension(portfolioFile.Name);
                    var reportPath = Path.Combine(d, $"{basename}_report.md");
                    Console.WriteLine("report: " + reportPath);
                    File.WriteAllLines(reportPath, portfolio.ToMarkdown());

                    var csv = Path.Combine(d, $"{basename}.csv");
                    Console.WriteLine("csv: " + csv);
                    File.WriteAllLines(csv, portfolio.ToXrayCsv());
                });
            });

            app.Command("rebalance", cmd =>
            {
                cmd.Description = "Attempts to balance portfolio based on a strategy";
                var portfolioPath = cmd.Argument("currentPortfolio", "Path to portfolio file.")
                    .Accepts(v => v.ExistingFile())
                    .IsRequired();

                var outputDir = cmd.Option(
                    template: "-o|--output <DIR>",
                    description: "Path to output directory. Defaults to directory containing accounts file.",
                    optionType: CommandOptionType.SingleValue)
                    .Accepts(v => v.LegalFilePath());

                var funds = cmd.Option("-f|--funds <PATH>",
                    "Path to custom funds yaml.",
                    CommandOptionType.SingleValue)
                    .Accepts(x => x.ExistingFile());

                var stockPercent = cmd.Option<int>(
                    "-s|--stockPercent <int>",
                    "Target percent of TOTAL PORTFOLIO in stocks, remainder will be in bonds (default is 90)",
                    CommandOptionType.SingleValue)
                    .Accepts(x => x.Range(0, 100));

                var domesticStockPercent = cmd.Option<int>(
                    "-ds|--domesticStockPercent <int>",
                    "Target percent OF STOCKS to be domestic (default is 6)",
                    CommandOptionType.SingleValue)
                    .Accepts(x => x.Range(0, 100));

                var domesticBondPercent = cmd.Option<int>(
                   "-db|--domesticBondPercent <int>",
                   "Target percent OF BONDS to be domestic (default is 70)",
                   CommandOptionType.SingleValue)
                   .Accepts(x => x.Range(0, 100));

                cmd.OnExecute(() =>
                {
                    // load yaml files
                    var data = File.ReadAllText(portfolioPath.Value);
                    var original = Portfolio.FromYaml(data);
                    Fund.FromYaml(funds.Value());

                    // finalize ratios
                    var stockRatio = stockPercent.HasValue()
                        ? double.Parse(stockPercent.Value()) / 100.0
                        : 0.9; // default to 90% stocks
                    var domesticStockRatio = domesticStockPercent.HasValue()
                        ? double.Parse(domesticStockPercent.Value()) / 100.0
                        : 0.6;
                    var domesticBondRatio = domesticBondPercent.HasValue()
                        ? double.Parse(domesticBondPercent.Value()) / 100.0
                        : 0.7;

                    var portfolio = Picker.Rebalance(
                        portfolio: original,
                        stockRatio: stockRatio,
                        domesticStockRatio: domesticStockRatio,
                        domesticBondRatio: domesticBondRatio);

                    var d = outputDir.HasValue()
                        ? outputDir.Value()
                        : new FileInfo(portfolioPath.Value).DirectoryName;
                    var today = DateTime.Now.ToString("MMddyyyy");

                    var balancedPortfolioPath = Path.Combine(d, $"portfolio_{today}.yaml");
                    Console.WriteLine("new portfolio: " + balancedPortfolioPath);
                    File.WriteAllText(balancedPortfolioPath, portfolio.ToYaml());

                    var reportPath = Path.Combine(d, $"portfolio_{today}_orders.md");
                    Console.WriteLine("orders: " + reportPath);
                    File.WriteAllLines(reportPath, portfolio.ToMarkdown(original));
                });
            });
            return app.Execute(args);
        }
    }
}
