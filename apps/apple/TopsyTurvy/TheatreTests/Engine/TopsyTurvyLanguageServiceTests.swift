import LanguageSupport
import XCTest
@testable import Theatre

/// Tests for the `TopsyTurvyLanguageService`.
final class TopsyTurvyLanguageServiceTests: XCTestCase {
    /// Tests that `openDocument` populates `diagnostics` for an invalid programme.
    func testOpenDocumentPopulatesDiagnosticsForInvalidProgramme() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock {
            await session.close()
        }

        let service = TopsyTurvyLanguageService(session: session)
        let source = "THIS IS NOT VALID TOPSY TURVY"
        
        try await service.openDocument(with: source, locationService: TestLocationService(source: source))

        try? await Task.sleep(for: .milliseconds(500))
        XCTAssertFalse(service.diagnostics.value.isEmpty)
    }

    /// Tests that `openDocument` reports no diagnostics for a valid programme.
    func testOpenDocumentReportsNoDiagnosticsForValidProgramme() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock {
            await session.close()
        }

        let service = TopsyTurvyLanguageService(session: session)
        let source = "HARK! \"Test\"\n\nFINALE.\n"
        
        try await service.openDocument(with: source, locationService: TestLocationService(source: source))

        try? await Task.sleep(for: .milliseconds(500))
        XCTAssertTrue(service.diagnostics.value.isEmpty)
    }

    /// Tests that `closeDocument` clears `diagnostics` to empty.
    func testCloseDocumentClearsDiagnostics() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock {
            await session.close()
        }

        let service = TopsyTurvyLanguageService(session: session)
        let source = "THIS IS NOT VALID TOPSY TURVY"
        try await service.openDocument(with: source, locationService: TestLocationService(source: source))
        try? await Task.sleep(for: .milliseconds(500))
        XCTAssertFalse(service.diagnostics.value.isEmpty)

        try await service.closeDocument()

        XCTAssertTrue(service.diagnostics.value.isEmpty)
        XCTAssertFalse(service.isOpen)
    }

    /// Tests that an `updateText` call superseded by a later call before its debounce completes does not
    /// interfere with the diagnostics reported by the newer, superseding call.
    func testUpdateTextDiscardsSupersededCalls() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock {
            await session.close()
        }

        let service = TopsyTurvyLanguageService(session: session)
        let validSource = "HARK! \"Test\"\n\nFINALE.\n"
        try await service.openDocument(with: validSource, locationService: TestLocationService(source: validSource))
        try? await Task.sleep(for: .milliseconds(500))
        let invalidSource = "THIS IS NOT VALID TOPSY TURVY"
        Task {
            await service.updateText(invalidSource)
        }
        
        try? await Task.sleep(for: .milliseconds(50))
        
        await service.updateText(validSource)
        
        try? await Task.sleep(for: .milliseconds(500))
        XCTAssertTrue(service.diagnostics.value.isEmpty, "the superseding call's (valid) result should win")
    }
    
    /// Tests that `documentDidChange` does not overwrite `currentText` thus preventing parsing errors.
    func testDocumentDidChangeDoesNotCorruptStateWithAFragment() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock {
            await session.close()
        }

        let service = TopsyTurvyLanguageService(session: session)
        let validSource = "HARK! \"Test\"\n\nPRAY WELCOME AllLords AS A PEER BEING 5\n\nFINALE.\n"
        let locationService = TestLocationService(source: validSource)
        try await service.openDocument(with: validSource, locationService: locationService)
        try? await Task.sleep(for: .milliseconds(500))
        XCTAssertTrue(service.diagnostics.value.isEmpty)
        
        try await service.documentDidChange(position: 20, changeInLength: 1, lineChange: 0, columnChange: 1, newText: "!")
        try? await Task.sleep(for: .milliseconds(500))

        XCTAssertTrue(
            service.diagnostics.value.isEmpty,
            "documentDidChange must not re-analyse using its fragment newText, corrupting diagnostics for the real document"
        )

        guard case .success(let offset) = locationService.location(from: TextLocation(zeroBasedLine: 2, column: 15)) else {
            return XCTFail("could not compute a string offset for the test fixture's location")
        }
        
        let markdown = await service.hoverContent(at: offset)
        XCTAssertTrue(
            markdown?.contains("AllLords") ?? false,
            "hoverContent must still operate against the real document, not a fragment documentDidChange may have leaked in"
        )
    }

    /// Tests that `hoverContent(at:)` returns Markdown content for a declared symbol.
    func testHoverContentReturnsMarkdownForDeclaredSymbol() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock {
            await session.close()
        }

        let service = TopsyTurvyLanguageService(session: session)
        let source = "HARK! \"Test\"\n\nPRAY WELCOME AllLords AS A PEER BEING 5\n\nFINALE.\n"
        let locationService = TestLocationService(source: source)
        try await service.openDocument(with: source, locationService: locationService)

        guard case .success(let offset) = locationService.location(from: TextLocation(zeroBasedLine: 2, column: 15)) else {
            return XCTFail("could not compute a string offset for the test fixture's location")
        }

        let markdown = await service.hoverContent(at: offset)

        XCTAssertNotNil(markdown)
        XCTAssertTrue(markdown?.contains("AllLords") ?? false)
    }

    /// Tests that `hoverContent(at:)` returns `nil` when no symbol is found at the requested location.
    func testHoverContentReturnsNilWhenNoSymbolFound() async throws {
        let session = TopsyTurvySession()
        await session.open()
        addTeardownBlock {
            await session.close()
        }

        let service = TopsyTurvyLanguageService(session: session)
        let source = "HARK! \"Test\"\n\nFINALE.\n"
        try await service.openDocument(with: source, locationService: TestLocationService(source: source))

        let markdown = await service.hoverContent(at: 0)

        XCTAssertNil(markdown)
    }
}
