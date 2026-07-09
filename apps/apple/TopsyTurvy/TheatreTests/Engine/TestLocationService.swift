import LanguageSupport

/// A minimal `LocationService` that computes line/column conversions directly from a stored source string,
/// standing in for the one `CodeEditorView` normally supplies.  Offsets are counted in UTF-16 code units,
/// matching the convention used by `TextLocation` and the native export boundary.
struct TestLocationService: LocationService {
    /// The source text.
    let source: String

    /// Converts an absolute UTF-16 offset into a 0-indexed line/column.
    /// 
    /// - Parameters:
    ///   - location: The absolute UTF-16 offset.
    /// 
    /// - Returns: The corresponding 0-indexed line and column.
    func textLocation(from location: Int) -> Result<TextLocation, Error> {
        let lines = self.source.components(separatedBy: "\n")
        var remainingLines = location
        for (index, line) in lines.enumerated() {
            let lineLength = line.utf16.count + 1
            if remainingLines < lineLength {
                return .success(TextLocation(zeroBasedLine: index, column: remainingLines))
            }
            
            remainingLines -= lineLength
        }
        
        return .success(TextLocation(zeroBasedLine: max(0, lines.count - 1), column: 0))
    }

    /// Converts a 0-indexed line/column into an absolute UTF-16 offset.
    /// 
    /// - Parameters:
    ///   - textLocation: The 0-indexed line and column.
    /// 
    /// - Returns: The corresponding absolute UTF-16 offset.
    func location(from textLocation: TextLocation) -> Result<Int, Error> {
        let lines = source.components(separatedBy: "\n")
        var offset = 0
        for index in 0..<textLocation.zeroBasedLine where index < lines.count {
            offset += lines[index].utf16.count + 1
        }

        return .success(offset + textLocation.zeroBasedColumn)
    }

    /// Gets the UTF-16 length of a line, including its trailing newline if not the last line.
    /// 
    /// - Parameters:
    ///   - zeroBasedLine: The 0-indexed line.
    /// 
    /// - Returns: The length of the line, or `nil` if out of range.
    func length(of zeroBasedLine: Int) -> Int? {
        let lines = source.components(separatedBy: "\n")
        guard zeroBasedLine < lines.count else {
            return nil
        }
        
        return lines[zeroBasedLine].utf16.count + (zeroBasedLine < lines.count - 1 ? 1 : 0)
    }
}
