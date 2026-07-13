import Combine
import Foundation
import LanguageSupport
import SwiftUI

/// Adapter for a `TopsyTurvySession` and the `CodeEditorView` `LanguageService` protocol.
///
/// One instance is owned per open document, alongside the `TopsyTurvySession` for that document.  It converts
/// between the types expected by the native Topsy Turvy toolchain and `CodeEditorView`, including between a
/// plain character position (an `Int`) and a line/column position; `CodeEditorView` provides and maintains
/// that conversion, so this service does not track line breaks itself.
final class TopsyTurvyLanguageService: LanguageService {
    /// The Topsy Turvy toolchain session backing this service.
    private let session: TopsyTurvySession

    /// The current text of the open source file.
    private var currentText: String = ""

    /// The location service supplied by `CodeEditorView` for the open document.
    private var locationService: LocationService?

    /// A value indicating whether a source file is currently open.
    private(set) var isOpen = false

    /// A stream of one-off notifications for `CodeEditorView` to react to.
    ///
    /// The only kind of notification currently defined is new syntax highlighting ready for specified lines.
    /// This service never sends one, since it does not supply its own highlighting; it is declared only
    /// because every `LanguageService` is required to have one.
    let events = PassthroughSubject<LanguageServiceEvent, Never>()

    /// The diagnostics for the open source file.
    let diagnostics = CurrentValueSubject<Set<TextLocated<Message>>, Never>([])

    /// Characters that should make the `CodeEditorView` built-in completion panel ask for completions, in
    /// addition to normal identifier characters.
    ///
    /// That panel only exists on macOS, which this app does not target, so this setting has no real effect and is
    /// left at the default of a single space.
    let completionTriggerCharacters = CurrentValueSubject<[Character], Never>([" "])

    /// Extra menu actions `CodeEditorView` can show for this language.
    ///
    /// This is only supported on macOS, so this app does not add any; its own toolbar buttons and menu
    /// commands already cover what it needs.
    let extraActions = CurrentValueSubject<[ExtraAction], Never>([])

    /// Counts how many times `refreshDiagnostics` has been requested to run.  If a newer request comes in before an
    /// older one finishes, the older one checks this count and skips applying its now-outdated result.
    private var analysisGenerationCount = 0

    /// Creates a `TopsyTurvyLanguageService` with the provided `TopsyTurvySession`.
    ///
    /// - Parameters:
    ///   - session: The Topsy Turvy toolchain session.
    init(session: TopsyTurvySession) {
        self.session = session
    }

    /// Opens a Topsy Turvy source file.
    ///
    /// - Parameters:
    ///   - text: The source file text.
    ///   - locationService: The `CodeEditorView` location service.
    func openDocument(with text: String, locationService: LocationService) async throws {
        self.locationService = locationService
        self.isOpen = true
        await self.updateText(text)
    }

    /// Handler for changes in the document.
    ///
    /// This is intentionally not implemented.  The `text` parameter is only the small piece of the document that
    /// changed, not the whole file.  Treating it as a full file would only check the small piece which would
    /// incorrectly reject valid code after an initial keystroke.  The `updateText(_:)` should be used instead.
    ///
    /// - Parameters:
    ///   - changeLocation: The string index at which the change starts.
    ///   - delta: The change in the overall document length, in characters.
    ///   - deltaLine: The change in the number of lines.
    ///   - deltaColumn: The change in the column position on the last line of the changed text.
    ///   - text: The document fragment at `changeLocation` after the change, not the whole document.
    func documentDidChange(
        position changeLocation: Int,
        changeInLength delta: Int,
        lineChange deltaLine: Int,
        columnChange deltaColumn: Int,
        newText text: String
    ) async throws {
    }

    /// Closes the open Topsy Turvy source file.
    func closeDocument() async throws {
        self.isOpen = false
        await MainActor.run {
            self.diagnostics.send([])
        }
    }

    /// Updates the open source file text and re-analyses it.
    ///
    /// This is called from `TopsyTurvyCodeEditorView` `text` binding whenever it changes: the single source
    /// of truth for the full current document.
    ///
    /// - Parameters:
    ///   - text: The open source file text.
    func updateText(_ text: String) async {
        self.currentText = text
        await self.refreshDiagnostics(source: text)
    }

