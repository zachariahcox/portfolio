[![Build Status](https://dev.azure.com/zachariahcox/PortfolioPicker/_apis/build/status/release?branchName=master)](https://dev.azure.com/zachariahcox/PortfolioPicker/_build/latest?definitionId=7&branchName=master)


# Introduction
This app helps rebalance portfolios that span multiple investment accounts.

Your current portfolio is described in a simple yaml syntax: 
```yaml
- name: my roth account
  brokerage: Vanguard
  type: ROTH
  positions:
  - symbol: VTSAX
    value: 100

- name: my 401k
  brokerage: Fidelity
  type: IRA
  positions:
  - symbol: FZROX
    value: 100

- name: my HSA
  brokerage: Fidelity
  type: ROTH
  positions:
  - symbol: FZROX
    value: 100

- name: my regular taxable
  brokerage: Vanguard
  type: BROKERAGE
  positions:
  - symbol: AMZN
    value: 100
  - symbol: MSFT
    value: 100
    hold: true
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

## Command line tool
The command line tool supports two commands, ```load``` and ```rebalance```.
Both require a path to an ```portfolio.yaml``` file and an output directory. 

The rebalance command runs the rebalancing logic and prints its recommendations. 
The load command simply reports on the portfolio you provide it.

# Getting Started
To build the project you will need [dotnet core 3.1 or greater](https://code.visualstudio.com/docs/languages/dotnet).

# Build and Test
```bash
cd PortfolioPicker
dotnet build
dotnet test
```

# Usage
```bash
cd PortfolioPicker
cd ../build
dotnet publish ../PortfolioPicker/CLI/ -o . -c release
./portfolio rebalance path/to/portfolio.yaml -o path/to/outputdir/ -db 100
```

# docker
For this, you'll need `docker` installed. 
```bash
cd PortfolioPicker
docker build -t portfoliopicker .
docker run -d -p 5000:80 portfoliopicker
```