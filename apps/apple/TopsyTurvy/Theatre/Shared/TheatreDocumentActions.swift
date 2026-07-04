import SwiftUI

/// The subset of a document window's actions exposed to menu bar commands via `FocusedValue`.
struct TheatreDocumentActions {
    /// A value indicating whether a run is currently in flight for the focused document.
    let isRunning: Bool

    /// Runs the focused document's programme.
    let perform: () -> Void

    /// Cancels the focused document's in-flight run, if any.
    let stop: () -> Void

    /// Replaces the focused document's buffer with its canonically formatted source.
    let format: () -> Void
}

/// The `FocusedValueKey` publishing the focused document window's `TheatreDocumentActions`.
private struct TheatreDocumentActionsKey: FocusedValueKey {
    typealias Value = TheatreDocumentActions
}

extension FocusedValues {
    /// The focused document window's `TheatreDocumentActions`, or `nil` when no document window has focus.
    var theatreDocumentActions: TheatreDocumentActions? {
        get { self[TheatreDocumentActionsKey.self] }
        set { self[TheatreDocumentActionsKey.self] = newValue }
    }
}