    /// Returns completions detail.
    ///
    /// This is not implemented as the `CodeEditorView` built-in completion panel, which this method feeds, only exists
    /// on macOS, which this app does not target. The completions the user actually sees come from a separate,
    /// always-visible bar above the keyboard (`CompletionBarView` and `CompletionBarController`) which does not
    /// call this method at all.
    ///
    /// - Parameters:
    ///   - location: The location for the completions.
    ///   - reason: The reason for the completions.
    ///
    /// - Returns: `Completions.none`, always.
    func completions(at location: Int, reason: CompletionTriggerReason) async throws -> Completions {
        .none
    }

    /// Provides tokens for syntax highlighting.
    ///
    /// As this service does not supply syntax highlighting information, this method throws an error if called.
    ///
    /// Although not supported, on macOS a successful call resulted in `CodeEditorView` treating a requested
    /// line as freshly edited and would move the cursor to the end of those lines.
    ///
    /// - Parameters:
    ///   - lineRange: The line range semantic tokens are being requested for.
    ///
    /// - Returns: Tokens for syntax highlighting.
    ///
    /// - Throws: `CancellationError`, always.
    func tokens(for lineRange: Range<Int>) async throws -> [[(token: LanguageConfiguration.Token, range: NSRange)]] {
        throw CancellationError()
    }

    /// Builds an info popover for the given location.
    ///
    /// This is automatic on macOS but requires a custom long-press gesture on iOS/iPadOS.
    ///
    /// - Parameters:
    ///   - at: The location for the info popup.
    ///
    /// - Returns: A view-range tuple of the info popup view and anchor.
    func info(at location: Int) async throws -> (view: any View, anchor: NSRange?)? {
        guard let markdown = await self.hoverContent(at: location) else {
            return nil
        }
        
        return (AnyView(Text(.init(markdown))), nil)
    }

    /// Builds Markdown hover content for a symbol at the specified location.
    ///
    /// Used by `info(at:)` when creating the info popup.
    ///
    /// - Parameters:
    ///   - location: The location in the source text.
    ///
    /// - Returns: The Markdown content, or `nil` if no symbol was found at the location.
    func hoverContent(at location: Int) async -> String? {
        guard let locationService, case .success(let textLocation) = locationService.textLocation(from: location) else {
            return nil
        }

        let result = await self.session.hover(
            source: self.currentText,
            line: Int32(textLocation.zeroBasedLine),
            column: Int32(textLocation.zeroBasedColumn)
        )
        
        return result.Found
            ? result.MarkdownContent
            : nil
    }

    /// Shows a view for debugging within `CodeEditorView`.
    ///
    /// `CodeEditorView` may show this view somewhere in its interface to help developers inspect the language
    /// service state.  There is no fixed format: it is up to each language service.
    ///
    /// - Returns: Any view, `nil` for this service.
    func capabilities() async throws -> (any View)? {
        nil
    }

    /// Refreshes the diagnostics for the source text.
    ///
    /// Replaces the diagnostics completely: stale decorations are always cleared before applying a new set.
    ///
    /// - Parameters:
    ///   - source: The source text.
    private func refreshDiagnostics(source: String) async {
        self.analysisGenerationCount += 1
        let thisGeneration = self.analysisGenerationCount

        guard let result = await session.scheduleAnalysis(source: source), thisGeneration == self.analysisGenerationCount else {
            return
        }

        let messages = Set(result.Diagnostics.map(Self.toCodeEditorDiagnosticMessage))
        await MainActor.run {
            self.diagnostics.send(messages)
        }
    }
    
    /// Converts a `DiagnosticInfo` into a `CodeEditorView` `TextLocated<Message>`.
    ///
    /// `DiagnosticInfo` is 1-indexed and half-open as used in the Topsy Turvy toolchain.
    ///
    /// - Parameters:
    ///   - diagnostic: The `DiagnosticInfo` from the Topsy Turvy toolchain.
    ///
    /// - Returns: A `TextLocated<Message>` for `CodeEditorView` view.
    private static func toCodeEditorDiagnosticMessage(_ diagnostic: DiagnosticInfo) -> TextLocated<Message> {
        let location = TextLocation(oneBasedLine: diagnostic.StartLine, column: diagnostic.StartColumn)
        let length = diagnostic.StartLine == diagnostic.EndLine
            ? max(1, diagnostic.EndColumn - diagnostic.StartColumn)
            : 1
        
        let category: Message.Category = switch diagnostic.Severity {
            case "Error": .error
            case "Warning": .warning
            default: .informational
        }
        
        let message = Message(category: category, length: length, summary: diagnostic.Message, description: nil)
        return TextLocated(location: location, entity: message)
    }
}
