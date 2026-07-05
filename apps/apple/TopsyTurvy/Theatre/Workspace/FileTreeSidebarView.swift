import SwiftUI

/// Displays a workspace's file tree as a navigable sidebar over `WorkspaceModel.fileTree`. Folders expand
/// with disclosure arrows (`OutlineGroup`); `.topsy` files are selectable to open, non-`.topsy` files are
/// shown for context — a programme might reference a data file — but dimmed and not selectable.
struct FileTreeSidebarView: View {
    let nodes: [WorkspaceFileNode]
    @Binding var selectedFileURL: URL?
    let onSelectFile: (WorkspaceFileNode) -> Void

    var body: some View {
        List(selection: $selectedFileURL) {
            OutlineGroup(nodes, id: \.id, children: \.childrenForOutline) { node in
                row(for: node)
                    .tag(node.isTopsyFile ? node.url : nil)
            }
        }
        .listStyle(.sidebar)
        .onChange(of: selectedFileURL) { _, newValue in
            guard let newValue, let node = Self.node(at: newValue, in: nodes) else { return }
            onSelectFile(node)
        }
        .overlay {
            if nodes.isEmpty {
                ContentUnavailableView("No Files", systemImage: "folder")
            }
        }
    }

    @ViewBuilder
    private func row(for node: WorkspaceFileNode) -> some View {
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

    /// Finds the node with the given URL within `nodes`, searching recursively — resolves `selectedFileURL`
    /// (a `List` selection value) back into the full node `onSelectFile` needs. Kept `static` and separate
    /// from the view body for direct unit testing.
    static func node(at url: URL, in nodes: [WorkspaceFileNode]) -> WorkspaceFileNode? {
        for node in nodes {
            if node.url == url { return node }
            if let found = self.node(at: url, in: node.children) { return found }
        }
        return nil
    }
}
