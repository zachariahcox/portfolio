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
There is a common set loaded by [default](https://github.com/zachariahcox/portfolio/blob/master/App/src/data/funds.json). 

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
cd portfolio
dotnet build
dotnet test
```

# Usage
```bash
cd portfolio
cd ../build
dotnet publish ../portfolio/CLI/ -o . -c release
./portfolio rebalance path/to/portfolio.yaml -o path/to/outputdir/ -db 100
```

# docker
For this, you'll need `docker` installed. 
```bash
cd portfolio
docker build -t portfolio . 
docker run -d -p 5000:80 portfolio
```

# api test
```bash
curl --location --request POST 'http://localhost:5000/rebalance' \
     --header 'Content-Type: application/json' \
     --data-raw '{
    "securities": [
        {
            "symbol": "abc",
            "symbolmap": "xyz",
            "quantitymap": 0.5,
            "expenseratio": 0.2,
            "stockratio": 0.7,
            "domesticratio": 0,
            "bondratio": 0.3,
            "internationalratio": 1,
            "description": "Fidelity ZERO Total Market Index Fund",
            "url": "https://finance.yahoo.com/quote/fzrox?p=fzrox"
        }
    ],
    "accounts": [
        {
            "name": "test",
            "brokerage": "fidelity",
            "type": "roth",
            "positions": [
                {
                    "symbol": "fzrox",
                    "quantity": 10.100,
                    "value": 12.56,
                    "hold": true
                }
            ]
        }
    ]
}'
```
