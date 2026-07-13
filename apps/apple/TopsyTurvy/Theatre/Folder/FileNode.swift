import Foundation

/// One file entry in a folder tree.
///
/// This can be either a folder, with child items, or a file.
struct FileNode: Identifiable, Hashable {
    /// The URL of the file.
    let url: URL

    /// A value indicating whether this node is a folder.
    let isDirectory: Bool

    /// The children of the node.
    ///
    /// Populated for directories and empty for files.
    var children: [FileNode]

    /// The stable identity of the node.
    var id: URL {
        url
    }

    /// The display name of the node as its last path component.
    var name: String { url.lastPathComponent }

    /// A value indicating whether this is a Topsy Turvy `.topsy` source file.
    ///
    /// Non-`.topsy` files are shown in the tree but cannot be opened in the editor.
    var isTopsyFile: Bool {
        !self.isDirectory && self.url.pathExtension.caseInsensitiveCompare("topsy") == .orderedSame
    }

    /// The `children` for a directory, `nil` for a file.
    ///
    /// This is required by `OutlineGroup` so only folders receive a disclosure arrow.
    var childrenForOutline: [FileNode]? {
        self.isDirectory
            ? self.children
            : nil
    }
}
