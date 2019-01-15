import React, { Component } from 'react';

export class Home extends Component {
    displayName = Home.name

    render() {
        return (
            <div>
                <h1>Portfolio Picker</h1>
                <p>Principals behind the four-fund strategy:</p>
                <ul>
                    <li>accounts prefer funds sponsored by their brokerage</li>
                    <li>roth accounts should prioritize stocks over bonds</li>
                    <li>taxable accounts should prioritize international assets over domestic</li>
                    <li>401k accounts should prioritize bonds and avoid international assets</li>
                </ul>
                <p>Preferred accounts:</p>
                <ul>
                    <li>dom stocks -> roth, tax, 401k</li>
                    <li>int stocks -> tax, roth, 401k</li>
                    <li>dom bonds  -> 401k, roth, tax</li>
                    <li>int bonds  -> tax, 401k, roth</li>
                </ul>
            </div>
        );
    }
}
