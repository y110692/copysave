import AppKit

enum ClipboardPayloadReader {
    static func hasSavablePayload() -> Bool {
        readPayload() != nil
    }

    static func readPayload() -> String? {
        let pasteboard = NSPasteboard.general
        if pasteboard.canReadObject(forClasses: [NSURL.self], options: nil) {
            return nil
        }

        if let text = pasteboard.string(forType: .string)?.replacingOccurrences(of: "\0", with: ""),
           !text.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty {
            return text
        }

        if let html = pasteboard.string(forType: .html)?
            .replacingOccurrences(of: "<!--StartFragment-->", with: "")
            .replacingOccurrences(of: "<!--EndFragment-->", with: "")
            .trimmingCharacters(in: .whitespacesAndNewlines),
           !html.isEmpty {
            return html
        }

        return nil
    }
}
