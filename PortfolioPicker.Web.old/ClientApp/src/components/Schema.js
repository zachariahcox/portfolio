import React, { Component } from 'react';
import MonacoEditor from 'react-monaco-editor';

export class Schema extends Component {
    displayName = Schema.name

    constructor(props) {
        super(props);
        this.state = {};
    }

    render() {
        const accountsYaml = `- name: Roth
  brokerage: Vanguard
  type: ROTH
  value: 100
- name: Other
  brokerage: Vanguard
  type: TAXABLE
  value: 100`;

        const fundsYaml = `- description: Vanguard Total Stock Market Index Fund
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

        const options = {
            selectOnLineNumbers: true,
            readOnly: true
        };

        return (
            <div>
                <h1>Account data</h1>
                <MonacoEditor
                    height="300"
                    language="yaml"
                    theme="vs-dark"
                    value={accountsYaml}
                    options={options}
                />

                <h1>Funds data</h1>
                <MonacoEditor
                    height="300"
                    language="yaml"
                    theme="vs-dark"
                    value={fundsYaml}
                    options={options}
                />
            </div>
        );
    }
}