import * as assert from 'assert';

import { resolveCliPath } from '../../paths.js';

suite('Path Resolution', () => {
    const extensionPath = '/fake/extension/path';

    test('resolveCliPath_WhenSettingIsEmpty_ReturnsDefaultPath', () => {
        const binaryName = process.platform === 'win32'
            ? 'operetta.exe'
            : 'operetta';

        const result = resolveCliPath('', extensionPath);

        assert.ok(
            result.endsWith(binaryName),
            `Expected path to end with "${binaryName}", got "${result}"`,
        );
    });

    test('resolveCliPath_WhenSettingIsUndefined_ReturnsDefaultPath', () => {
        const binaryName = process.platform === 'win32'
            ? 'operetta.exe'
            : 'operetta';

        const result = resolveCliPath(undefined, extensionPath);

        assert.ok(
            result.endsWith(binaryName),
            `Expected path to end with "${binaryName}", got "${result}"`,
        );
    });

    test('resolveCliPath_WhenSettingIsProvided_ReturnsSettingValue', () => {
        const customPath = '/custom/path/to/operetta';

        const result = resolveCliPath(customPath, extensionPath);

        assert.strictEqual(result, customPath);
    });

    test('resolveCliPath_WhenNoBundledBinary_FallsBackToDevLayout', () => {
        const result = resolveCliPath('', extensionPath);

        assert.ok(
            result.includes('Debug'),
            `Expected dev-layout fallback path to contain "Debug", got "${result}"`,
        );
    });
});
