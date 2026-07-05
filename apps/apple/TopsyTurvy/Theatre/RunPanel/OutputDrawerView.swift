import SwiftUI

/// Displays run output, adapting to size class per `THEATRE_DESIGN.md` §4: a snap-state drawer docked at
/// the bottom of the editor column on regular-width screens (iPad), or a status pill plus a detented sheet
/// on compact-width screens (iPhone). `isPresented` doubles as "drawer expanded" and "sheet presented", so
/// callers share one binding regardless of size class.
///
/// Two snap states only (collapsed / expanded) — the free-form drag-to-resize divider this replaces is a
/// deliberately dropped affordance (`TOURING_THEATRE_PLAN.md` §13, kill list).
struct OutputDrawerView: View {
    let lines: [String]
    let statusMessage: String?
    @Binding var isPresented: Bool

    @Environment(\.horizontalSizeClass) private var horizontalSizeClass

    var body: some View {
        if horizontalSizeClass == .compact {
            statusPill
                .sheet(isPresented: $isPresented) {
                    NavigationStack {
                        OutputPaneView(lines: lines)
                            .navigationTitle("Output")
                            .navigationBarTitleDisplayMode(.inline)
                    }
                    .presentationDetents([.medium, .large])
                }
        } else {
            drawer
        }
    }

    /// The compact-width status pill: a persistent summary of the last run, tapped to present the output
    /// sheet — visible without needing the sheet open, unlike the docked pane it replaces.
    private var statusPill: some View {
        Button {
            isPresented = true
        } label: {
            HStack(spacing: 6) {
                Image(systemName: "terminal")
                Text(statusMessage ?? "No output yet")
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

    /// The regular-width drawer: a grab-bar header (status + chevron, tap to expand/collapse) with the
    /// output pane revealed beneath it when expanded.
    private var drawer: some View {
        VStack(spacing: 0) {
            header
            if isPresented {
                Divider()
                OutputPaneView(lines: lines)
                    .frame(height: 260)
                    .transition(.move(edge: .bottom).combined(with: .opacity))
            }
        }
        .background(.bar)
        .animation(.easeInOut(duration: 0.2), value: isPresented)
    }

    private var header: some View {
        Button {
            isPresented.toggle()
        } label: {
            HStack {
                Image(systemName: "terminal")
                Text(statusMessage ?? "Output")
                    .lineLimit(1)
                Spacer()
                Image(systemName: "chevron.up")
                    .rotationEffect(.degrees(isPresented ? 180 : 0))
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
