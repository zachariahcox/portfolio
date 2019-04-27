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
            var app = new CommandLineApplication(throwOnUnexpectedArg: false)
            {
                Name = "PortfolioPicker",
                Description = "Portfolio balance suggestion engine."
            };

            app.HelpOption(inherited: true);

            app.Command("load", cmd => {

                cmd.Description = "Creates current portfolio report";

                var accounts = cmd.Argument("accounts", "Path to accounts file.")
                    .Accepts(v => v.ExistingFile())
                    .IsRequired();

                var outputDir = cmd.Option(
                    template: "-o|--output <directory>",
                    description: "Path to output directory. Defaults to directory containing accounts file.",
                    optionType: CommandOptionType.SingleValue)
                    .Accepts(v => v.LegalFilePath());

                cmd.OnExecute(() =>
                {
                    var data = File.ReadAllText(accounts.Value);
                    var portfolio = Portfolio.FromYaml(data);
                    var reportPath = ReportPath(cmd.Name, accounts.Value, outputDir.Value());
                    Console.WriteLine("report: " + reportPath);
                    File.WriteAllLines(reportPath, portfolio.ToMarkdownLines());
                });
            });

            app.Command("rebalance", (Action<CommandLineApplication>)(cmd =>
            {
                cmd.Description = "Attempts to balance portfolio based on a strategy";
                var accounts = cmd.Argument("accounts", "Path to accounts file.")
                    .Accepts(v => v.ExistingFile())
                    .IsRequired();

                var outputDir = cmd.Option(
                    template: "-o|--output <DIR>",
                    description: "Path to output directory. Defaults to directory containing accounts file.",
                    optionType: CommandOptionType.SingleValue)
                    .Accepts(v => v.LegalFilePath());

                var funds = cmd.Option("-f|--funds <PATH>",
                    "Path to funds yaml.",
                    CommandOptionType.SingleValue)
                    .Accepts(x => x.ExistingFile());

                var stockPercent = cmd.Option<int>(
                    "-s|--stockPercent <int>",
                    "Target percent of TOTAL PORTFOLIO in stocks (remainder will be in bonds)",
                    CommandOptionType.SingleValue)
                    .Accepts(x => x.Range(0, 100));

                var domesticStockPercent = cmd.Option<int>(
                    "-ds|--domesticStockPercent <int>",
                    "Target percent OF STOCKS to be domestic",
                    CommandOptionType.SingleValue)
                    .Accepts(x => x.Range(0, 100));

                var domesticBondPercent = cmd.Option<int>(
                   "-db|--domesticBondPercent <int>",
                   "Target percent OF BONDS to be domestic",
                   CommandOptionType.SingleValue)
                   .Accepts(x => x.Range(0, 100));

                cmd.OnExecute((Action)(() =>
                {
                    var data = File.ReadAllText(accounts.Value);
                    var picker = Picker.Create(
                        accountsYaml: data,
                        fundsYaml: funds.Value());

                    var stockRatio = stockPercent.HasValue()
                        ? double.Parse(stockPercent.Value()) / 100.0
                        : 0.9;

                    var domesticStockRatio = domesticStockPercent.HasValue()
                        ? double.Parse(domesticStockPercent.Value()) / 100.0
                        : 0.6;

                    var domesticBondRatio = domesticBondPercent.HasValue()
                        ? double.Parse(domesticBondPercent.Value()) / 100.0
                        : 0.7;

                    var portfolio = picker.Rebalance(
                        stockRatio: stockRatio,
                        domesticStockRatio: domesticStockRatio,
                        domesticBondRatio: domesticBondRatio);

                    var reportPath = ReportPath(cmd.Name, accounts.Value, outputDir.Value());
                    Console.WriteLine("report: " + reportPath);
                    File.WriteAllLines(reportPath, portfolio.ToMarkdownLines());
                }));
            }));

            // handle default case
            app.OnExecute(() =>
            {
                Console.WriteLine("Specify a subcommand");
                app.ShowHelp();
                return 1;
            });
            return app.Execute(args);
        }

        private static string ReportPath(
            string title,
            string accountsPath,
            string customOutputPath)
        {
            var directory = customOutputPath == null
                ? new FileInfo(accountsPath).DirectoryName
                : customOutputPath;
            var today = DateTime.Now.ToString("MM_dd_yyyy");
            return Path.Combine(directory, $"portfolio_{title}_{today}.md");
        }
    }
}
