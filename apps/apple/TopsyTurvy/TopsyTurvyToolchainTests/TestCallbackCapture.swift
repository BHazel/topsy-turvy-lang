import TopsyTurvyToolchain

/// Holds output captured by `captureOutputLine`, for tests to assert against.
///
/// A `@convention(c)` function cannot capture instance state directly — Swift only permits forming a C
/// function pointer from a free function or a literal closure, not a static method — so the two callback
/// targets below are top-level functions rather than members of this type, and they read/write this
/// type's static state instead of capturing it.
final class TestCallbackCapture {
    /// Holds the lines of output captured by `captureOutputLine`.
    static var outputLines: [String] = []
}

/// The `output_line` callback target registered with `topsyturvy_session_create`.
/// - Parameter context: The context pointer passed to `topsyturvy_session_create`.
/// - Parameter utf8: The UTF-8 string to capture.
/// - Parameter suppressNewline: A value indicating whether the string should be appended to the last line of output (if non-zero) or added as a new line (if zero).
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

/// The `resolve_import` callback target registered with `topsyturvy_session_create`; these smoke tests never import.
/// - Parameter context: The context pointer passed to `topsyturvy_session_create`.
/// - Parameter filename: The UTF-8 string containing the filename to resolve.
/// - Returns: Always `nil`, since these smoke tests never import.
func resolveImportStub(context: UnsafeMutableRawPointer?, filename: UnsafePointer<UInt8>?) -> UnsafeMutablePointer<UInt8>? {
    nil
}
