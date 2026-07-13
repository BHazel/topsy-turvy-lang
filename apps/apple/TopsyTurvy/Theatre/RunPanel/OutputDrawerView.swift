import SwiftUI

/// Wraps the execution output view depending on the size and orientation class.
///
/// Adapts to size and orientation size class:
/// - A snap-state drawer docked at the bottom of the editor column on regular-width screens (iPad), or,
/// - A status bar plus a detented sheet on compact-width screens (iPhone).
///
/// `isPresented` is used for both the drawer expansion and sheet presentation state.
struct OutputDrawerView: View {
    /// The horizontal size class, from the environment.
    @Environment(\.horizontalSizeClass) private var horizontalSizeClass
    
    /// The lines of output text.
    let lines: [String]
    
    /// The status message.
    let statusMessage: String?
    
    /// A value indicating whether the output view is presented.
    @Binding var isPresented: Bool

    /// The view body.
    var body: some View {
        if self.horizontalSizeClass == .compact {
            self.statusPill
                .sheet(isPresented: self.$isPresented) {
                    NavigationStack {
                        OutputPaneView(lines: self.lines)
                            .navigationTitle("Output")
                            .navigationBarTitleDisplayMode(.inline)
                            .toolbar {
                                ToolbarItem(placement: .topBarTrailing) {
                                    Button("Close") {
                                        self.isPresented = false
                                    }
                                }
                            }
                    }
                    .presentationDetents([.medium, .large])
                }
        } else {
            self.drawer
        }
    }

    /// The compact-width status pill.
    ///
    /// A persistent summary of the last run, tapped to present the full output sheet.
    private var statusPill: some View {
        Button {
            self.isPresented = true
        } label: {
            HStack(spacing: 6) {
                Image(systemName: "terminal")
                Text(self.statusMessage ?? "No output yet")
                    .lineLimit(1)
            }
            .font(.footnote)
            .foregroundStyle(.secondary)
            .padding(.horizontal, 12)
            .padding(.vertical, 6)
            .background(.thinMaterial, in: Capsule())
        }
        .buttonStyle(.plain)
        .accessibilityIdentifier("OutputDrawerView.statusPill")
    }

    /// The regular-width drawer.
    ///
    /// A grab-bar header, with status, chevron and tapped to expand/collapse, with the
    /// output pane revealed beneath it when expanded.
    private var drawer: some View {
        VStack(spacing: 0) {
            self.header
            if self.isPresented {
                Divider()
                OutputPaneView(lines: self.lines)
                    .frame(height: 260)
                    .transition(.move(edge: .bottom).combined(with: .opacity))
            }
        }
        .background(.bar)
        .animation(.easeInOut(duration: 0.2), value: isPresented)
    }

    /// The drawer header.
    private var header: some View {
        Button {
            self.isPresented.toggle()
        } label: {
            HStack {
                Image(systemName: "terminal")
                Text(self.statusMessage ?? "Output")
                    .lineLimit(1)
                Spacer()
                Image(systemName: "chevron.up")
                    .rotationEffect(.degrees(self.isPresented ? 180 : 0))
            }
            .font(.footnote)
            .foregroundStyle(.secondary)
            .padding(.horizontal, 12)
            .frame(height: 32)
        }
        .buttonStyle(.plain)
        .accessibilityIdentifier("OutputDrawerView.header")
    }
}
