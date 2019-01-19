import React, { Component } from 'react';

export class Schema extends Component {
    displayName = Schema.name

    constructor(props) {
        super(props);
        this.state = {};
    }

    render() {

        let exampleAccountString = `
[{
    'name': 'Roth',
    'brokerage': 'Vanguard',
    'type': 'ROTH',
    'taxable': false,
    'value': 100.0
}]`;

        return (
            <div>
                <em>Account data</em>
                <pre>{exampleAccountString}</pre>

                <em>Funds data</em>
                <pre>todo</pre>
            </div>
        );
    }
}
