import Foundation
import Observation

/// Represents an open folder, owning the security-scoped access lifecycle for the root folder and resulting file
/// tree for the sidebar.
///
/// Access must be started with `startAccess()` when the folder browser appears and balanced with
/// `stopAccess()` when it disappears.  This mirrors the explicit `open()`/`close()` lifecycle of
/// `TopsyTurvySession`, since `deinit` cannot reliably call isolated methods.
@MainActor
@Observable
final class TheatreFolder {
    /// The root URL of the folder.
    ///
    /// Resolved from its security-scoped bookmark.
    let rootURL: URL

    /// The contents of the root folder.
    ///
    /// Sorted as a tree, directories first then alphabetically.
    private(set) var fileTree: [FileNode] = []

    /// A value indicating whether security-scoped access to the root URL is currently active.
    private var isAccessingSecurityScope = false

    /// Creates a folder for an already-resolved root URL.
    ///
    /// - Parameter rootURL: The folder root URL.
    init(rootURL: URL) {
        self.rootURL = rootURL
    }

    /// Begins security-scoped access to the root folder and builds the initial file tree.
    func startAccess() {
        self.isAccessingSecurityScope = self.rootURL.startAccessingSecurityScopedResource()
        self.refresh()
    }

    /// Ends security-scoped access to the root folder, if it was started.
    func stopAccess() {
        guard self.isAccessingSecurityScope else {
            return
        }
        
        self.rootURL.stopAccessingSecurityScopedResource()
        self.isAccessingSecurityScope = false
    }

    /// Rebuilds the file tree.
    func refresh() {
        self.fileTree = FileTree.build(at: self.rootURL)
    }
}
