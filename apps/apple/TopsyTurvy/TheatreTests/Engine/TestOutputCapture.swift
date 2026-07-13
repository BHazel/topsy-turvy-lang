/// Holds output lines captured by `TopsyTurvySessionTests`.
final class TestOutputCapture: @unchecked Sendable {
    /// The output lines.
    private(set) var lines: [String] = []

    /// Appends the specified text to the output.
    ///
    /// - Parameters:
    ///   - text: The text to append.
    ///   - suppressNewline: A value indicating whether a newline should be omitted.
    func append(_ text: String, suppressNewline: Bool) {
        if suppressNewline, !self.lines.isEmpty {
            self.lines[self.lines.count - 1] += text
        } else {
            self.lines.append(text)
        }
    }
}
