# Introduction
A simple dotnet console application to recommend financial portfolio allocations based on various strategies. 

Your current portfolio is described in a simple yaml syntax: 

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
This object supports rendering to markdown by default. 

## Command line tool
The command line tool supports two commands, ```load``` and ```rebalance```.
Both require a path to an accounts.yaml file and an output directory. 

The rebalance command runs the rebalancing logic and prints its recommendations. The load command simply reports on the portfolio you provide it.

# Getting Started
To run the command line tool you will need dotnet core installed. 

# Build and Test
```bash
cd PortfolioPicker
dotnet build
dotnet test
```