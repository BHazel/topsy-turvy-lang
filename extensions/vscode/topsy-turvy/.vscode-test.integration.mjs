import * as os from 'os';
import * as path from 'path';
import { defineConfig } from '@vscode/test-cli';

export default defineConfig([
    {
        workspaceFolder: '.',
        extensionDevelopmentPath: '.',
        files: 'out/test/integration/**/*.test.js',
        version: 'stable',
        launchArgs: ['--user-data-dir', path.join(os.tmpdir(), 'tt-vsc-test')],
    },
]);
