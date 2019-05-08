import React, { Component } from 'react';
import brace from 'brace';
import AceEditor from 'react-ace';
import 'brace/mode/yaml';
import 'brace/theme/tomorrow';

export class Editor extends Component {
    static displayName = Editor.name;
    //componentDidMount() {
    //    const customMode = new CustomSqlMode();
    //    this.refs.aceEditor.editor.getSession().setMode(customMode);
    //}
    //componentWillUnmount() {
    //    this.props.dataReference.save(this.refs.aceEditor.getCode());
    //}
    //mount(editor, monaco) {
    //    editor.focus();
    //    editor.layout();
    //    this.editor = editor;
    //}

    setFile(file) {
        var reader = new FileReader();
        reader.onloadend = (event) => {
            this.refs.aceEditor.setValue(reader.result);
        };
        reader.readAsText(file);
    }

    render() {
        return (
            <div>
                <h1>{this.props.dataReference.name}</h1>
                <input
                    type="file"
                    accept=".yaml"
                    onChange={e => this.setFile.bind(this)(e.target.files[0])}
                />
                <AceEditor
                    ref="aceEditor"
                    mode="yaml"
                    theme="tomorrow"
                    name="editor"
                    value={this.props.dataReference.data}
                    fontSize={14}
                    editorProps={{ $blockScrolling: false }}
                />
            </div>
        );
    }
}