import LanguageSupport

/// The `CodeEditorView` `LanguageConfiguration` for Topsy Turvy.
extension LanguageConfiguration {
    /// Builds the Topsy Turvy language configuration, used to drive keyword highlighting, diagnostics,
    /// semantic tokens, hover and completion in `TopsyTurvyCodeEditorView`.
    ///
    /// - Parameters:
    ///   - languageService: The language service backing this document, or `nil` before the
    ///   document session has finished opening.
    ///
    /// - Returns: The language configuration for Topsy Turvy.
    static func topsyTurvy(languageService: LanguageService?) -> LanguageConfiguration {
        LanguageConfiguration(
            name: "Topsy Turvy",
            supportsSquareBrackets: false,
            supportsCurlyBrackets: false,
            caseInsensitiveReservedIdentifiers: false,
            stringRegex: /"(?:~.|[^"\\])*"/,
            characterRegex: /'(?:~.|[^'])'/,
            numberRegex: /-?[0-9]+(?:\.[0-9]+)?/,
            singleLineComment: "ASIDE:",
            nestedComment: (open: "(ASIDE, AT SOME LENGTH:", close: "END OF ASIDE.)"),
            identifierRegex: /[A-Za-z][A-Za-z0-9_-]*/,
            operatorRegex: nil,
            reservedIdentifiers: TopsyTurvyKeywords.reservedWords,
            reservedOperators: [],
            languageService: languageService
        )
    }
}
