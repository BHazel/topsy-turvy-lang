import Combine
import Foundation
import LanguageSupport
import SwiftUI

/// Bridges `TopsyTurvySession` into `CodeEditorView`'s `LanguageService` protocol.
///
/// This is a thin Swift-side adapter, not a reimplementation of any language logic: every method below
/// converts a protocol-shaped call into an existing native call (`session.scheduleAnalyse`/`.tokens`/`.hover`/
/// `.complete`, themselves thin wrappers around `topsyturvy_*` exports) and reshapes the JSON result into the
/// types `CodeEditorView` expects. All parsing, tokenising, type-checking, hover-content and completion-candidate
/// logic stays in the .NET engine, reusable by the CLI, LSP and web editor exactly as before — nothing here is
/// Apple-specific intelligence.
///
/// One instance is owned per open document, alongside that document's `TopsyTurvySession`.
final class TopsyTurvyLanguageService: LanguageService {
    private let session: TopsyTurvySession
    private var currentText: String = ""
    private var locationService: LocationService?

    private(set) var isOpen = false

    let events = PassthroughSubject<LanguageServiceEvent, Never>()
    let diagnostics = CurrentValueSubject<Set<TextLocated<Message>>, Never>([])
    let completionTriggerCharacters = CurrentValueSubject<[Character], Never>([" "])
    let extraActions = CurrentValueSubject<[ExtraAction], Never>([])

    /// Incremented on every `openDocument`/`documentDidChange` call; a stale generation after the debounce
    /// wait means a newer edit has superseded this call, mirroring `TopsyTurvySession.scheduleAnalyse`'s own
    /// guard, applied here since the protocol's own change hooks (not `TopsyTurvyCodeEditorView`) now drive
    /// analysis.
    private var analyseGeneration = 0

    init(session: TopsyTurvySession) {
        self.session = session
    }

    func openDocument(with text: String, locationService: LocationService) async throws {
        self.locationService = locationService
        isOpen = true
        await updateText(text)
    }

    /// A required, but deliberately empty, protocol conformance.
    ///
    /// `newText` here is **not** the whole document — confirmed by reading `CodeStorageDelegate.swift`
    /// directly: it is only `(textStorage.string as NSString).substring(with: editedRange)`, the fragment at
    /// the edited range. An earlier version of this method wrongly treated it as the full document and
    /// assigned it straight to `currentText`, which corrupted every analysis after the first keystroke (every
    /// diagnostic/hover/completion call afterwards ran against a tiny fragment instead of the real document,
    /// producing spurious parse errors like "Expected: HARK!" on genuinely valid source). `TopsyTurvyCodeEditorView`
    /// drives re-analysis instead, via `updateText(_:)` called from its `text` binding's `.onChange` — SwiftUI
    /// is guaranteed to already see the correct, full post-edit document by then, since `CodeStorageDelegate`
    /// calls `setText(textStorage.string)` synchronously before this method's `Task` is even dispatched.
    func documentDidChange(
        position changeLocation: Int,
        changeInLength delta: Int,
        lineChange deltaLine: Int,
        columnChange deltaColumn: Int,
        newText text: String
    ) async throws {
    }

    func closeDocument() async throws {
        isOpen = false
        diagnostics.send([])
    }

    /// Updates the tracked document text and re-analyses, called from `TopsyTurvyCodeEditorView`'s `text`
    /// binding whenever it changes — the single source of truth for the full current document, since the
    /// `LanguageService` protocol's own `documentDidChange` cannot supply it (see that method's doc comment).
    func updateText(_ text: String) async {
        currentText = text
        await refreshDiagnostics(source: text)
    }

    /// Completion support is backed out entirely, on every platform, per explicit user direction.
    ///
    /// `CodeEditorView`'s automatic macOS completion panel (`CodeActions.swift`'s `CompletionPanel`, a real
    /// `NSPanel`) calls `makeKeyAndOrderFront`/`makeFirstResponder` on every appearance, stealing keyboard
    /// focus from the editor on nearly every word typed — confirmed via live use, not hypothetical. A custom
    /// on-demand alternative (a toolbar button on iOS/iPadOS, a keyboard shortcut on macOS) was built and
    /// tried, then also rejected as a worse trade-off ("a terrible developer experience") than the focus
    /// steal it was meant to avoid — a genuine catch-22 with no configuration knob on either side to resolve
    /// it (`CompletionPanel` is `final`/`internal` to the package: not subclassable, not exposed). Returning
    /// `.none` unconditionally here disables the automatic macOS panel outright, since it never has anything
    /// to show; there is no remaining completion UI on any platform.
    func completions(at location: Int, reason: CompletionTriggerReason) async throws -> Completions {
        .none
    }

