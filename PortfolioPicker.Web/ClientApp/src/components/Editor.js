import React, { Component } from 'react';
import MonacoEditor from 'react-monaco-editor';
//import { Col, Grid, Row } from 'react-bootstrap';
//class EditorLayout extends Component {
//    displayName = EditorLayout.name
//    render() {
//        return (
//            <Grid fluid>
//                <Row sm={2}>
//                    <Col>
//                        {this.props.children}
//                    </Col>
//                </Row>
//                <Row sm={10}>
//                    <Col>
//                        {this.props.children}
//                    </Col>
//                </Row>
//            </Grid>
//        );
//    }
//}

export class Editor extends Component {
    displayName = Editor.name
    constructor(props) {
        super(props);
        this.editor = null;
    }
    componentWillUnmount() {
        if (this.editor) {
            this.props.save(this.editor.getValue());
        }
    }
    editorDidMount(editor, monaco) {
        editor.focus();
        this.editor = editor;
    }

    handleFileChosen(file) {
        var reader = new FileReader();
        reader.onloadend = (event) => {
            this.editor.setValue(reader.result);
        };
        reader.readAsText(file);
    }

    render() {
        const code = this.props.code;
        const options = {
            selectOnLineNumbers: true
        };
        return (
            <div>
                <h1>Accounts</h1>
                <MonacoEditor
                    height="600"
                    language="yaml"
                    theme="vs-dark"
                    value={code}
                    options={options}
                    editorDidMount={this.editorDidMount.bind(this)}
                />
                <input
                    type="file"
                    accept=".yaml"
                    onChange={e => this.handleFileChosen.bind(this)(e.target.files[0])}
                />
            </div>
        );
    }
}