import Foundation

/// Builds a workspace's file tree from disk: a pure function over `FileManager`, kept separate from
/// `WorkspaceModel` for direct unit testing against real temporary directories.
enum WorkspaceFileTree {
    /// Recursively enumerates `rootURL`, returning its contents as a sorted tree — directories first, then
    /// alphabetically by localized name. Hidden files (dotfiles) are skipped.
    /// - Parameter rootURL: The workspace's root folder.
    /// - Returns: The folder's contents, as a sorted tree.
    static func build(at rootURL: URL) -> [WorkspaceFileNode] {
        contents(of: rootURL)
    }

    private static func contents(of directoryURL: URL) -> [WorkspaceFileNode] {
        let entries = (try? FileManager.default.contentsOfDirectory(
            at: directoryURL,
            includingPropertiesForKeys: [.isDirectoryKey],
            options: [.skipsHiddenFiles]
        )) ?? []

        return entries
            .map { url -> WorkspaceFileNode in
                let isDirectory = (try? url.resourceValues(forKeys: [.isDirectoryKey]))?.isDirectory ?? false
                let children = isDirectory ? contents(of: url) : []
                return WorkspaceFileNode(url: url, isDirectory: isDirectory, children: children)
            }
            .sorted { lhs, rhs in
                if lhs.isDirectory != rhs.isDirectory {
                    return lhs.isDirectory && !rhs.isDirectory
                }
                return lhs.name.localizedStandardCompare(rhs.name) == .orderedAscending
            }
    }
}
