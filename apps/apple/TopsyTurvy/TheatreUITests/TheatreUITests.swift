import XCTest

/// End-to-end UI tests for the core Theatre flows: opening/editing a document, running a programme with
/// streamed output, and stopping a long-running one.
///
/// Each test opens its own fresh document. When run back-to-back within the same `xcodebuild test` session,
/// the iOS Simulator's system document browser (`com.apple.DocumentManager`) can occasionally leave a
/// document-loading state that takes longer than usual to settle on the second launch — a Simulator
/// infrastructure quirk observed during development, not a defect in Theatre itself (each scenario below was
/// independently verified to pass reliably in isolation, e.g. via `-only-testing:`). Both tests use a generous
/// timeout for the initial document-open step to absorb that.
final class TheatreUITests: XCTestCase {
    override func setUpWithError() throws {
        continueAfterFailure = false
    }

    /// Launches the app and, on platforms that show a document browser at launch (iOS/iPadOS), creates a new
    /// blank document so the editor and run panel are on screen.
    private func launchIntoDocument() -> XCUIApplication {
        let app = XCUIApplication()
        app.launch()

        if !app.buttons["RunControlsView.performButton"].waitForExistence(timeout: 3) {
            let createDocumentButton = app.buttons["Create Document"]
            if createDocumentButton.waitForExistence(timeout: 10) {
                createDocumentButton.tap()
            }
        }

        XCTAssertTrue(
            app.buttons["RunControlsView.performButton"].waitForExistence(timeout: 30),
            "expected the run panel to appear after opening a document")

        return app
    }

    /// Types `text` into the editor's text view.
    private func typeIntoEditor(_ app: XCUIApplication, text: String) {
        let editor = app.textViews.firstMatch
        XCTAssertTrue(editor.waitForExistence(timeout: 5), "expected the code editor's text view to exist")
        editor.tap()
        editor.typeText(text)
    }

    /// Tests that running a `BEHOLD`-only programme streams its output into the output pane.
    func testPerformStreamsOutputForBeholdStatement() {
        let app = launchIntoDocument()

        typeIntoEditor(app, text: "HARK! \"Test\"\n\nBEHOLD \"Hello, Theatre!\"\n\nFINALE.\n")

        app.buttons["RunControlsView.performButton"].tap()

        let output = app.staticTexts["Hello, Theatre!"]
        XCTAssertTrue(output.waitForExistence(timeout: 10), "expected the output pane to show the BEHOLD line")
    }

    /// Tests that the Format toolbar button normalises keyword casing to the canonical uppercase form.
    func testFormatNormalisesKeywordCasing() {
        let app = launchIntoDocument()

        typeIntoEditor(app, text: "hark! \"Test\"\n\nfinale.\n")

        app.buttons["TheatreDocumentView.formatButton"].tap()

        let editor = app.textViews.firstMatch
        let formatted = expectation(for: NSPredicate(format: "value CONTAINS 'HARK!'"), evaluatedWith: editor)
        wait(for: [formatted], timeout: 5)
    }

    /// Tests that Stop halts a deliberately infinite loop and that the Perform button re-enables.
    func testStopHaltsInfiniteLoop() {
        let app = launchIntoDocument()

        typeIntoEditor(
            app,
            text: "HARK! \"Test\"\n\nBY A LEGAL FICTION\n  BEHOLD \"looping\"\nTHE TERM EXPIRES.\n\nFINALE.\n")

        let performButton = app.buttons["RunControlsView.performButton"]
        performButton.tap()

        let stopButton = app.buttons["RunControlsView.stopButton"]
        XCTAssertTrue(stopButton.waitForExistence(timeout: 5))
        stopButton.tap()

        // No status label to check (removed as a UI smell — see RunPanelView); instead confirm Stop actually
        // interrupted the infinite loop by waiting for Perform to re-enable, which an uncancelled loop never does.
        let performReenabled = expectation(for: NSPredicate(format: "isEnabled == true"), evaluatedWith: performButton)
        wait(for: [performReenabled], timeout: 10)
    }
}
