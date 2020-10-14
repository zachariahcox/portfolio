# Introduction
This app helps rebalance portfolios that span multiple investment accounts.
The application parses the yaml/json and produces a ```Portfolio``` object. 

# Getting Started
To build the project you will need [dotnet core 3.1 or greater](https://code.visualstudio.com/docs/languages/dotnet).

## Build and Test
```bash
cd portfolio
dotnet build
dotnet test
```

## docker
For this, you'll need `docker` installed. 
```bash
cd portfolio
docker build -t portfolio . 
docker run -d -p 5000:80 portfolio
```

## web api
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

# cli
The command line tool supports two commands, ```load``` and ```rebalance```.
Both require a path to an ```portfolio.yaml``` file and an output directory. 

The rebalance command runs the rebalancing logic and prints its recommendations. 
The load command simply reports on the portfolio you provide it.

## Usage
```bash
cd portfolio
cd ../build
dotnet publish ../portfolio/CLI/ -o . -c release
./portfolio rebalance path/to/portfolio.yaml -o path/to/outputdir/ -db 100
```

