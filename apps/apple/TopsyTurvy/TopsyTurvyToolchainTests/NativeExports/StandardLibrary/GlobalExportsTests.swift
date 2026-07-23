import XCTest
import TopsyTurvyToolchain
@testable import Theatre

/// Tests for the Standard Library global-namespace native exports (`topsyturvy_std_*`).
final class GlobalExportsTests: XCTestCase {
    /// Sets up the test fixture.
    override func setUp() {
        super.setUp()
        TestCallbackCapture.outputLines = []
        TestCallbackCapture.inputLineResult = nil
    }

    /// Tests that `topsyturvy_std_preview_behold` invokes the registered output callback directly, without a
    /// running programme.
    func testPreviewBeholdInvokesOutputCallback() {
        let session = topsyturvy_tc_session_create(captureOutputLine, resolveImportStub, provideInputLine, nil)
        XCTAssertNotNil(session)
        defer {
            topsyturvy_tc_session_destroy(session)
        }

        let text = "Hello from preview"
        let status = text.withCString { textPointer -> Int32 in
            textPointer.withMemoryRebound(to: UInt8.self, capacity: text.utf8.count + 1) { utf8Pointer in
                topsyturvy_std_preview_behold(session, utf8Pointer, 1)
            }
        }

        XCTAssertEqual(status, 0)
        XCTAssertTrue(TestCallbackCapture.outputLines.contains(text))
    }

    /// Tests that `topsyturvy_std_preview_pray_tell` reads through the registered input callback directly,
    /// without a running programme.
    func testPreviewPrayTellReadsFromInputCallback() {
        TestCallbackCapture.inputLineResult = "a line typed by the user"
        let session = topsyturvy_tc_session_create(captureOutputLine, resolveImportStub, provideInputLine, nil)
        XCTAssertNotNil(session)
        defer {
            topsyturvy_tc_session_destroy(session)
        }

        guard let resultPointer = topsyturvy_std_preview_pray_tell(session) else {
            XCTFail("topsyturvy_std_preview_pray_tell returned a null pointer")
            return
        }
        defer {
            topsyturvy_tc_free(resultPointer)
        }

        XCTAssertEqual(String(cString: resultPointer), "a line typed by the user")
    }
}
