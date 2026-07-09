import Foundation
import TopsyTurvyToolchain

/// Wraps a native `topsyturvy_session` to interact with the Topsy Turvy toolchain functions.
///
/// All native calls run on the actor executor off the main thread.  Any callers that update UI state
/// from the `onOutputLine` handler must hop to `@MainActor` themselves.
actor TopsyTurvySession {
    /// The native session handle.
    ///
    /// This will be `nil` before `open()` and after `close()`.
    ///
    /// It is marked `nonisolated(unsafe)` so `cancel()` can read it without actor isolation.  This is safe because the
    /// native contract guarantees `topsyturvy_cancel` may be called concurrently with any other export.
    private nonisolated(unsafe) var session: UnsafeMutableRawPointer?

    /// The callback targets registered with the native session.
    private let sessionCallbacks = SessionCallbacks()

    /// The opaque context pointer routing native callbacks back to `sessionCallbacks`.
    private let contextPointer: UnsafeMutableRawPointer

    /// Incremented on every call to `scheduleAnalyse` to request an analysis.  A stale generation after the
    /// debounce wait means a newer edit has superseded this call, so its result should be discarded.
    private var analysisGenerationCount = 0

    /// Registers a handler for each output line the running programme writes.
    ///
    /// This is invoked off the main thread.
    ///
    /// - Parameters:
    ///   - handler: The handler to register.
    func setOutputHandler(_ handler: @escaping (_ text: String, _ suppressNewline: Bool) -> Void) {
        self.sessionCallbacks.onOutputLine = handler
    }

    /// Registers a handler that resolves a `PRAY ADMIT` import filename to its source text, or `nil` if it
    /// cannot be resolved.
    ///
    /// - Parameters:
    ///   - resolver: The resolver to register.
    func setImportResolver(_ resolver: @escaping (_ filename: String) -> String?) {
        self.sessionCallbacks.onResolveImport = resolver
    }

    /// Creates a new session for interacting with the native Topsy Turvy toolchain.
    init() {
        self.contextPointer = Unmanaged.passUnretained(sessionCallbacks).toOpaque()
    }

    /// Creates a native session, if not already open.
    func open() {
        guard self.session == nil else {
            return
        }
        
        self.session = topsyturvy_session_create(sessionOutputLineCallback, sessionResolveImportCallback, contextPointer)
    }

    /// Destroys the native session and releases any outstanding import buffer.
    func close() {
        if let session {
            topsyturvy_session_destroy(session)
        }
        
        self.session = nil
        self.sessionCallbacks.freePendingImportBuffer()
    }

    /// Deinitialises the session and frees the underlying native resources.
    deinit {
        if let session {
            topsyturvy_session_destroy(session)
        }
    }

    /// Cancels a current execution, if any, for this session.
    ///
    /// This is safe to call from any thread while `execute` is in flight on the actor.
    nonisolated func cancel() {
        if let session {
            topsyturvy_cancel(session)
        }
    }

    /// Schedules an anslysis of the source text after a 400ms debounce, discarding the result if a newer call
    /// arrives prior to the wait completing.
    ///
    /// - Parameters:
    ///   - source: The Topsy Turvy source to analyse.
    ///
    /// - Returns: The analysis result, or `nil` if superseded by a newer call.
    func scheduleAnalysis(source: String) async -> AnalysisResult? {
        self.analysisGenerationCount += 1
        let thisGeneration = self.analysisGenerationCount

        try? await Task.sleep(for: .milliseconds(400))
        guard thisGeneration == analysisGenerationCount else {
            return nil
        }

        return self.analyse(source: source)
    }

    /// Parses and type-checks the source text.
    ///
    /// - Parameters:
    ///   - source: The Topsy Turvy source to analyse.
    ///
    /// - Returns: The analysis result.
    func analyse(source: String) -> AnalysisResult {
        guard let session else {
            return AnalysisResult(Success: true, Diagnostics: [])
        }
        
        return withUTF8CString(source) { sourcePointer in
            decodeJSON(AnalysisResult.self, from: topsyturvy_analyse(session, sourcePointer))
                ?? AnalysisResult(Success: true, Diagnostics: [])
        }
    }

    /// Builds Markdown hover content for the symbol at the given 0-indexed line/column.
    ///
    /// - Parameters:
    ///   - source: The source text.
    ///   - line: The 0-indexed line.
    ///   - column: The 0-indexed column.
    ///
    /// - Returns: The hover result.
    func hover(source: String, line: Int32, column: Int32) -> HoverResult {
        guard let session else {
            return HoverResult(Found: false, MarkdownContent: nil)
        }
        
        return withUTF8CString(source) { sourcePointer in
            decodeJSON(HoverResult.self, from: topsyturvy_hover(session, sourcePointer, line, column))
                ?? HoverResult(Found: false, MarkdownContent: nil)
        }
    }

    /// Builds keyword and symbol completion candidates for the given 0-indexed line/column.
    ///
    /// Keywords are always offered, even when the source fails to parse.
    ///
    /// - Parameters:
    ///   - source: The source text.
    ///   - line: The 0-indexed line.
    ///   - column: The 0-indexed column.
    ///
    /// - Returns: The completions result with the completion candidates.
    func completions(source: String, line: Int32, column: Int32) -> CompletionResult {
        guard let session else { return CompletionResult(Items: []) }
        return withUTF8CString(source) { sourcePointer in
            decodeJSON(CompletionResult.self, from: topsyturvy_complete(session, sourcePointer, line, column))
                ?? CompletionResult(Items: [])
        }
    }

    /// Formats the source text with canonical keyword casting and libretto-style indentation.
    ///
    /// - Parameters:
    ///   - source: The source text.
    ///
    /// - Returns: The formatted source text, or `nil` if the session is not open or an exception was thrown.
    func format(source: String) -> String? {
        guard let session else {
            return nil
        }
        
        return withUTF8CString(source) { sourcePointer in
            guard let pointer = topsyturvy_format(session, sourcePointer) else {
                return nil
            }
            
            defer {
                topsyturvy_free(pointer)
            }
            
            return String(cString: pointer)
        }
    }
    
    /// Executes the source text.
    ///
    /// The source code is parsed and type-checked prior to execution.
    /// - Parameters:
    ///   - source: The source text.
    ///   - arguments: The command-line arguments exposed as `THE PROPS`.
    ///   - stdin: Newline-delimited input pre-seeding standard input as read by `PRAY TELL`, or `nil` for none.
    ///
    /// - Returns: `0` for a successful execution , `1` for a parse failure, `2` for a type-check failure, `3` for an exception/runtime error or `4` if the execution was cancelled.
    func execute(source: String, arguments: [String] = [], stdin: String? = nil) -> Int32 {
        guard let session else {
            return 3
        }

        let argumentsJSON: String? = arguments.isEmpty
            ? nil
        : (try? JSONEncoder().encode(arguments)).flatMap {
            String(data: $0, encoding: .utf8)
        }

        return withUTF8CString(source) { sourcePointer in
            withOptionalUTF8CString(argumentsJSON) { argumentsPointer in
                withOptionalUTF8CString(stdin) { stdinPointer in
                    topsyturvy_execute(session, sourcePointer, argumentsPointer, stdinPointer)
                }
            }
        }
    }

    /// Gets the message of the most recently caught exception, or `nil` if none has occurred.
    ///
    /// - Returns: The message of the most recently caught exception.
    func lastError() -> String? {
        guard let session, let pointer = topsyturvy_last_error(session) else {
            return nil
        }
        
        defer {
            topsyturvy_free(pointer)
        }
        
        return String(cString: pointer)
    }
}

