import LanguageSupport

/// A minimal `LocationService` computing line/column conversions directly from a stored source string, for use
/// by `TopsyTurvyLanguageServiceTests`.
///
/// `CodeEditorView` normally supplies its own `LocationService` to `openDocument(with:locationService:)`;
/// opening a document directly in a test bypasses that, so a stand-in is needed. Offsets are counted in UTF-16
/// code units, matching the convention `TextLocation`/the native export boundary already use.
struct TestLocationService: LocationService {
    let source: String

    func textLocation(from location: Int) -> Result<TextLocation, Error> {
        let lines = source.components(separatedBy: "\n")
        var remaining = location
        for (index, line) in lines.enumerated() {
            let lineLength = line.utf16.count + 1
            if remaining < lineLength {
                return .success(TextLocation(zeroBasedLine: index, column: remaining))
            }
            remaining -= lineLength
        }
        return .success(TextLocation(zeroBasedLine: max(0, lines.count - 1), column: 0))
    }

    func location(from textLocation: TextLocation) -> Result<Int, Error> {
        let lines = source.components(separatedBy: "\n")
        var offset = 0
        for index in 0..<textLocation.zeroBasedLine where index < lines.count {
            offset += lines[index].utf16.count + 1
        }
        return .success(offset + textLocation.zeroBasedColumn)
    }

    func length(of zeroBasedLine: Int) -> Int? {
        let lines = source.components(separatedBy: "\n")
        guard zeroBasedLine < lines.count else { return nil }
        return lines[zeroBasedLine].utf16.count + (zeroBasedLine < lines.count - 1 ? 1 : 0)
    }
}
