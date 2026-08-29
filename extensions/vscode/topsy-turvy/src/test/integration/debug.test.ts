import * as assert from 'assert';
import * as fs from 'fs';
import * as os from 'os';
import * as path from 'path';
import * as vscode from 'vscode';

const source = `HARK! "Debug Test"
PRINCIPALS
THE CURTAIN RISES.
BEHOLD "OK"
FINALE.
`;

suite('Debug Adapter Integration', function () {
    this.timeout(30000);

    let temporaryDirectory: string;

    suiteSetup(function () {
        temporaryDirectory = fs.mkdtempSync(path.join(os.tmpdir(), 'tt-debug-integration-'));
    });

    suiteTeardown(() => {
        fs.rmSync(temporaryDirectory, {
            recursive: true,
            force: true
        });
    });

    test('DebugType_TopsyTurvy_IsRegistered', async () => {
        const debuggers = (vscode.extensions.getExtension('bwhazel.topsy-turvy')?.packageJSON?.contributes?.debuggers ?? []) as Array<{ type: string }>;

        assert.ok(
            debuggers.some((theDebugger) => theDebugger.type === 'topsy-turvy'),
            'Expected a "topsy-turvy" entry under contributes.debuggers in package.json',
        );
    });

    test('StartDebugging_WithLaunchConfiguration_StartsAndTerminatesSession', async () => {
        const temporaryFile = path.join(temporaryDirectory, `debug-${Date.now()}.topsy`);
        fs.writeFileSync(temporaryFile, source, 'utf8');

        const started = new Promise<vscode.DebugSession>((resolve) => {
            const disposable = vscode.debug.onDidStartDebugSession((session) => {
                if (session.type === 'topsy-turvy') {
                    disposable.dispose();
                    resolve(session);
                }
            });
        });

        const terminated = new Promise<void>((resolve) => {
            const disposable = vscode.debug.onDidTerminateDebugSession((session) => {
                if (session.type === 'topsy-turvy') {
                    disposable.dispose();
                    resolve();
                }
            });
        });

        const ok = await vscode.debug.startDebugging(undefined, {
            type: 'topsy-turvy',
            request: 'launch',
            name: 'Debug Test',
            program: temporaryFile,
        });

        assert.ok(ok, 'Expected startDebugging to report success');

        const session = await started;
        assert.strictEqual(session.type, 'topsy-turvy', 'Expected the started session type to be topsy-turvy');

        await vscode.debug.stopDebugging(session);
        await terminated;
    });
});
