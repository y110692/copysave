import AppKit
import ApplicationServices

enum FinderContext {
    static func isEligibleFinderForeground() -> Bool {
        guard let app = NSWorkspace.shared.frontmostApplication,
              app.bundleIdentifier == "com.apple.finder"
        else {
            return false
        }

        let applicationElement = AXUIElementCreateApplication(app.processIdentifier)
        var focusedValue: CFTypeRef?
        let result = AXUIElementCopyAttributeValue(applicationElement, kAXFocusedUIElementAttribute as CFString, &focusedValue)
        guard result == .success, let focusedElement = axElement(from: focusedValue) else {
            return true
        }

        return !isTextInputElement(focusedElement)
    }

    static func frontFolderURL() -> URL? {
        let source = """
        tell application "Finder"
            if (count of Finder windows) is 0 then
                return POSIX path of (desktop as alias)
            end if
            return POSIX path of (target of front Finder window as alias)
        end tell
        """

        var scriptError: NSDictionary?
        guard let script = NSAppleScript(source: source),
              let descriptor = script.executeAndReturnError(&scriptError).stringValue
        else {
            return nil
        }

        let path = descriptor.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !path.isEmpty else {
            return nil
        }

        let url = URL(fileURLWithPath: path)
        return FileManager.default.fileExists(atPath: url.path) ? url : nil
    }

    private static func isTextInputElement(_ element: AXUIElement) -> Bool {
        var current: AXUIElement? = element

        while let node = current {
            let role = attributeValue(kAXRoleAttribute, for: node)
            let subrole = attributeValue(kAXSubroleAttribute, for: node)

            if role == (kAXTextFieldRole as String)
                || role == (kAXTextAreaRole as String)
                || role == (kAXComboBoxRole as String)
                || subrole == (kAXSearchFieldSubrole as String) {
                return true
            }

            current = parent(of: node)
        }

        return false
    }

    private static func attributeValue(_ attribute: CFString, for element: AXUIElement) -> String? {
        var value: CFTypeRef?
        guard AXUIElementCopyAttributeValue(element, attribute, &value) == .success else {
            return nil
        }

        return value as? String
    }

    private static func parent(of element: AXUIElement) -> AXUIElement? {
        var value: CFTypeRef?
        guard AXUIElementCopyAttributeValue(element, kAXParentAttribute as CFString, &value) == .success else {
            return nil
        }

        return axElement(from: value)
    }

    private static func axElement(from value: CFTypeRef?) -> AXUIElement? {
        guard let value else {
            return nil
        }

        guard CFGetTypeID(value) == AXUIElementGetTypeID() else {
            return nil
        }

        return unsafeBitCast(value, to: AXUIElement.self)
    }
}
