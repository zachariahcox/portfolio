import React, { Component } from 'react';
import { Tabs } from 'react-bootstrap';
import { Tab } from 'react-bootstrap';
import { Editor } from './Editor';

export class DataLayout extends Component {
    displayName = DataLayout.name
    render() {
        const dataReferences = this.props.dataReferences;
        return (
            <Tabs
                id="datatabs"
                defaultActiveKey={dataReferences[0].name}
                mountOnEnter={true}
                transition={false}
                onSelect={(i, l, e) => {
                    if (i != l) {
                        console.log("save");
                    }
                }}
            >
                {dataReferences.map((c, index) =>
                    <Tab
                        key={index}
                        eventKey={c.name}
                        title={c.name}>
                        <Editor dataReference={c} />
                    </Tab>
                )}
            </Tabs>
        );
    }
}