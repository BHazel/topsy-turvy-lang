import Foundation

/// Builds a folder file tree.
enum FileTree {
    /// Recursively builds a directory tree, including subfolders.
    ///
    /// Directories are listed before files at each level; within each, entries are ordered alphabetically by
    /// localised name. Hidden files are skipped.
    ///
    /// - Parameters:
    ///   - rootURL: The root URL of the directory.
    ///
    /// - Returns: A collection of file nodes.
    static func build(at rootURL: URL) -> [FileNode] {
        let entries = (try? FileManager.default.contentsOfDirectory(
            at: rootURL,
            includingPropertiesForKeys: [.isDirectoryKey],
            options: [.skipsHiddenFiles]
        )) ?? []

        return entries
            .map { url -> FileNode in
                let isDirectory = (try? url.resourceValues(forKeys: [.isDirectoryKey]))?.isDirectory ?? false
                let children = isDirectory
                    ? build(at: url)
                    : []

                return FileNode(url: url, isDirectory: isDirectory, children: children)
            }
            .sorted { lhs, rhs in
                if lhs.isDirectory != rhs.isDirectory {
                    return lhs.isDirectory && !rhs.isDirectory
                }

                return lhs.name.localizedStandardCompare(rhs.name) == .orderedAscending
            }
    }
}
