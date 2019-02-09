import React, { Component } from 'react';
import { Route } from 'react-router';
import { Layout } from './components/Layout';
import { Home } from './components/Home';
import { Schema } from './components/Schema';
import { Portfolio } from './components/Portfolio';
import { Editor } from './components/Editor';

export default class App extends Component {
    displayName = App.name

    constructor(props) {
        super(props);

        const exampleFunds = "";

        const exampleAccounts = `- name: Roth
  brokerage: Vanguard
  type: ROTH
  value: 100
- name: Other
  brokerage: Vanguard
  type: TAXABLE
  value: 100`;

        this.state = {
            code: exampleAccounts,
            portfolio: null
        };
        this.save = this.save.bind(this);
        this.cachePortfolio = this.cachePortfolio.bind(this);
    }

    cachePortfolio(p) {
        this.setState({
            portfolio: p
        });
    }

    save(src) {
        if (src != this.state.code) {
            this.setState({
                code: src,
                portfolio: null
            });
        }
    }

    render() {
        return (
            <Layout>
                <Route exact path='/' component={Home} />
                <Route path='/schema' component={Schema} />
                <Route path='/portfolio' component={() => <Portfolio
                    code={this.state.code}
                    portfolio={this.state.portfolio}
                    cachePortfolio={this.cachePortfolio}
                />} />
                <Route path='/editor'
                    component={() => <Editor
                        code={this.state.code}
                        save={this.save} />} />
            </Layout>
        );
    }
}
