import * as assert from 'assert';

import { resolveCliPath, resolveServerPath } from '../../paths.js';

suite('Path Resolution', () => {
    const extensionPath = '/fake/extension/path';

    test('resolveCliPath_WhenSettingIsEmpty_ReturnsDefaultPath', () => {
        const binaryName = process.platform === 'win32'
            ? 'operetta.exe'
            : 'operetta';

        const result = resolveCliPath('', 'Debug', extensionPath);

        assert.ok(
            result.endsWith(binaryName),
            `Expected path to end with "${binaryName}", got "${result}"`,
        );
    });

    test('resolveCliPath_WhenSettingIsUndefined_ReturnsDefaultPath', () => {
        const binaryName = process.platform === 'win32'
            ? 'operetta.exe'
            : 'operetta';

        const result = resolveCliPath(undefined, 'Debug', extensionPath);

        assert.ok(
            result.endsWith(binaryName),
            `Expected path to end with "${binaryName}", got "${result}"`,
        );
    });

    test('resolveCliPath_WhenSettingIsProvided_ReturnsSettingValue', () => {
        const customPath = '/custom/path/to/operetta';

        const result = resolveCliPath(customPath, 'Debug', extensionPath);

        assert.strictEqual(result, customPath);
    });

    test('resolveCliPath_WhenBuildConfigIsRelease_DefaultPathContainsRelease', () => {
        const result = resolveCliPath('', 'Release', extensionPath);

        assert.ok(
            result.includes('Release'),
            `Expected default path to contain "Release", got "${result}"`,
        );
    });

    test('resolveServerPath_WhenSettingIsEmpty_ReturnsDefaultPath', () => {
        const result = resolveServerPath('', 'Debug', extensionPath);

        assert.ok(
            result.includes('BWHazel.TopsyTurvy.LanguageServer'),
            `Expected path to contain "BWHazel.TopsyTurvy.LanguageServer", got "${result}"`,
        );
    });

    test('resolveServerPath_WhenSettingIsUndefined_ReturnsDefaultPath', () => {
        const result = resolveServerPath(undefined, 'Debug', extensionPath);

        assert.ok(
            result.includes('BWHazel.TopsyTurvy.LanguageServer'),
            `Expected path to contain "BWHazel.TopsyTurvy.LanguageServer", got "${result}"`,
        );
    });

    test('resolveServerPath_WhenSettingIsProvided_ReturnsSettingValue', () => {
        const customPath = '/custom/path/to/server';

        const result = resolveServerPath(customPath, 'Debug', extensionPath);

        assert.strictEqual(result, customPath);
    });

    test('resolveServerPath_WhenBuildConfigIsRelease_DefaultPathContainsRelease', () => {
        const result = resolveServerPath('', 'Release', extensionPath);

        assert.ok(
            result.includes('Release'),
            `Expected default path to contain "Release", got "${result}"`,
        );
    });
});
