import SwiftUI
import TopsyTurvyToolchain

/// The main content view.
struct ContentView: View {
    private let apiVersion = topsyturvy_api_version()

    /// The main view.
    var body: some View {
        Text("Topsy Turvy engine contract version: \(apiVersion)")
            .padding()
    }
}
