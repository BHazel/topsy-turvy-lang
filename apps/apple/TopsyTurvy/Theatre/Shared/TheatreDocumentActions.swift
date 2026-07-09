/// Actions available for an active document.
struct TheatreDocumentActions {
    /// A value indicating whether code is being executed for the active document.
    let isRunning: Bool

    /// Runs the active document programme.
    let perform: () -> Void

    /// Cancels the active document code execution if running.
    let stop: () -> Void

    /// Replaces the active document source code with its canonically formatted source.
    let format: () -> Void
}
