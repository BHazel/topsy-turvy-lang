import SwiftUI

/// Displays a file tree as a navigable sidebar over `TheatreFolder.fileTree`.
struct FileTreeSidebarView: View {
    /// The tree entry nodes.
    let nodes: [FileNode]
    
    /// The selected file URL.
    @Binding var selectedFileURL: URL?
    
    /// The handler for when a file is selected.
    let onSelectFile: (FileNode) -> Void

    /// The view body.
    var body: some View {
        List(selection: self.$selectedFileURL) {
            OutlineGroup(self.nodes, id: \.id, children: \.childrenForOutline) { node in
                self.treeRow(for: node)
                    .tag(node.isTopsyFile ? node.url : nil)
            }
        }
        .listStyle(.sidebar)
        .onChange(of: self.selectedFileURL) { _, newValue in
            guard let newValue, let node = Self.node(at: newValue, in: self.nodes) else {
                return
            }
            
            self.onSelectFile(node)
        }
        .overlay {
            if self.nodes.isEmpty {
                ContentUnavailableView("No Files", systemImage: "folder")
            }
        }
    }

    /// View builder for a row in the file tree.
    ///
    /// - Parameters:
    ///   - node: The file node.
    ///
    /// - Returns: The row view for the file tree view.
    @ViewBuilder
    private func treeRow(for node: FileNode) -> some View {
        if node.isDirectory {
            Label(node.name, systemImage: "folder")
        } else if node.isTopsyFile {
            Label(node.name, systemImage: "doc.text")
        } else {
            Label(node.name, systemImage: "doc")
                .foregroundStyle(.secondary)
                .accessibilityHint("Not a Topsy Turvy source file")
        }
    }

    /// Finds the node with the given URL within `nodes`, searching recursively.
    ///
    /// - Parameters:
    ///   - url: The file URL.
    ///   - nodes: The file nodes.
    ///
    /// - Returns: The file node for the URL, otherwise `nil` if not found.
    static func node(at url: URL, in nodes: [FileNode]) -> FileNode? {
        for node in nodes {
            if node.url == url {
                return node
            }
            
            if let foundNode = self.node(at: url, in: node.children) {
                return foundNode
            }
        }
        
        return nil
    }
}
