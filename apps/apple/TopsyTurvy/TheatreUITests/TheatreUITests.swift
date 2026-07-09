import XCTest

/// End-to-end UI tests for the core Theatre flows.
final class TheatreUITests: XCTestCase {
    /// Sets up the fixture with error.
    override func setUpWithError() throws {
        continueAfterFailure = false
    }

    /// Tests that running a programme streams its output into the output pane.
    func testPerformStreamsOutputForBeholdStatement() {
        let app = self.launchIntoDocument()
        self.typeIntoEditor(app, text: "HARK! \"Test\"\n\nBEHOLD \"Hello, Theatre!\"\n\nFINALE.\n")

        app.buttons["RunToolbarButtons.performButton"].tap()

        let output = app.staticTexts["Hello, Theatre!"]
        XCTAssertTrue(output.waitForExistence(timeout: 10), "expected the output pane to show the BEHOLD line")
    }

    /// Tests that the Format toolbar button normalises keyword casing to the canonical uppercase form.
    func testFormatNormalisesKeywordCasing() {
        let app = self.launchIntoDocument()
        self.typeIntoEditor(app, text: "hark! \"Test\"\n\nfinale.\n")
        
        var formatButton = app.buttons["EditorHostView.formatButton"]
        if !formatButton.exists {
            app.buttons["OverflowBarButtonItem"].tap()
            formatButton = app.buttons["Format"]
        }
        
        if !formatButton.waitForExistence(timeout: 3) {
            let visibleButtons = app.buttons.allElementsBoundByIndex.map { "\($0.label) [\($0.identifier)]" }
            XCTFail("expected the Format button to exist directly or in the overflow menu; visible buttons: \(visibleButtons)")
            return
        }
        
        formatButton.tap()

        let editor = app.textViews.firstMatch
        let formatted = expectation(for: NSPredicate(format: "value CONTAINS 'HARK!'"), evaluatedWith: editor)
        wait(for: [formatted], timeout: 5)
    }

    /// Tests that Stop halts an infinite loop and that the Perform button re-enables.
    func testStopHaltsInfiniteLoop() {
        let app = self.launchIntoDocument()

        self.typeIntoEditor(
            app,
            text: "HARK! \"Test\"\n\nBY A LEGAL FICTION\n  BEHOLD \"looping\"\nTHE TERM EXPIRES.\n\nFINALE.\n")

        let performButton = app.buttons["RunToolbarButtons.performButton"]
        performButton.tap()
        
        let closeOutputButton = app.buttons["Close"]
        if closeOutputButton.waitForExistence(timeout: 3) {
            closeOutputButton.tap()
        }

        let stopButton = app.buttons["RunToolbarButtons.stopButton"]
        XCTAssertTrue(stopButton.waitForExistence(timeout: 5))
        stopButton.tap()
        
        let performReenabled = expectation(for: NSPredicate(format: "isEnabled == true"), evaluatedWith: performButton)
        wait(for: [performReenabled], timeout: 10)
    }
    
    /// Launches the app and creates a new, blank document so the editor and run panel are on screen.
    ///
    /// `DocumentGroup` can reopen the previously open document on relaunch instead of showing its browser, so
    /// if a document is already open, this returns to the browser first.
    ///
    /// - Returns: The app proxy for testing.
    private func launchIntoDocument() -> XCUIApplication {
        let app = XCUIApplication()
        app.launch()

        if app.buttons["RunToolbarButtons.performButton"].waitForExistence(timeout: 3) {
            let backButton = app.buttons["BackButton"]
            if backButton.waitForExistence(timeout: 3) {
                backButton.tap()
            }
        }

        let createDocumentButton = app.buttons["Create Document"]
        XCTAssertTrue(createDocumentButton.waitForExistence(timeout: 10), "expected the document browser Create Document button to appear")
        createDocumentButton.tap()

        XCTAssertTrue(
            app.buttons["RunToolbarButtons.performButton"].waitForExistence(timeout: 30),
            "expected the Run panel to appear after opening a document")

        return app
    }

    /// Types specified into the editor text view.
    ///
    /// - Parameters:
    ///   - app: The app proxy for testing.
    ///   - text: The text to insert.
    private func typeIntoEditor(_ app: XCUIApplication, text: String) {
        let editor = app.textViews.firstMatch
        XCTAssertTrue(editor.waitForExistence(timeout: 5), "expected the code editor text view to exist")

        // A coordinate-based tap reliably focuses this custom text view; XCUITest's default `tap()` does not.
        for _ in 0..<5 where !self.hasKeyboardFocus(editor) {
            editor.coordinate(withNormalizedOffset: CGVector(dx: 0.3, dy: 0.3)).tap()
            usleep(300_000)
        }

        XCTAssertTrue(self.hasKeyboardFocus(editor), "expected the code editor to gain keyboard focus after tapping")
        editor.typeText(text)
    }

    /// Gets whether an element currently has keyboard focus.
    ///
    /// Read via Key-Value Coding since `hasKeyboardFocus` is not part of the `XCUIElement` compiled Swift interface.
    ///
    /// - Parameters:
    ///   - element: The element to check.
    /// 
    /// - Returns: `true` if `element` has keyboard focus.
    private func hasKeyboardFocus(_ element: XCUIElement) -> Bool {
        (element.value(forKey: "hasKeyboardFocus") as? Bool) ?? false
    }
}
