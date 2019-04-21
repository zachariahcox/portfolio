# Introduction
A simple dotnet console application to recommend financial portfolio allocations based on various strategies. 
It's written in dotnet core for practice, but would make a much better python app probably.

Your current portfolio is described in a simple yaml syntax: 
```yaml
- name: my roth account
  brokerage: Vanguard
  type: ROTH
  positions:
  - symbol: VMMXX
    value: 100
  - symbol: VTIAX
    value: 100
  - symbol: VTSAX
    value: 100

- name: my regular taxable
  brokerage: Vanguard
  type: TAXABLE
  positions:
  - symbol: AMZN
    value: 100
  - symbol: MSFT
    value: 100
    hold: true

- name: my 401k
  brokerage: Fidelity
  type: ROTH
  positions:
  - symbol: FZROX
    value: 100
```

You can also provide a list of fund descriptions you want to be made available.
There is a common set loaded by [default](https://zachariahcox.visualstudio.com/_git/PortfolioPicker?path=%2FPortfolioPicker%2FApp%2Fdata%2Ffunds.yaml&version=GBmaster&_a=contents&line=2&lineStyle=plain&lineEnd=9&lineStartColumn=1&lineEndColumn=19). 

```yaml
- description: Vanguard Total Stock Market Index Fund
  symbol: VTSAX
  brokerage: Vanguard # this is the arbitrary title you give to brokerages. It is used to preference which accounts hold which positions
  url: https://investor.vanguard.com/mutual-funds/profile/VTSAX
  expenseRatio: 0.04
  stockRatio: 1    # 0-to-1 percentage of the holdings which are stocks
  domesticRatio: 1 # 0-to-1 percentage of the holdings which are domestic

- description: Vanguard Total International Stock Index Fund
  symbol: VTIAX
  brokerage: Vanguard
  url: https://investor.vanguard.com/mutual-funds/profile/VTIAX
  expenseRatio: 0.11
  stockRatio: 1
  domesticRatio: 0
```

The application parses the yaml and produces a ```Portfolio``` object. 
This object supports rendering markdown reports. 
[vscode](https://code.visualstudio.com/) is a great crossplatform editor for markdown documents. 

## Command line tool
The command line tool supports two commands, ```load``` and ```rebalance```.
Both require a path to an ```accounts.yaml``` file and an output directory. 

The rebalance command runs the rebalancing logic and prints its recommendations. The load command simply reports on the portfolio you provide it.

This will produce an analysis of your current portfolio:
```bash
dotnet picker load /path/to/your/accounts.yml ~/Desktop
```

This will produce a suggested rebalance of your current portfolio.
```bash
dotnet picker rebalance /path/to/your/accounts.yml ~/Desktop
```

# Getting Started
To run the command line tool you will need [dotnet core installed](https://code.visualstudio.com/docs/languages/dotnet) (it's kind of like the python interpreter). 

# Build and Test
```bash
cd PortfolioPicker
dotnet build
dotnet test
```

## Example
There are other examples in the tests project. 
Using the ```example.yaml`` file from above, running this should produce the following report:
```bash
$ portfolio rebalance /path/to/example.yaml
```
--- 

# portfolio
## stats
|stat|value|
|---|---|
|date|04/21/2019|
|TotalValue|$600.00|
|ExpenseRatio|0.0557|
|BondRatio|0.10|
|StockRatio|0.90|
|DomesticRatio|0.61|
|InternationalRatio|0.39|
|Strategy|FourFundStrategy|

## positions
|account|symbol|value|
|---|---|---:|
|my 401k|[FZROX](https://finance.yahoo.com/quote/FZROX?p=FZROX)|$58.00|
|my 401k|[FXNAX](https://finance.yahoo.com/quote/FXNAX?p=FXNAX)|$42.00|
|my regular taxable|[MSFT](https://finance.yahoo.com/quote/MSFT?p=MSFT)|$100.00|
|my regular taxable|[VTABX](https://finance.yahoo.com/quote/VTABX?p=VTABX)|$18.00|
|my regular taxable|[VTIAX](https://finance.yahoo.com/quote/VTIAX?p=VTIAX)|$82.00|
|my roth account|[VTIAX](https://finance.yahoo.com/quote/VTIAX?p=VTIAX)|$134.00|
|my roth account|[VTSAX](https://finance.yahoo.com/quote/VTSAX?p=VTSAX)|$166.00|
## orders
|account|action|symbol|value|
|---|---|---|---:|
|my 401k|buy|[FXNAX](https://finance.yahoo.com/quote/FXNAX?p=FXNAX)|$42.00|
|my 401k|sell|[FZROX](https://finance.yahoo.com/quote/FZROX?p=FZROX)|$42.00|
|my regular taxable|buy|[VTABX](https://finance.yahoo.com/quote/VTABX?p=VTABX)|$18.00|
|my regular taxable|buy|[VTIAX](https://finance.yahoo.com/quote/VTIAX?p=VTIAX)|$82.00|
|my regular taxable|sell|[AMZN](https://finance.yahoo.com/quote/AMZN?p=AMZN)|$100.00|
|my roth account|buy|[VTIAX](https://finance.yahoo.com/quote/VTIAX?p=VTIAX)|$34.00|
|my roth account|buy|[VTSAX](https://finance.yahoo.com/quote/VTSAX?p=VTSAX)|$66.00|
|my roth account|sell|[VMMXX](https://finance.yahoo.com/quote/VMMXX?p=VMMXX)|$100.00|

---

Running this should produce the following report: 
```bash
portfolio load /path/to/example.yaml
```

---

# Custom portfolio
## stats
|stat|value|
|---|---|
|date|04/21/2019|
|TotalValue|$600.00|
|ExpenseRatio|0.0250|
|BondRatio|0.00|
|StockRatio|1.00|
|DomesticRatio|0.83|
|InternationalRatio|0.17|

## positions
|account|symbol|value|
|---|---|---:|
|my 401k|[FZROX](https://finance.yahoo.com/quote/FZROX?p=FZROX)|$100.00|
|my regular taxable|[AMZN](https://finance.yahoo.com/quote/AMZN?p=AMZN)|$100.00|
|my regular taxable|[MSFT](https://finance.yahoo.com/quote/MSFT?p=MSFT)|$100.00|
|my roth account|[VMMXX](https://finance.yahoo.com/quote/VMMXX?p=VMMXX)|$100.00|
|my roth account|[VTIAX](https://finance.yahoo.com/quote/VTIAX?p=VTIAX)|$100.00|
|my roth account|[VTSAX](https://finance.yahoo.com/quote/VTSAX?p=VTSAX)|$100.00|

---