/// Decodes a JSON object returned from the Topsy Turvy toolchain.
///
/// - Parameters:
///   - type: The concrete type to decode the JSON object into.
///   - from: The pointer to the JSON object.
///
/// - Returns: The decoded JSON object, or `nil` if an exception occurs.
private func decodeJSON<T: Decodable>(_ type: T.Type, from pointer: UnsafeMutablePointer<UInt8>?) -> T? {
    guard let pointer else {
        return nil
    }
    
    defer {
        topsyturvy_free(pointer)
    }

    guard let data = String(cString: pointer).data(using: .utf8) else {
        return nil
    }
    
    return try? JSONDecoder().decode(T.self, from: data)
}

/// Calls `body` with the null-terminated UTF-8 representation of `string`.
///
/// - Parameters:
///   - string: The string to convert.
///   - body: The closure to call with the converted string.
///
/// - Returns: The result of `body`.
private func withUTF8CString<R>(_ string: String, _ body: (UnsafePointer<UInt8>) -> R) -> R {
    string.withCString { pointer in
        pointer.withMemoryRebound(to: UInt8.self, capacity: string.utf8.count + 1) { body($0) }
    }
}

/// Calls `body` with the null-terminated UTF-8 representation of `string`, or `nil` if `string` is `nil`.
///
/// - Parameters:
///   - string: The string to convert, or `nil`.
///   - body: The closure to call with the converted string.
///
/// - Returns: The result of `body`.
private func withOptionalUTF8CString<R>(_ string: String?, _ body: (UnsafePointer<UInt8>?) -> R) -> R {
    guard let string else { return body(nil) }
    return withUTF8CString(string) { body($0) }
}
