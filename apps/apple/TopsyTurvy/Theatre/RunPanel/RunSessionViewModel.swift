import Foundation
import Observation

/// Drives one Run/Stop cycle: output accumulation, status reporting, and the `@MainActor` hop from the
/// session's background-thread output callback.
@MainActor
@Observable
final class RunSessionViewModel {
    /// The accumulated output lines from the most recent run.
    private(set) var outputLines: [String] = []

    /// A value indicating whether a run is currently in flight.
    private(set) var isRunning = false

    /// A human-readable summary of the most recently completed run's outcome, or `nil` before any run.
    private(set) var statusMessage: String?

    /// Output lines awaiting the next main-actor flush, and whether a flush is already scheduled — guarded by
    /// `pendingLock` since `scheduleOutput` is called from the session's background callback thread.
    /// `@ObservationIgnored` is required for `nonisolated(unsafe)` to have any effect here: `@Observable`
    /// macro-expands stored properties into tracked ones, and `nonisolated` cannot apply to those.
    @ObservationIgnored
    nonisolated(unsafe) private var pendingLines: [(text: String, suppressNewline: Bool)] = []
    @ObservationIgnored
    nonisolated(unsafe) private var flushScheduled = false
    private let pendingLock = NSLock()

    /// Buffers an output line for the next main-actor flush.
    ///
    /// A tight `BEHOLD`-in-a-loop programme can invoke the output callback thousands of times a second;
    /// spawning a new unstructured `Task { @MainActor in ... }` per line saturates the main actor's queue and
    /// makes the UI (including the Stop button) briefly unresponsive. Coalescing into a single pending buffer,
    /// flushed by at most one in-flight `Task`, keeps the number of main-actor hops bounded regardless of
    /// output volume.
    nonisolated func scheduleOutput(_ text: String, suppressNewline: Bool) {
        pendingLock.lock()
        pendingLines.append((text, suppressNewline))
        let alreadyScheduled = flushScheduled
        flushScheduled = true
        pendingLock.unlock()

        guard !alreadyScheduled else { return }

        Task { @MainActor in
            self.flushPendingOutput()
        }
    }

    private func flushPendingOutput() {
        pendingLock.lock()
        let batch = pendingLines
        pendingLines = []
        flushScheduled = false
        pendingLock.unlock()

        for line in batch {
            appendOutput(line.text, suppressNewline: line.suppressNewline)
        }
    }

    /// The maximum number of output lines retained: a runaway loop with no artificial delay can otherwise grow
    /// `outputLines` fast enough that `OutputPaneView`'s `ForEach` diffing cost alone measurably delays the UI
    /// (including hit-testing the Stop button), long past when the interpreter itself has actually cancelled.
    private static let maxRetainedLines = 2_000

    /// Appends one output line, honouring `suppressNewline` by extending the last line rather than starting a
    /// new one, and trims to `maxRetainedLines` to bound rendering cost for runaway output.
    private func appendOutput(_ text: String, suppressNewline: Bool) {
        if suppressNewline, !outputLines.isEmpty {
            outputLines[outputLines.count - 1] += text
        } else {
            outputLines.append(text)
        }

        if outputLines.count > Self.maxRetainedLines {
            outputLines.removeFirst(outputLines.count - Self.maxRetainedLines)
        }
    }

    /// Saves the document (if a save handler is provided), then runs `source` on `session`, streaming output
    /// and recording a status summary on completion.
    func run(session: TopsyTurvySession, source: String, args: [String], stdin: String, save: (() async -> Void)? = nil) async {
        await save?()

        outputLines = []
        statusMessage = nil
        isRunning = true

        await session.setOutputHandler { [weak self] text, suppressNewline in
            self?.scheduleOutput(text, suppressNewline: suppressNewline)
        }

        let status = await session.execute(source: source, args: args, stdin: stdin.isEmpty ? nil : stdin)
        statusMessage = await describeStatus(status, session: session)
        isRunning = false
    }

    /// Cooperatively cancels the in-flight run, if any.
    func stop(session: TopsyTurvySession) {
        session.cancel()
    }

    private func describeStatus(_ status: Int32, session: TopsyTurvySession) async -> String {
        switch status {
        case 0: return "Performance concluded successfully."
        case 1: return "The performance could not begin: parsing failed."
        case 2: return "The performance could not begin: type-checking failed."
        case 4: return "The performance was cancelled."
        default:
            let lastError = await session.lastError()
            return lastError.map { "A runtime error occurred: \($0)" } ?? "An unexpected error occurred."
        }
    }
}
