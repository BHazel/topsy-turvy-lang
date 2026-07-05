import Foundation
import Observation

/// One open workspace window's folder: owns the security-scoped access lifecycle for the root folder and
/// the resulting file tree for the sidebar.
///
/// Access must be started with `startAccess()` when the workspace window appears and balanced with
/// `stopAccess()` when it disappears — mirroring `TopsyTurvySession`'s explicit `open()`/`close()` lifecycle,
/// since `deinit` cannot reliably call an isolated method.
@MainActor
@Observable
final class WorkspaceModel {
    /// The workspace's root folder, resolved from its security-scoped bookmark.
    let rootURL: URL

    /// The root folder's contents, as a sorted tree — directories first, then alphabetically.
    private(set) var fileTree: [WorkspaceFileNode] = []

    private var isAccessingSecurityScope = false

    /// Creates a workspace model for an already-resolved root folder URL.
    /// - Parameter rootURL: The workspace's root folder, resolved from its security-scoped bookmark.
    init(rootURL: URL) {
        self.rootURL = rootURL
    }

    /// Begins security-scoped access to the root folder and builds the initial file tree.
    func startAccess() {
        isAccessingSecurityScope = rootURL.startAccessingSecurityScopedResource()
        refresh()
    }

    /// Ends security-scoped access to the root folder, if it was started.
    func stopAccess() {
        guard isAccessingSecurityScope else { return }
        rootURL.stopAccessingSecurityScopedResource()
        isAccessingSecurityScope = false
    }

    /// Rebuilds the file tree from disk, for example after an external change or a manual refresh.
    func refresh() {
        fileTree = WorkspaceFileTree.build(at: rootURL)
    }
}
