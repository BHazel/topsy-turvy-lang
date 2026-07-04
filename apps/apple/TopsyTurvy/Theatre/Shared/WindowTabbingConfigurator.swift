import SwiftUI

#if os(macOS)
import AppKit

/// Sets the hosting `NSWindow`'s tabbing mode to `.preferred`, so multiple open `.topsy` documents default to
/// native macOS window tabs (as in Xcode/Safari) regardless of the user's system-wide "prefer tabs" setting —
/// appropriate for an editor-style app where several open files is the common case, not the exception.
private struct WindowTabbingConfigurator: NSViewRepresentable {
    func makeNSView(context: Context) -> NSView {
        let view = NSView(frame: .zero)
        DispatchQueue.main.async {
            view.window?.tabbingMode = .preferred
        }
        return view
    }

    func updateNSView(_ nsView: NSView, context: Context) {}
}

extension View {
    /// Prefers native window tabbing for the window hosting this view; a no-op on non-macOS platforms.
    func preferWindowTabbing() -> some View {
        background(WindowTabbingConfigurator())
    }
}
#else
extension View {
    /// A no-op on non-macOS platforms — window tabbing is a macOS-only concept.
    func preferWindowTabbing() -> some View { self }
}
#endif
