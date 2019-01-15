import React, { Component } from 'react';

export class Schema extends Component {
    displayName = Schema.name

    constructor(props) {
        super(props);
        this.state = {};
    }

    render() {
        return (
            <div>
                Detailed info about json schema
            </div>
        );
    }
}
