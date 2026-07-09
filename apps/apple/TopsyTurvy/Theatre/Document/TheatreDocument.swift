import SwiftUI
import UniformTypeIdentifiers

/// Represents a source file open in the app.
struct TheatreDocument: FileDocument {
    /// File types readable by the app.
    static var readableContentTypes: [UTType] {
        [.topsySource]
    }
    
    /// File types writable by the app.
    static var writableContentTypes: [UTType] {
        [.topsySource]
    }

    /// The text of the source file.
    var text: String

    /// Creates a new source file.
    ///
    /// - Parameters:
    ///   - text: The text of the source file, defaulting to an empty string (blank document).
    init(text: String = "") {
        self.text = text
    }

    /// Loads a source file.
    ///
    /// This method is called by `DocumentGroup` when opening a file; it is not called directly otherwise.
    ///
    /// - Parameters:
    ///   - configuration: The configuration for reading a source file.
    ///
    /// - Throws: `CocoaError(.fileReadCorruptFile)` if the source file is not a regular file or is not in UTF-8.
    init(configuration: ReadConfiguration) throws {
        guard let data = configuration.file.regularFileContents,
              let text = String(data: data, encoding: .utf8)
        else {
            throw CocoaError(.fileReadCorruptFile)
        }
        
        self.text = text
    }

    /// Handles writing of source files.
    ///
    /// This method is called by `DocumentGroup` when saving a file; it is not called directly otherwise.
    ///
    /// - Parameters:
    ///   - configuration: The configuration for writing a source file.
    ///
    /// - Returns: The `FileWrapper` for the written file.
    func fileWrapper(configuration: WriteConfiguration) throws -> FileWrapper {
        FileWrapper(regularFileWithContents: Data(text.utf8))
    }
}
