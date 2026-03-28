import Foundation

enum FileNameHelper {
    static func sanitizeName(_ value: String) -> String {
        let forbidden = CharacterSet(charactersIn: "<>:\"/\\|?*")
        let cleaned = value
            .components(separatedBy: forbidden)
            .joined()
            .trimmingCharacters(in: .whitespacesAndNewlines.union(.controlCharacters))
            .trimmingCharacters(in: CharacterSet(charactersIn: ". "))
        return cleaned.isEmpty ? "clipboard" : cleaned
    }

    static func sanitizeExtension(_ value: String) -> String {
        let trimmed = value.trimmingCharacters(in: .whitespacesAndNewlines)
        let cleaned = trimmed
            .trimmingCharacters(in: CharacterSet(charactersIn: "."))
            .lowercased()
            .filter { $0.isLetter || $0.isNumber || $0 == "_" || $0 == "-" }
        return cleaned.isEmpty ? "txt" : cleaned
    }

    static func uniqueFileURL(in directory: URL, fileName: String, fileExtension: String) -> URL {
        let safeName = sanitizeName(fileName)
        let safeExtension = sanitizeExtension(fileExtension)
        var candidate = directory.appendingPathComponent(safeName).appendingPathExtension(safeExtension)
        var counter = 2

        while FileManager.default.fileExists(atPath: candidate.path) {
            candidate = directory
                .appendingPathComponent("\(safeName)_\(counter)")
                .appendingPathExtension(safeExtension)
            counter += 1
        }

        return candidate
    }
}
