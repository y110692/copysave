import AppKit

final class SaveDialogController: NSWindowController {
    private let nameField = NSTextField(string: "clipboard")
    private let extensionField = NSTextField(string: "txt")

    convenience init(defaultName: String, defaultExtension: String) {
        let panel = NSPanel(
            contentRect: NSRect(x: 0, y: 0, width: 360, height: 220),
            styleMask: [.borderless],
            backing: .buffered,
            defer: false
        )

        panel.isOpaque = false
        panel.backgroundColor = .clear
        panel.level = .modalPanel
        panel.hasShadow = true
        panel.collectionBehavior = [.canJoinAllSpaces, .fullScreenAuxiliary]

        self.init(window: panel)
        nameField.stringValue = defaultName
        extensionField.stringValue = defaultExtension
        configureWindow(panel)
    }

    func runModal() -> (name: String, fileExtension: String)? {
        guard let window else {
            return nil
        }

        NSApp.activate(ignoringOtherApps: true)
        window.center()
        let response = NSApp.runModal(for: window)
        window.orderOut(nil)

        guard response == .OK else {
            return nil
        }

        return (
            FileNameHelper.sanitizeName(nameField.stringValue),
            FileNameHelper.sanitizeExtension(extensionField.stringValue)
        )
    }

    private func configureWindow(_ panel: NSPanel) {
        let rootView = RoundedCardView(frame: panel.contentView?.bounds ?? .zero)
        rootView.translatesAutoresizingMaskIntoConstraints = false

        let container = NSView(frame: panel.contentView?.bounds ?? .zero)
        container.wantsLayer = true
        container.layer?.backgroundColor = NSColor.clear.cgColor

        let nameLabel = makeLabel("name")
        let extensionLabel = makeLabel("extension")
        let okButton = makeButton()

        nameField.translatesAutoresizingMaskIntoConstraints = false
        extensionField.translatesAutoresizingMaskIntoConstraints = false
        okButton.translatesAutoresizingMaskIntoConstraints = false

        [nameField, extensionField].forEach { field in
            field.font = NSFont.systemFont(ofSize: 14)
            field.isBordered = true
            field.focusRingType = .none
        }

        rootView.addSubview(nameLabel)
        rootView.addSubview(nameField)
        rootView.addSubview(extensionLabel)
        rootView.addSubview(extensionField)
        rootView.addSubview(okButton)
        container.addSubview(rootView)
        panel.contentView = container
        panel.initialFirstResponder = nameField

        NSLayoutConstraint.activate([
            rootView.leadingAnchor.constraint(equalTo: container.leadingAnchor, constant: 14),
            rootView.trailingAnchor.constraint(equalTo: container.trailingAnchor, constant: -14),
            rootView.topAnchor.constraint(equalTo: container.topAnchor, constant: 14),
            rootView.bottomAnchor.constraint(equalTo: container.bottomAnchor, constant: -14),

            nameLabel.leadingAnchor.constraint(equalTo: rootView.leadingAnchor, constant: 14),
            nameLabel.topAnchor.constraint(equalTo: rootView.topAnchor, constant: 14),
            nameLabel.trailingAnchor.constraint(equalTo: rootView.trailingAnchor, constant: -14),

            nameField.leadingAnchor.constraint(equalTo: nameLabel.leadingAnchor),
            nameField.trailingAnchor.constraint(equalTo: nameLabel.trailingAnchor),
            nameField.topAnchor.constraint(equalTo: nameLabel.bottomAnchor, constant: 6),
            nameField.heightAnchor.constraint(equalToConstant: 30),

            extensionLabel.leadingAnchor.constraint(equalTo: nameLabel.leadingAnchor),
            extensionLabel.trailingAnchor.constraint(equalTo: nameLabel.trailingAnchor),
            extensionLabel.topAnchor.constraint(equalTo: nameField.bottomAnchor, constant: 14),

            extensionField.leadingAnchor.constraint(equalTo: nameLabel.leadingAnchor),
            extensionField.trailingAnchor.constraint(equalTo: nameLabel.trailingAnchor),
            extensionField.topAnchor.constraint(equalTo: extensionLabel.bottomAnchor, constant: 6),
            extensionField.heightAnchor.constraint(equalToConstant: 30),

            okButton.trailingAnchor.constraint(equalTo: nameLabel.trailingAnchor),
            okButton.topAnchor.constraint(equalTo: extensionField.bottomAnchor, constant: 16),
            okButton.bottomAnchor.constraint(equalTo: rootView.bottomAnchor, constant: -14)
        ])

        DispatchQueue.main.async {
            self.nameField.selectText(nil)
        }
    }

    private func makeLabel(_ text: String) -> NSTextField {
        let label = NSTextField(labelWithString: text)
        label.translatesAutoresizingMaskIntoConstraints = false
        label.font = NSFont.systemFont(ofSize: 12)
        label.textColor = NSColor(calibratedRed: 0.42, green: 0.36, blue: 0.32, alpha: 1)
        return label
    }

    private func makeButton() -> NSButton {
        let button = NSButton(title: "ok", target: self, action: #selector(submit))
        button.bezelStyle = .rounded
        button.keyEquivalent = "\r"
        return button
    }

    @objc private func submit() {
        NSApp.stopModal(withCode: .OK)
        close()
    }

    override func cancelOperation(_ sender: Any?) {
        NSApp.stopModal(withCode: .cancel)
        close()
    }
}

private final class RoundedCardView: NSView {
    override init(frame frameRect: NSRect) {
        super.init(frame: frameRect)
        translatesAutoresizingMaskIntoConstraints = false
        wantsLayer = true
        layer?.cornerRadius = 18
        layer?.borderWidth = 1
        layer?.borderColor = NSColor(calibratedWhite: 0.2, alpha: 0.14).cgColor
        layer?.backgroundColor = NSColor(calibratedRed: 1, green: 0.98, blue: 0.94, alpha: 0.98).cgColor
    }

    @available(*, unavailable)
    required init?(coder: NSCoder) {
        fatalError("init(coder:) has not been implemented")
    }
}
