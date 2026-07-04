import SwiftUI

/// Perform/Stop buttons, promoted to the toolbar so they stay reachable even while the run panel drawer is
/// collapsed.
struct RunToolbarButtons: View {
    let isRunning: Bool
    let onRun: () -> Void
    let onStop: () -> Void

    var body: some View {
        Button(action: onRun) {
            Label("Perform", systemImage: "play.fill")
        }
        .disabled(isRunning)
        .accessibilityIdentifier("RunControlsView.performButton")

        Button(action: onStop) {
            Label("Stop", systemImage: "stop.fill")
        }
        .disabled(!isRunning)
        .accessibilityIdentifier("RunControlsView.stopButton")
    }
}
