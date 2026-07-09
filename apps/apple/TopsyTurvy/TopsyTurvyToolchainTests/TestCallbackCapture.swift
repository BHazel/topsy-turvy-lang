import TopsyTurvyToolchain

/// Holds outputs from callbacks, used for testing purposes.
///
/// A `@convention(c)` function cannot capture instance state, so the callback targets below are top-level
/// functions that read and write the static state of this type instead.
final class TestCallbackCapture {
    /// Holds the lines of output captured by `captureOutputLine`.
    static var outputLines: [String] = []
}

/// The `output_line` callback target registered with `topsyturvy_session_create`.
///
/// - Parameters:
///   - context: The context pointer passed to `topsyturvy_session_create`.
///   - utf8: The UTF-8 string to capture.
///   - suppressNewline: A value indicating whether the string should be appended to the last line of output (if non-zero) or added as a new line (if zero).
func captureOutputLine(context: UnsafeMutableRawPointer?, utf8: UnsafePointer<UInt8>?, suppressNewline: UInt8) {
    guard let utf8 else {
        return
    }

    let message = String(cString: utf8)
    if suppressNewline != 0, !TestCallbackCapture.outputLines.isEmpty {
        TestCallbackCapture.outputLines[TestCallbackCapture.outputLines.count - 1] += message
    } else {
        TestCallbackCapture.outputLines.append(message)
    }
}

/// The `resolve_import` callback target registered with `topsyturvy_session_create`.
///
/// This always returns `nil`, since these tests do not import.
///
/// - Parameters:
///   - context: The context pointer passed to `topsyturvy_session_create`.
///   - filename: The UTF-8 string containing the filename to resolve.
///
/// - Returns: A pointer to the imported source text.
func resolveImportStub(context: UnsafeMutableRawPointer?, filename: UnsafePointer<UInt8>?) -> UnsafeMutablePointer<UInt8>? {
    nil
}