    /// Semantic tokens are deliberately not supplied; this always throws.
    ///
    /// Throwing — not returning empty arrays — matters: `CodeStorageDelegate.requestSemanticTokens` only
    /// skips its follow-up work on the throw path. On any successful return (even all-empty), it calls
    /// `textStorageObserver.processEditing(edited: .editedAttributes, range: <the requested lines>, ...,
    /// invalidatedRange: <the whole document>)` — and on macOS, `NSTextView` responds to that by moving the
    /// insertion point to the end of the edited range. An earlier implementation both returned real tokens
    /// here *and* emitted `.tokensAvailable(0..<lineCount)` after every debounced analysis pass, making the
    /// edited range the entire document ~400ms after the user paused typing — the live-reported "cursor jumps
    /// to the end of the file" bug on macOS (iOS's `UITextView` does not move its caret on attribute-only
    /// edits, which is why iPadOS was unaffected). Supplying tokens was also already documented as having
    /// almost no visible highlighting benefit: the syntactic pass driven by `reservedIdentifiers` pre-classifies
    /// every keyword before semantic tokens are consulted. The native `topsyturvy_tokens` export and
    /// `SourceTokeniser` remain in the frozen v2 ABI, available to other toolchain components or a future,
    /// symbol-aware classification pass.
    func tokens(for lineRange: Range<Int>) async throws -> [[(token: LanguageConfiguration.Token, range: NSRange)]] {
        throw CancellationError()
    }

    /// Builds an info popover for the given location, automatic on macOS via `CodeActions.swift`'s AppKit-only
    /// `InfoPopover` — a no-op path on iOS/iPadOS, which instead calls `hoverContent(at:)` directly from a
    /// custom long-press gesture.
    func info(at location: Int) async throws -> (view: any View, anchor: NSRange?)? {
        guard let markdown = await hoverContent(at: location) else { return nil }
        return (AnyView(Text(.init(markdown))), nil)
    }

    /// Builds Markdown hover content for the symbol at `location`, shared by `info(at:)` (automatic on macOS)
    /// and the custom iOS/iPadOS long-press hover popover.
    /// - Parameter location: A string index into the current document text.
    /// - Returns: The Markdown content, or `nil` if no symbol was found there.
    func hoverContent(at location: Int) async -> String? {
        guard let locationService, case .success(let textLocation) = locationService.textLocation(from: location) else {
            return nil
        }

        let result = await session.hover(
            source: currentText,
            line: Int32(textLocation.zeroBasedLine),
            column: Int32(textLocation.zeroBasedColumn)
        )
        return result.Found ? result.MarkdownContent : nil
    }

    func capabilities() async throws -> (any View)? {
        nil
    }

    /// Requests a debounced analysis and, if not superseded by a newer edit, replaces the diagnostics subject's
    /// value wholesale (stale decorations are always cleared before applying a new set).
    ///
    /// Deliberately does **not** emit `.tokensAvailable`: that event makes `CodeStorageDelegate` run
    /// `processEditing` with the requested lines as the edited range, and emitting it for the whole document
    /// after every analysis pass moved the macOS insertion point to the end of the file ~400ms after the user
    /// paused typing — see `tokens(for:)`'s doc comment for the full mechanism.
    private func refreshDiagnostics(source: String) async {
        analyseGeneration += 1
        let thisGeneration = analyseGeneration

        guard let result = await session.scheduleAnalyse(source: source), thisGeneration == analyseGeneration else {
            return
        }

        diagnostics.send(Set(result.Diagnostics.map(Self.diagnosticMessage)))
    }

    /// Maps a `DiagnosticInfo` (1-indexed, half-open) onto a `TextLocated<Message>` for `CodeEditor`.
    private static func diagnosticMessage(_ diagnostic: DiagnosticInfo) -> TextLocated<Message> {
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
