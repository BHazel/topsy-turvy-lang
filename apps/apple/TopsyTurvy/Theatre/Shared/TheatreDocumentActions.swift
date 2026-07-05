/// The active document window's actions, exposed to menu bar commands via `TheatreActiveScene` (not
/// `FocusedValue` — see that type's doc comment for why).
struct TheatreDocumentActions {
    /// A value indicating whether a run is currently in flight for the active document.
    let isRunning: Bool

    /// Runs the active document's programme.
    let perform: () -> Void

    /// Cancels the active document's in-flight run, if any.
    let stop: () -> Void

    /// Replaces the active document's buffer with its canonically formatted source.
    let format: () -> Void
}
