import React, { Component } from 'react';
import MonacoEditor from 'react-monaco-editor';

export class DataReference {
    constructor(name, src, save) {
        this.name = name;
        this.src = src;
        this.save = save;
    }
}

export class Editor extends Component {
    displayName = Editor.name
    constructor(props) {
        super(props);
        this.editor = null;
    }

    componentWillUnmount() {
        if (this.editor) {
            this.props.dataReference.save(this.editor.getValue());
        }
    }
    mount(editor, monaco) {
        editor.focus();
        editor.layout();
        this.editor = editor;
    }
    setFile(file) {
        var reader = new FileReader();
        reader.onloadend = (event) => {
            this.editor.setValue(reader.result);
        };
        reader.readAsText(file);
    }

    render() {
        const options = {
            selectOnLineNumbers: true,
        };
        return (
            <div>
                <h1>{this.props.dataReference.name}</h1>
                <input
                    type="file"
                    accept=".yaml"
                    onChange={e => this.setFile.bind(this)(e.target.files[0])}
                />
                <MonacoEditor
                    height="300px"
                    automaticLayout="true"
                    language="yaml"
                    value={this.props.dataReference.src}
                    options={options}
                    editorDidMount={this.mount.bind(this)}
                />
            </div>
        );
    }
}