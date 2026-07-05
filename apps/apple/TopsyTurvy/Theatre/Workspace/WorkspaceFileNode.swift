import Foundation

/// One entry in a workspace's file tree: either a folder (with children) or a file.
struct WorkspaceFileNode: Identifiable, Hashable {
    /// The node's location on disk, also its stable identity within the tree.
    let url: URL

    /// A value indicating whether this node is a folder.
    let isDirectory: Bool

    /// This node's children, populated for directories and empty for files.
    var children: [WorkspaceFileNode]

    var id: URL { url }

    /// The node's display name, its last path component.
    var name: String { url.lastPathComponent }

    /// A value indicating whether this is a `.topsy` source file — non-`.topsy` files are shown in the tree
    /// but are not directly openable in the editor.
    var isTopsyFile: Bool {
        !isDirectory && url.pathExtension.caseInsensitiveCompare("topsy") == .orderedSame
    }

    /// `children` for a directory, `nil` for a file — the shape `OutlineGroup` needs so only folders get a
    /// disclosure arrow.
    var childrenForOutline: [WorkspaceFileNode]? {
        isDirectory ? children : nil
    }
}
