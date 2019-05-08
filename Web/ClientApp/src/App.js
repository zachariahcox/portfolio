import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { Controls } from './components/Controls';
import { Portfolio } from './components/Portfolio';

export class DataReference {
    constructor(name, data, save) {
        this.name = name;
        this.data = data;
        this.save = save;
    }
}


export default class App extends Component {
    static displayName = App.name;

    constructor(props) {
        super(props);
        const exampleFunds = `# Add additioanl funds to use here:
#- description: Vanguard Total Stock Market Index Fund
#  symbol: VTSAX
#  brokerage: Vanguard
#  expenseRatio: 0.04
#  stockRatio: 1
#  domesticRatio: 1
`;

        const exampleAccounts = `# Describe your current positions here:
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
  type: BROKERAGE
  positions:
  - symbol: AMZN
    value: 100
  - symbol: MSFT
    value: 100
    hold: true  # if true, keep this position no matter what

- name: my 401k
  brokerage: Fidelity
  type: IRA
  positions:
  - symbol: FZROX
    value: 100
`;

        this.state = {
            totalStock: 0.5,
            domesticStock: 0.5,
            domesticBonds: 0.5,
            accountsYaml: exampleAccounts,
            fundsYaml: exampleFunds,
            portfolio: null,
        };

        this.cachePortfolio = this.cachePortfolio.bind(this);
        this.cacheAccounts = this.cacheAccounts.bind(this);
        this.cacheFunds = this.cacheFunds.bind(this);
        this.cacheTotalStock = this.cacheTotalStock.bind(this);
        this.cacheDomesticStock = this.cacheDomesticStock.bind(this);
        this.cacheDomesticBonds = this.cacheDomesticBonds.bind(this);
    }

    cachePortfolio(p) {
        this.setState({
            portfolio: p
        });
    }
    cacheAccounts(src) {
        if (src != this.state.accountsYaml) {
            this.setState({
                accountsYaml: src,
                portfolio: null
            });
        }
    }
    cacheFunds(src) {
        if (src != this.state.fundsYaml) {
            this.setState({
                fundsYaml: src,
                portfolio: null
            });
        }
    }
    cacheTotalStock(i) {
        if (i != this.state.totalStock) {
            this.setState({
                totalStock: i,
                portfolio: null
            });
        }
    }
    cacheDomesticStock(i) {
        if (i != this.state.domesticStock) {
            this.setState({
                domesticStock: i,
                portfolio: null
            });
        }
    }
    cacheDomesticBonds(i) {
        if (i != this.state.domesticBonds) {
            this.setState({
                domesticBonds: i,
                portfolio: null
            });
        }
    }

    render() {
        const a = new DataReference("accounts", this.state.accountsYaml, this.cacheAccounts);
        const f = new DataReference("funds", this.state.fundsYaml, this.cacheFunds);
        const ts = new DataReference("totalStock", this.state.totalStock, this.cacheTotalStock);
        const ds = new DataReference("domesticStock", this.state.domesticStock, this.cacheDomesticStock);
        const db = new DataReference("domesticBonds", this.state.domesticBonds, this.cacheDomesticBonds);
        const p = new DataReference("portfolio", this.state.portfolio, this.cachePortfolio);
        return (
            <Layout>
                <Route exact path='/' component={Home} />
                <Route path='/controls' component={() =>
                    <Controls
                        accounts={a}
                        funds={f}
                        totalStock={ts}
                        domesticStock={ds}
                        domesticBonds={db}
                    />
                }/>
                <Route path='/portfolio' component={() =>
                    <Portfolio
                        accounts={a}
                        funds={f}
                        totalStock={ts}
                        domesticStock={ds}
                        domesticBonds={db}
                        portfolio={p}
                    />
                } />
            </Layout>
        );
    }
}
