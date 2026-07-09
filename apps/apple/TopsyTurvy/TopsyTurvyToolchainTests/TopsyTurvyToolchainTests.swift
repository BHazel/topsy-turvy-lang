import XCTest
import TopsyTurvyToolchain
@testable import Theatre

/// Tests for the `TopsyTurvyToolchain` framework.
final class TopsyTurvyToolchainTests: XCTestCase {
    /// Sets up the test fixture.
    override func setUp() {
        super.setUp()
        TestCallbackCapture.outputLines = []
    }

    /// Tests that `topsyturvy_api_version` returns 1.
    ///
    /// As the framework is built and imported separately, this ensures drift is captured early.
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

    /// Tests that `topsyturvy_tokens` reports a single "comment" token spanning multiple lines for a block comment.
    func testTokensReturnsCommentTokenSpanningMultipleLines() {
        let session = topsyturvy_session_create(captureOutputLine, resolveImportStub, nil)
        XCTAssertNotNil(session)
        defer {
            topsyturvy_session_destroy(session)
        }

        let source = "HARK! \"Test\"\n\n(ASIDE, AT SOME LENGTH:\nspans several\nlines\nEND OF ASIDE.)\n\nFINALE.\n"
        
        let result = tokens(session: session, source: source)

        let commentTokens = result.Tokens.filter { $0.Category == "comment" }
        XCTAssertEqual(commentTokens.count, 1)
        XCTAssertNotEqual(commentTokens.first?.StartLine, commentTokens.first?.EndLine)
    }

    /// Calls `topsyturvy_tokens` and decodes its JSON result.
    ///
    /// - Parameters:
    ///   - session: The session to tokenise in.
    /// - source: The source code to tokenise.
    ///
    /// - Returns: The decoded `TokenResult`.
    private func tokens(session: UnsafeMutableRawPointer?, source: String) -> TokenResult {
        let tokenResultJson = source.withCString { sourcePointer -> String in
            sourcePointer.withMemoryRebound(to: UInt8.self, capacity: source.utf8.count + 1) { utf8Pointer in
                guard let resultPointer = topsyturvy_tokens(session, utf8Pointer) else {
                    XCTFail("topsyturvy_tokens returned a null pointer")
                    return "{}"
                }

                defer {
                    topsyturvy_free(resultPointer)
                }

                return String(cString: resultPointer)
            }
        }

        let data = tokenResultJson.data(using: .utf8)!
        return try! JSONDecoder().decode(TokenResult.self, from: data)
    }

    /// Calls `topsyturvy_analyse` and decodes its JSON result.
    ///
    /// - Parameters:
    ///   - session: The session to analyse in.
    ///   - source: The source code to analyse.
    ///
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
    ///
    /// - Parameters:
    ///   - session: The session to execute in.
    ///   - source: The source code to execute.
    /// 
    /// - Returns: The exit status returned by `topsyturvy_execute`.
    private func execute(session: UnsafeMutableRawPointer?, source: String) -> Int32 {
        source.withCString { sourcePointer -> Int32 in
            sourcePointer.withMemoryRebound(to: UInt8.self, capacity: source.utf8.count + 1) { utf8Pointer in
                topsyturvy_execute(session, utf8Pointer, nil, nil)
            }
        }
    }
}
