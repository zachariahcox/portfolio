import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Rules } from './components/Rules';
import { Schema } from './components/Schema';
import { Portfolio } from './components/Portfolio';
import { Editor } from './components/Editor';

export default class App extends Component {
    displayName = App.name

    constructor(props) {
        super(props);

        const exampleFunds = `- description: Vanguard Total Stock Market Index Fund
  symbol: VTSAX
  brokerage: Vanguard
  url: https://investor.vanguard.com/mutual-funds/profile/VTSAX
  expenseRatio: 0.04
  stockRatio: 1
  domesticRatio: 1

- description: Vanguard Total International Stock Index Fund
  symbol: VTIAX
  brokerage: Vanguard
  url: https://investor.vanguard.com/mutual-funds/profile/VTIAX
  expenseRatio: 0.11
  stockRatio: 1
  domesticRatio: 0`;

        const exampleAccounts = `- name: Roth
  brokerage: Vanguard
  type: ROTH
  value: 100
- name: Other
  brokerage: Vanguard
  type: TAXABLE
  value: 100`;

        this.state = {
            accountsYaml: exampleAccounts,
            fundsYaml: exampleFunds,
            portfolio: null
        };
        this.cacheAccounts = this.cacheAccounts.bind(this);
        this.cacheFunds = this.cacheFunds.bind(this);
        this.cachePortfolio = this.cachePortfolio.bind(this);
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

    render() {
        return (
            <Layout>
                <Route exact path='/' component={Rules} />
                <Route path='/schema' component={Schema} />
                <Route path='/portfolio' component={() => <Portfolio
                    accountsYaml={this.state.accountsYaml}
                    fundsYaml={this.state.fundsYaml}
                    portfolio={this.state.portfolio}
                    cachePortfolio={this.cachePortfolio}
                    />} />
                <Route path='/editor' component={() => <Editor
                    accountsYaml={this.state.accountsYaml}
                    fundsYaml={this.state.fundsYaml}
                    cacheAccounts={this.cacheAccounts}
                    cacheFunds={this.cacheFunds}
                    />} />
            </Layout>
        );
    }
}
