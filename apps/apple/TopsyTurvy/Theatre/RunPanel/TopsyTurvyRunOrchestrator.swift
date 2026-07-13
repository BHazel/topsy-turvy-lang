import Foundation
import Observation

/// Orchestrator for a programme execution.
@MainActor
@Observable
final class TopsyTurvyRunOrchestrator {
    /// The accumulated output lines from the most recent run.
    private(set) var outputLines: [String] = []

    /// A value indicating whether a run is in progress.
    private(set) var isRunning = false

    /// A human-readable summary of the most recently completed run outcome, or `nil` before any run.
    private(set) var statusMessage: String?

    /// Output lines awaiting the next flush.
    @ObservationIgnored
    nonisolated(unsafe) private var pendingOutputLines: [(text: String, suppressNewline: Bool)] = []
    
    /// A value indicating whether a flush of output lines is scheduled.
    @ObservationIgnored
    nonisolated(unsafe) private var isFlushScheduled = false
    
    /// The lock on pending output lines.
    ///
    /// Used to guard the pending output lines and flush scheduling flag as `scheduleOutput` is called
    /// from the session backgrund thread.
    private let pendingLock = NSLock()

    /// Buffers an output line for the next output lines flush.
    ///
    /// Coalescing many outputs into a single pending buffer,  flushed by at most one in-flight `Task`,
    /// keeps the number of main-actor hops bounded regardless of output volume.
    ///
    /// - Parameters:
    ///   - text: The output text.
    ///   - suppressNewline: A value indicating whether a newline should be omitted.
    nonisolated func scheduleOutput(_ text: String, suppressNewline: Bool) {
        self.pendingLock.lock()
        self.pendingOutputLines.append((text, suppressNewline))

        if self.pendingOutputLines.count > Self.maxRetainedLines {
            self.pendingOutputLines.removeFirst(self.pendingOutputLines.count - Self.maxRetainedLines)
        }

        let alreadyScheduled = self.isFlushScheduled
        self.isFlushScheduled = true
        self.pendingLock.unlock()

        guard !alreadyScheduled else {
            return
        }

        Task { @MainActor in
            self.flushPendingOutput()
        }
    }

    /// Flushes pending output lines.
    private func flushPendingOutput() {
        self.pendingLock.lock()
        let pendingBatch = self.pendingOutputLines
        self.pendingOutputLines = []
        self.pendingLock.unlock()

        for line in pendingBatch {
            self.appendOutput(line.text, suppressNewline: line.suppressNewline)
        }

        self.pendingLock.lock()
        let hasMorePending = !self.pendingOutputLines.isEmpty
        self.isFlushScheduled = hasMorePending
        self.pendingLock.unlock()

        if hasMorePending {
            Task { @MainActor in
                self.flushPendingOutput()
            }
        }
    }

    /// The maximum number of output lines retained.
    private nonisolated static let maxRetainedLines = 2_000

    /// Appends one output line, honouring `suppressNewline` by extending the last line rather than starting a
    /// new one, and trims to `maxRetainedLines` to bound rendering cost for runaway output.
    ///
    /// - Parameters:
    ///   - text: The output text.
    ///   - suppressNewline: A value indicating whether a newline should be omitted.
    private func appendOutput(_ text: String, suppressNewline: Bool) {
        if suppressNewline, !self.outputLines.isEmpty {
            self.outputLines[self.outputLines.count - 1] += text
        } else {
            self.outputLines.append(text)
        }

        if self.outputLines.count > Self.maxRetainedLines {
            self.outputLines.removeFirst(self.outputLines.count - Self.maxRetainedLines)
        }
    }

    /// Saves the source file if a save handler is provided, then executes the source through the Topsy Turvy toolchain
    /// streaming output and recording a status summary on completion.
    ///
    /// - Parameters:
    ///   - session: The Topsy Turvy toolchain session.
    ///   - source: The source code to execute.
    ///   - args: The command-line arguments.
    ///   - stdin: The preset standard input.
    ///   - save: The function to perform a file save.
    func run(session: TopsyTurvySession, source: String, args: [String], stdin: String, save: (() async -> Void)? = nil) async {
        await save?()

        self.outputLines = []
        self.statusMessage = nil
        self.isRunning = true

        await session.setOutputHandler { [weak self] text, suppressNewline in
            self?.scheduleOutput(text, suppressNewline: suppressNewline)
        }

        let status = await session.execute(source: source, arguments: args, stdin: stdin.isEmpty ? nil : stdin)
        self.statusMessage = await describeStatus(status, session: session)
        self.isRunning = false
    }

    /// Cancels a source execution, if running.
    ///
    /// - Parameters:
    ///   - session: The Topsy Turvy toolchain session.
    func stop(session: TopsyTurvySession) {
        session.cancel()
    }

    /// Gets a human-friendly description for the run status.
    ///
    /// - Parameters:
    ///   - status: The run status code.
    ///   - session: The Topsy Turvy toolchain session.
    ///
    /// - Returns: A human-friendly description for the run status.
    private func describeStatus(_ status: Int32, session: TopsyTurvySession) async -> String {
        switch status {
            case 0: return "Performance concluded successfully."
            case 1: return "The performance could not begin: parsing failed."
            case 2: return "The performance could not begin: type-checking failed."
            case 4: return "The performance was cancelled."
            default:
                let lastError = await session.lastError()
                return lastError.map {
                    "A runtime error occurred: \($0)"
                } ?? "An unexpected error occurred."
        }
    }
}
