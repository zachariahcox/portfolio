import React, { Component } from 'react';
import { Container, Row, Col } from 'reactstrap';
import brace from 'brace';
import AceEditor from 'react-ace';
import 'brace/mode/yaml';
import 'brace/theme/tomorrow';

export class Controls extends Component {
    static displayName = Controls.name;

  constructor (props) {
      super(props);

      this.exampleAccounts = `# Describe your current positions here:
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
      this.exampleFunds = `# Add additioanl funds to use here:
#- description: Vanguard Total Stock Market Index Fund
#  symbol: VTSAX
#  brokerage: Vanguard
#  expenseRatio: 0.04
#  stockRatio: 1
#  domesticRatio: 1
`;

      this.state = {
          accountsYaml: this.exampleAccounts,
          fundsYaml: this.exampleFunds
      };
  }

    render() {
    return (
        <div>
            <h1>Controls</h1>
            <Container>
                <Row>
                <Col>What percent of total assets should be in stocks?</Col>
                <Col><input type="number" defaultValue="90" /></Col>
                </Row>
                <Row>
                <Col>What percent of stocks should be domestic?</Col>
                <Col><input type="number" defaultValue="60"/></Col>
                </Row>
                <Row>
                <Col>What percent of bonds should be domestic?</Col>
                <Col><input type="number" defaultValue="70" /></Col>
                </Row>
            <Row>
                    <Col>
                        Accounts:
                        <AceEditor
                        mode="yaml"
                        theme="tomorrow"
                        name="editor"
                        value={this.state.accountsYaml}
                        fontSize={14}
                        editorProps={{ $blockScrolling: false }}
                        />
                </Col>
                    <Col>
                        Additional Funds:
                        <AceEditor
                        mode="yaml"
                        theme="tomorrow"
                        name="editor"
                        value={this.state.fundsYaml}
                        fontSize={14}
                        editorProps={{ $blockScrolling: false }}
                        />
                </Col>
            </Row>
        </Container>
      </div>
    );
  }
}
