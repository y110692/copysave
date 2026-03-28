import AppKit

final class AppDelegate: NSObject, NSApplicationDelegate {
    private let hotKeyMonitor = HotKeyMonitor()
    private var statusItem: NSStatusItem?

    func applicationDidFinishLaunching(_ notification: Notification) {
        StartupManager.ensureLaunchAgent()
        configureStatusItem()
        hotKeyMonitor.onIntercept = { [weak self] in
            self?.handleInterceptedPaste()
        }
        hotKeyMonitor.start()
    }

    func applicationWillTerminate(_ notification: Notification) {
        hotKeyMonitor.stop()
    }

    private func configureStatusItem() {
        statusItem = NSStatusBar.system.statusItem(withLength: NSStatusItem.variableLength)
        statusItem?.button?.title = "CopySave"

        let menu = NSMenu()
        menu.addItem(NSMenuItem(title: "Quit", action: #selector(quit), keyEquivalent: "q"))
        statusItem?.menu = menu
    }

    private func handleInterceptedPaste() {
        guard let folderURL = FinderContext.frontFolderURL(),
              let payload = ClipboardPayloadReader.readPayload()
        else {
            return
        }

        let dialog = SaveDialogController(defaultName: "clipboard", defaultExtension: "txt")
        guard let result = dialog.runModal() else {
            return
        }

        let targetURL = FileNameHelper.uniqueFileURL(
            in: folderURL,
            fileName: result.name,
            fileExtension: result.fileExtension
        )

        do {
            try payload.write(to: targetURL, atomically: true, encoding: .utf8)
        } catch {
            NSSound.beep()
        }
    }

    @objc private func quit() {
        NSApp.terminate(nil)
    }
}
