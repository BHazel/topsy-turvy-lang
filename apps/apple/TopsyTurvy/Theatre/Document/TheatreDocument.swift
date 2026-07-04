import SwiftUI
import UniformTypeIdentifiers

/// A `.topsy` source file open in Theatre.
///
/// Named after the app, not the language, since Theatre may host additional document formats (e.g. UtopIR
/// files) in future.
struct TheatreDocument: FileDocument {
    static var readableContentTypes: [UTType] { [.topsySource] }
    static var writableContentTypes: [UTType] { [.topsySource] }

    /// The document's Topsy Turvy source text.
    var text: String

    /// Creates a new, empty document.
    init(text: String = "") {
        self.text = text
    }

    init(configuration: ReadConfiguration) throws {
        guard let data = configuration.file.regularFileContents,
              let text = String(data: data, encoding: .utf8)
        else {
            throw CocoaError(.fileReadCorruptFile)
        }
        self.text = text
    }

    func fileWrapper(configuration: WriteConfiguration) throws -> FileWrapper {
        FileWrapper(regularFileWithContents: Data(text.utf8))
    }
}
