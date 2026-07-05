import Foundation
import TopsyTurvyToolchain

/// Wraps one native `topsyturvy_session`, owned per open document.
///
/// All native calls run on the actor's executor (off the main thread); callers that update UI state from
/// `onOutputLine` must hop to `@MainActor` themselves. `cancel()` is `nonisolated` and reads the session
/// handle without actor isolation, since the header documents `topsyturvy_cancel` as the one export safe to
/// call from any thread while another call (e.g. a long-running `execute`) is in flight on the actor.
actor TopsyTurvySession {
    /// The native session handle, or `nil` before `open()`/after `close()`.
    ///
    /// Marked `nonisolated(unsafe)` so `cancel()` can read it without actor isolation — safe because the
    /// native contract guarantees `topsyturvy_cancel` may be called concurrently with any other export.
    private nonisolated(unsafe) var session: UnsafeMutableRawPointer?

    private let callbackBox = SessionCallbackBox()
    private let contextPointer: UnsafeMutableRawPointer

    /// Incremented on every `scheduleAnalyse` call; a stale generation after the debounce wait means a newer
    /// edit has superseded this call, so its result should be discarded.
    private var analyseGeneration = 0

    /// Registers a handler invoked (off the main thread) for each output line the running programme writes.
    func setOutputHandler(_ handler: @escaping (_ text: String, _ suppressNewline: Bool) -> Void) {
        callbackBox.onOutputLine = handler
    }

    /// Registers a handler that resolves a `PRAY ADMIT` import filename to its source text, or `nil` if it
    /// cannot be resolved.
    func setImportResolver(_ resolver: @escaping (_ filename: String) -> String?) {
        callbackBox.onResolveImport = resolver
    }

    init() {
        self.contextPointer = Unmanaged.passUnretained(callbackBox).toOpaque()
    }

    /// Creates the native session, if not already open.
    func open() {
        guard session == nil else { return }
        session = topsyturvy_session_create(sessionOutputLineCallback, sessionResolveImportCallback, contextPointer)
    }

    /// Destroys the native session and releases any outstanding import buffer.
    func close() {
        if let session {
            topsyturvy_session_destroy(session)
        }
        session = nil
        callbackBox.freePendingImportBuffer()
    }

    deinit {
        if let session {
            topsyturvy_session_destroy(session)
        }
    }

    /// Cooperatively cancels the session's current execution, if any. Safe to call from any thread while
    /// `execute` is in flight on the actor.
    nonisolated func cancel() {
        if let session {
            topsyturvy_cancel(session)
        }
    }

    /// Analyses `source` after a 400ms single-flight debounce, discarding the result if a newer call to this
    /// method arrives before the wait completes.
    /// - Parameter source: The Topsy Turvy source to analyse.
    /// - Returns: The analysis result, or `nil` if superseded by a newer call.
    func scheduleAnalyse(source: String) async -> AnalysisResult? {
        analyseGeneration += 1
        let thisGeneration = analyseGeneration

        try? await Task.sleep(for: .milliseconds(400))
        guard thisGeneration == analyseGeneration else { return nil }

        return analyse(source: source)
    }

    /// Parses and type-checks `source` immediately, with no debounce.
    func analyse(source: String) -> AnalysisResult {
        guard let session else { return AnalysisResult(Success: true, Diagnostics: []) }
        return withUTF8CString(source) { sourcePointer in
            decodeJSON(AnalysisResult.self, from: topsyturvy_analyse(session, sourcePointer))
                ?? AnalysisResult(Success: true, Diagnostics: [])
        }
    }

    /// Builds Markdown hover content for the symbol at the given 0-indexed line/column.
    func hover(source: String, line: Int32, column: Int32) -> HoverResult {
        guard let session else { return HoverResult(Found: false, MarkdownContent: nil) }
        return withUTF8CString(source) { sourcePointer in
            decodeJSON(HoverResult.self, from: topsyturvy_hover(session, sourcePointer, line, column))
                ?? HoverResult(Found: false, MarkdownContent: nil)
        }
    }

    /// Builds keyword and symbol completion candidates for the given 0-indexed line/column. Keywords are
    /// always offered, even when the source fails to parse.
    func complete(source: String, line: Int32, column: Int32) -> CompletionResult {
        guard let session else { return CompletionResult(Items: []) }
        return withUTF8CString(source) { sourcePointer in
            decodeJSON(CompletionResult.self, from: topsyturvy_complete(session, sourcePointer, line, column))
                ?? CompletionResult(Items: [])
        }
    }

    /// Formats `source` with canonical keyword casing and libretto indentation.
    /// - Returns: The formatted source text, or `nil` if the session is not open or an exception was thrown.
    func format(source: String) -> String? {
        guard let session else { return nil }
        return withUTF8CString(source) { sourcePointer in
            guard let pointer = topsyturvy_format(session, sourcePointer) else { return nil }
            defer { topsyturvy_free(pointer) }
            return String(cString: pointer)
        }
    }

    /// Parses, type-checks and interprets `source`, streaming output via `onOutputLine`.
    /// - Parameter source: The Topsy Turvy programme source.
    /// - Parameter args: The arguments exposed as `THE PROPS`.
    /// - Parameter stdin: Newline-delimited input pre-seeding `PRAY TELL`, or `nil` for none.
    /// - Returns: `0` success, `1` parse failure, `2` type-check failure, `3` exception/runtime error, `4` cancelled.
    func execute(source: String, args: [String] = [], stdin: String? = nil) -> Int32 {
        guard let session else { return 3 }

        let argsJSON: String? = args.isEmpty
            ? nil
            : (try? JSONEncoder().encode(args)).flatMap { String(data: $0, encoding: .utf8) }

        return withUTF8CString(source) { sourcePointer in
            withOptionalUTF8CString(argsJSON) { argsPointer in
                withOptionalUTF8CString(stdin) { stdinPointer in
                    topsyturvy_execute(session, sourcePointer, argsPointer, stdinPointer)
                }
            }
        }
    }

    /// The message of the most recently caught exception, or `nil` if none has occurred.
    func lastError() -> String? {
        guard let session, let pointer = topsyturvy_last_error(session) else { return nil }
        defer { topsyturvy_free(pointer) }
        return String(cString: pointer)
    }
}

/// Calls `topsyturvy_free` on `pointer` and decodes its JSON contents, if non-`nil`.
private func decodeJSON<T: Decodable>(_ type: T.Type, from pointer: UnsafeMutablePointer<UInt8>?) -> T? {
    guard let pointer else { return nil }
    defer { topsyturvy_free(pointer) }

    guard let data = String(cString: pointer).data(using: .utf8) else { return nil }
    return try? JSONDecoder().decode(T.self, from: data)
}

/// Calls `body` with `string`'s null-terminated UTF-8 representation.
private func withUTF8CString<R>(_ string: String, _ body: (UnsafePointer<UInt8>) -> R) -> R {
    string.withCString { pointer in
        pointer.withMemoryRebound(to: UInt8.self, capacity: string.utf8.count + 1) { body($0) }
    }
}

/// Calls `body` with `string`'s null-terminated UTF-8 representation, or `nil` if `string` is `nil`.
private func withOptionalUTF8CString<R>(_ string: String?, _ body: (UnsafePointer<UInt8>?) -> R) -> R {
    guard let string else { return body(nil) }
    return withUTF8CString(string) { body($0) }
}
