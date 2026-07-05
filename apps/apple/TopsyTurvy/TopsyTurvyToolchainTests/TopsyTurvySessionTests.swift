import XCTest
@testable import Theatre

/// Tests for the `TopsyTurvySession` actor: JSON decoding round-trips, debounce behaviour, output streaming,
/// and the `resolve_import` buffer-ownership pattern.
final class TopsyTurvySessionTests: XCTestCase {
    /// Tests that `analyse` reports success for a valid programme.
    func testAnalyseReportsSuccessForValidProgramme() async {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock { await session.close() }

        let result = await session.analyse(source: "HARK! \"Test\"\n\nFINALE.\n")

        XCTAssertTrue(result.Success)
        XCTAssertTrue(result.Diagnostics.isEmpty)
    }

    /// Tests that `analyse` reports a diagnostic for an invalid programme.
    func testAnalyseReportsDiagnosticForInvalidProgramme() async {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock { await session.close() }

        let result = await session.analyse(source: "THIS IS NOT VALID TOPSY TURVY")

        XCTAssertFalse(result.Success)
        XCTAssertFalse(result.Diagnostics.isEmpty)
    }

    /// Tests that a `scheduleAnalyse` call superseded by a later call before its debounce completes returns
    /// `nil`, while the superseding call still returns a result.
    func testScheduleAnalyseDiscardsSupersededCalls() async {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock { await session.close() }

        // `async let` gives no ordering guarantee for which child task enters the actor first, so the first
        // call is started on its own `Task` and given a head start (well within the 400ms debounce window)
        // before the second call is issued directly, making the supersede ordering deterministic.
        let firstTask = Task { await session.scheduleAnalyse(source: "HARK! \"Test\"\n\nFINALE.\n") }
        try? await Task.sleep(for: .milliseconds(50))
        let second = await session.scheduleAnalyse(source: "HARK! \"Test\"\n\nFINALE.\n")
        let first = await firstTask.value

        XCTAssertNil(first, "a scheduleAnalyse call superseded before its debounce completes should return nil")
        XCTAssertNotNil(second)
    }

    /// Tests that `execute` invokes the registered output handler for a `BEHOLD` statement.
    func testExecuteInvokesOutputHandler() async {
        let session = TopsyTurvySession()
        let capture = TestOutputCapture()
        await session.setOutputHandler(capture.append)
        await session.open()
        addTeardownBlock { await session.close() }

        let status = await session.execute(source: "HARK! \"Test\"\n\nBEHOLD \"Hello\"\n\nFINALE.\n")

        XCTAssertEqual(status, 0)
        XCTAssertTrue(capture.lines.contains("Hello"))
    }

    /// Tests that `THE PROPS` reflects arguments passed via `execute`.
    func testExecutePassesArgumentsAsThePropsElement() async {
        let session = TopsyTurvySession()
        let capture = TestOutputCapture()
        await session.setOutputHandler(capture.append)
        await session.open()
        addTeardownBlock { await session.close() }

        let source = """
        HARK! "Test"

        BEHOLD VICTIM 1 ON THE PROPS

        FINALE.
        """
        let status = await session.execute(source: source, args: ["first-argument"])

        XCTAssertEqual(status, 0)
        XCTAssertTrue(capture.lines.contains("first-argument"))
    }

    /// Tests that `execute` succeeds for a two-file programme where the main source `PRAY ADMIT`s a sibling
    /// file and calls a function it declares — the Phase 2 exit-gate scenario. Exercises the toolchain's
    /// cross-file symbol resolution fix in `TopsyTurvyTypeChecker` (previously, any call to an imported
    /// function failed type-checking, since only the interpreter resolved imports, not the type checker).
    func testExecuteResolvesFunctionFromImportedFile() async {
        let session = TopsyTurvySession()
        let capture = TestOutputCapture()
        await session.setOutputHandler(capture.append)
        await session.setImportResolver { filename in
            guard filename == "greetings.topsy" else { return nil }
            return """
            HARK! "Utils"

            PRINCIPALS
            THE CURTAIN RISES.

            IT IS MY DUTY TO PERFORM greet UNDER THE TERMS OF name AS A YARN
              BEHOLD WOVEN OF "Hello, " AND name AND "!" IF YOU PLEASE.
            MY DUTY IS DISCHARGED.

            FINALE.
            """
        }
        await session.open()
        addTeardownBlock { await session.close() }

        let source = """
        HARK! "Two File Test"

        PRINCIPALS
        THE CURTAIN RISES.
        PRAY ADMIT "greetings.topsy"

        SUMMON greet WITH "Theatre" IF YOU PLEASE.

        FINALE.
        """
        let status = await session.execute(source: source)

        XCTAssertEqual(status, 0)
        XCTAssertTrue(capture.lines.contains("Hello, Theatre!"))
    }

    /// Tests that `format` normalises keyword casing to the canonical uppercase form, mirroring
    /// `NativeExportsTests.FormatSource_WithLowercaseKeywords_ReturnsCanonicalCase`.
    func testFormatNormalisesKeywordCasing() async {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock { await session.close() }

        let formatted = await session.format(source: "hark! \"Test\"\n\nfinale.\n")

        XCTAssertNotNil(formatted)
        XCTAssertTrue(formatted?.contains("HARK!") ?? false)
        XCTAssertTrue(formatted?.contains("FINALE.") ?? false)
    }

    /// Tests that `complete` offers the matching keyword for a partially typed word, exercising the real
    /// `topsyturvy_complete` round-trip (not just JSON decoding of a canned payload).
    func testCompleteOffersMatchingKeywordForPartialWord() async {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock { await session.close() }

        let result = await session.complete(source: "HAR", line: 0, column: 3)

        XCTAssertTrue(result.Items.contains { $0.Label == "HARK!" })
    }

    /// Tests that repeatedly resolving `PRAY ADMIT` imports via `execute` does not crash across many calls,
    /// exercising the cached-buffer-freed-on-next-call ownership pattern.
    func testRepeatedImportResolutionDoesNotCrash() async {
        let session = TopsyTurvySession()
        await session.setImportResolver { filename in "ASIDE: contents of \(filename)\n" }
        await session.open()
        addTeardownBlock { await session.close() }

        for index in 0..<50 {
            let source = """
            HARK! "Test"

            PRAY ADMIT "sibling-\(index).topsy"

            FINALE.
            """
            _ = await session.execute(source: source)
        }
    }
}

/// Holds output lines captured by `TopsyTurvySessionTests`, since a `@Sendable` closure cannot capture a
/// mutable local variable directly.
private final class TestOutputCapture: @unchecked Sendable {
    private(set) var lines: [String] = []

    func append(_ text: String, suppressNewline: Bool) {
        if suppressNewline, !lines.isEmpty {
            lines[lines.count - 1] += text
        } else {
            lines.append(text)
        }
    }
}
