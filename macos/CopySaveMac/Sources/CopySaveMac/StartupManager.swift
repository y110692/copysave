import Foundation

enum StartupManager {
    private static let agentLabel = "com.copysave.mac"

    static func ensureLaunchAgent() {
        let fileManager = FileManager.default
        let launchAgentsURL = fileManager.homeDirectoryForCurrentUser
            .appendingPathComponent("Library")
            .appendingPathComponent("LaunchAgents")

        try? fileManager.createDirectory(
            at: launchAgentsURL,
            withIntermediateDirectories: true,
            attributes: nil
        )

        let executableURL = URL(fileURLWithPath: CommandLine.arguments[0]).standardizedFileURL
        let plistURL = launchAgentsURL.appendingPathComponent("\(agentLabel).plist")
        let plist: [String: Any] = [
            "Label": agentLabel,
            "ProgramArguments": [executableURL.path],
            "RunAtLoad": true,
            "KeepAlive": false
        ]

        (plist as NSDictionary).write(to: plistURL, atomically: true)
    }
}
