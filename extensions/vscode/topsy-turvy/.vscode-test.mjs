import * as os from 'os';
import * as path from 'path';
import { defineConfig } from '@vscode/test-cli';

export default defineConfig([{
    files: 'out/test/unit/**/*.test.js',
    launchArgs: ['--user-data-dir', path.join(os.tmpdir(), 'tt-vsc-test-unit')],
}]);

