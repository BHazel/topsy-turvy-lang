import LanguageSupport

/// The `CodeEditorView` `LanguageConfiguration` for Topsy Turvy, derived from `TopsyTurvyKeywords`.
extension LanguageConfiguration {
    /// Builds the Topsy Turvy language configuration, used to drive keyword highlighting, diagnostics,
    /// semantic tokens, hover and completion in `TopsyTurvyCodeEditorView`.
    ///
    /// This is a function rather than a shared `static let`, unlike most of this package's other bundled
    /// language configurations, because `languageService` is per-document state (one `TopsyTurvyLanguageService`
    /// per open document) — a `static let` would incorrectly share a single language service across every
    /// open document. Every other property is identical across documents, so reusing the same `name` for
    /// every call is fine: the package only requires unique names for configurations that also differ in a
    /// property other than `languageService`.
    /// - Parameter languageService: The language service backing this document, or `nil` before the
    ///   document's session has finished opening.
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
