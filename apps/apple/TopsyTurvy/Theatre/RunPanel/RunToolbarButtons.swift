import SwiftUI

/// Toolbar buttons for the Run and Stop actions.
struct RunToolbarButtons: View {
    /// A value indicating whether a run is in progress.
    let isRunning: Bool
    
    /// The handler for when a run is executed.
    let onRun: () -> Void
    
    /// The handler for when a run is stopped.
    let onStop: () -> Void

    /// The view body.
    var body: some View {
        Button(action: self.onRun) {
            Label("Perform", systemImage: "play.fill")
        }
        .disabled(self.isRunning)
        .accessibilityIdentifier("RunToolbarButtons.performButton")

        Button(action: self.onStop) {
            Label("Stop", systemImage: "stop.fill")
        }
        .disabled(!self.isRunning)
        .accessibilityIdentifier("RunToolbarButtons.stopButton")
    }
}
