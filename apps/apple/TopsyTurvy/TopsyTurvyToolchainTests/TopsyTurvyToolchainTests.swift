import XCTest
import TopsyTurvyToolchain

/// Tests for the `TopsyTurvyToolchain` framework.
final class TopsyTurvyToolchainTests: XCTestCase {
    /// Sets up the test fixture.
    override func setUp() {
        super.setUp()
        TestCallbackCapture.outputLines = []
    }

    /// Tests that `topsyturvy_api_version` returns 1.
    func testApiVersionReturnsOne() {
        XCTAssertEqual(topsyturvy_api_version(), 1)
    }

    /// Tests that `topsyturvy_analyse` reports success for a valid programme.
    func testAnalyseSourceReportsSuccessForValidProgramme() {
        let session = topsyturvy_session_create(captureOutputLine, resolveImportStub, nil)
        XCTAssertNotNil(session)
        defer {
            topsyturvy_session_destroy(session)
        }

        let result = analyse(session: session, source: "HARK! \"Test\"\n\nFINALE.\n")

        XCTAssertTrue(result.Success)
        XCTAssertTrue(result.Diagnostics.isEmpty)
    }

    /// Tests that `topsyturvy_execute` invokes the registered output callback for a `BEHOLD` statement.
    func testExecuteProgrammeInvokesOutputCallback() {
        let session = topsyturvy_session_create(captureOutputLine, resolveImportStub, nil)
        XCTAssertNotNil(session)
        defer {
            topsyturvy_session_destroy(session)
        }

        let status = execute(session: session, source: "HARK! \"Test\"\n\nBEHOLD \"Hello\"\n\nFINALE.\n")

        XCTAssertEqual(status, 0)
        XCTAssertTrue(TestCallbackCapture.outputLines.contains("Hello"))
    }

    /// Calls `topsyturvy_analyse` and decodes its JSON result.
    /// - Parameter session: The session to analyse in.
    /// - Parameter source: The source code to analyse.
    /// - Returns: The decoded `AnalysisResult`.
    private func analyse(session: UnsafeMutableRawPointer?, source: String) -> AnalysisResult {
        let analysisResultJson = source.withCString { sourcePointer -> String in
            sourcePointer.withMemoryRebound(to: UInt8.self, capacity: source.utf8.count + 1) { utf8Pointer in
                guard let resultPointer = topsyturvy_analyse(session, utf8Pointer) else {
                    XCTFail("topsyturvy_analyse returned a null pointer")
                    return "{}"
                }

                defer {
                    topsyturvy_free(resultPointer)
                }

                return String(cString: resultPointer)
            }
        }

        let data = analysisResultJson.data(using: .utf8)!
        return try! JSONDecoder().decode(AnalysisResult.self, from: data)
    }

    /// Calls `topsyturvy_execute` with no arguments or pre-seeded input.
    /// - Parameter session: The session to execute in.
    /// - Parameter source: The source code to execute.
    /// - Returns: The exit status returned by `topsyturvy_execute`.
    private func execute(session: UnsafeMutableRawPointer?, source: String) -> Int32 {
        source.withCString { sourcePointer -> Int32 in
            sourcePointer.withMemoryRebound(to: UInt8.self, capacity: source.utf8.count + 1) { utf8Pointer in
                topsyturvy_execute(session, utf8Pointer, nil, nil)
            }
        }
    }
}
