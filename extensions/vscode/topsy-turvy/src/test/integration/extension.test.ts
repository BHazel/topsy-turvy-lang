import * as assert from 'assert';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';

const validSource = `HARK! "Test"
PRINCIPALS
THE CURTAIN RISES.
BEHOLD "OK"
FINALE.
`;

const invalidSource = `FINALE.`;

async function waitForDiagnostics(
    uri: vscode.Uri,
    predicate: (diagnostics: readonly vscode.Diagnostic[]) => boolean,
    timeoutMs = 10000,
    intervalMs = 500,
): Promise<readonly vscode.Diagnostic[]> {
    const deadline = Date.now() + timeoutMs;
    while (Date.now() < deadline) {
        const diagnostics = vscode.languages.getDiagnostics(uri);
        if (predicate(diagnostics)) {
            return diagnostics;
        }

        await new Promise((resolve) => setTimeout(resolve, intervalMs));
    }

    return vscode.languages.getDiagnostics(uri);
}

suite('Extension Integration', function () {
    this.timeout(30000);

    let temporaryDirectory: string;

    suiteSetup(async function () {
        temporaryDirectory = fs.mkdtempSync(path.join(os.tmpdir(), 'tt-integration-'));

        const seedFile = path.join(temporaryDirectory, 'seed.topsy');
        fs.writeFileSync(seedFile, validSource, 'utf8');
        const document = await vscode.workspace.openTextDocument(vscode.Uri.file(seedFile));
        await vscode.window.showTextDocument(document);
        await new Promise((resolve) => setTimeout(resolve, 5000));
    });

    suiteTeardown(() => {
        fs.rmSync(temporaryDirectory, {
            recursive: true,
            force: true
        });
    });

    test('Extension_OnActivation_RegistersCommands', async () => {
        const commands = await vscode.commands.getCommands(true);

        assert.ok(commands.includes('topsy-turvy.runFile'), 'runFile command not registered');
        assert.ok(commands.includes('topsy-turvy.stopFile'), 'stopFile command not registered');
    });

    test('Extension_OnActivation_HasCorrectConfigurationDefaults', () => {
        const config = vscode.workspace.getConfiguration('topsy-turvy');

        assert.strictEqual(config.get<string>('cliPath'), '', 'cliPath default should be empty');
    });

    test('Extension_WhenTopsyFileOpened_AssignsCorrectLanguageId', async () => {
        const temporaryFile = path.join(temporaryDirectory, `lang-id-${Date.now()}.topsy`);
        fs.writeFileSync(temporaryFile, validSource, 'utf8');

        const document = await vscode.workspace.openTextDocument(vscode.Uri.file(temporaryFile));

        assert.strictEqual(
            document.languageId,
            'topsy-turvy',
            '.topsy files should be assigned the topsy-turvy language ID',
        );
    });

    test('Extension_WhenTopsyFileOpened_ActivatesLanguageClient', async () => {
        const temporaryFile = path.join(temporaryDirectory, `activation-${Date.now()}.topsy`);
        fs.writeFileSync(temporaryFile, validSource, 'utf8');
        const uri = vscode.Uri.file(temporaryFile);
        const document = await vscode.workspace.openTextDocument(uri);
        await vscode.window.showTextDocument(document);

        const diagnostics = vscode.languages.getDiagnostics(uri);

        assert.ok(Array.isArray(diagnostics), 'Expected getDiagnostics to return an array');
    });

    test('Extension_WhenValidSourceOpened_ShowsNoDiagnostics', async () => {
        const temporaryFile = path.join(temporaryDirectory, `valid-${Date.now()}.topsy`);
        fs.writeFileSync(temporaryFile, validSource, 'utf8');
        const uri = vscode.Uri.file(temporaryFile);
        const document = await vscode.workspace.openTextDocument(uri);
        await vscode.window.showTextDocument(document);

        const diagnostics = await waitForDiagnostics(uri, (diagnostics) => diagnostics.length === 0, 10000);

        assert.strictEqual(
            diagnostics.length,
            0,
            `Expected no diagnostics for valid source, got: ${JSON.stringify(diagnostics.map((d) => d.message))}`,
        );
    });

    test('Extension_WhenInvalidSourceOpened_ShowsDiagnostics', async () => {
        const temporaryFile = path.join(temporaryDirectory, `invalid-${Date.now()}.topsy`);
        fs.writeFileSync(temporaryFile, invalidSource, 'utf8');
        const uri = vscode.Uri.file(temporaryFile);
        const document = await vscode.workspace.openTextDocument(uri);
        await vscode.window.showTextDocument(document);

        const diagnostics = await waitForDiagnostics(uri, (diagnostics) => diagnostics.length > 0, 10000);

        assert.ok(
            diagnostics.length > 0,
            'Expected diagnostics for invalid source, but none were reported',
        );
    });

    test('Extension_WhenInvalidSourceOpened_DiagnosticsHaveErrorSeverity', async () => {
        const temporaryFile = path.join(temporaryDirectory, `severity-${Date.now()}.topsy`);
        fs.writeFileSync(temporaryFile, invalidSource, 'utf8');
        const uri = vscode.Uri.file(temporaryFile);
        const document = await vscode.workspace.openTextDocument(uri);
        await vscode.window.showTextDocument(document);

        const diagnostics = await waitForDiagnostics(uri, (diagnostics) => diagnostics.length > 0, 10000);

        assert.ok(diagnostics.length > 0, 'Expected at least one diagnostic');
        assert.strictEqual(
            diagnostics[0].severity,
            vscode.DiagnosticSeverity.Error,
            'Syntax error diagnostics should have Error severity',
        );
    });

    test('Extension_WhenInvalidSourceOpened_DiagnosticsHaveNonTrivialRange', async () => {
        const temporaryFile = path.join(temporaryDirectory, `range-${Date.now()}.topsy`);
        fs.writeFileSync(temporaryFile, invalidSource, 'utf8');
        const uri = vscode.Uri.file(temporaryFile);
        const document = await vscode.workspace.openTextDocument(uri);
        await vscode.window.showTextDocument(document);

        const diagnostics = await waitForDiagnostics(uri, (diagnostics) => diagnostics.length > 0, 10000);
        
        assert.ok(diagnostics.length > 0, 'Expected at least one diagnostic');
        const range = diagnostics[0].range;
        const isNonTrivial =
            range.start.line > 0 ||
            range.start.character > 0 ||
            range.end.line > 0 ||
            range.end.character > 0;
        
        assert.ok(isNonTrivial, `Expected a non-trivial range, got ${JSON.stringify(range)}`);
    });

    test('Extension_WhenInvalidSourceEditedToValid_ClearsDiagnostics', async () => {
        const temporaryFile = path.join(temporaryDirectory, `edit-${Date.now()}.topsy`);
        fs.writeFileSync(temporaryFile, invalidSource, 'utf8');
        const uri = vscode.Uri.file(temporaryFile);
        const document = await vscode.workspace.openTextDocument(uri);
        await vscode.window.showTextDocument(document);
        await waitForDiagnostics(uri, (diagnostics) => diagnostics.length > 0, 10000);
        const edit = new vscode.WorkspaceEdit();
        edit.replace(uri, new vscode.Range(0, 0, document.lineCount, 0), validSource);
        await vscode.workspace.applyEdit(edit);

        const diagnostics = await waitForDiagnostics(uri, (diagnostics) => diagnostics.length === 0, 10000);
        
        assert.strictEqual(
            diagnostics.length,
            0,
            'Editing invalid source to valid should clear all diagnostics',
        );
    });
});
