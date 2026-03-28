import ApplicationServices
import Carbon.HIToolbox
import Foundation

final class HotKeyMonitor {
    private var eventTap: CFMachPort?
    private var runLoopSource: CFRunLoopSource?
    private var swallowUntilKeyUp = false
    var onIntercept: (() -> Void)?

    func start() {
        guard eventTap == nil else {
            return
        }

        let keyDownMask = 1 << CGEventType.keyDown.rawValue
        let keyUpMask = 1 << CGEventType.keyUp.rawValue
        let mask = CGEventMask(keyDownMask | keyUpMask)

        let callback: CGEventTapCallBack = { _, type, event, userInfo in
            guard let userInfo else {
                return Unmanaged.passUnretained(event)
            }

            let monitor = Unmanaged<HotKeyMonitor>.fromOpaque(userInfo).takeUnretainedValue()
            return monitor.handle(type: type, event: event)
        }

        let pointer = UnsafeMutableRawPointer(Unmanaged.passUnretained(self).toOpaque())
        eventTap = CGEvent.tapCreate(
            tap: .cgSessionEventTap,
            place: .headInsertEventTap,
            options: .defaultTap,
            eventsOfInterest: mask,
            callback: callback,
            userInfo: pointer
        )

        guard let eventTap else {
            return
        }

        runLoopSource = CFMachPortCreateRunLoopSource(kCFAllocatorDefault, eventTap, 0)
        if let runLoopSource {
            CFRunLoopAddSource(CFRunLoopGetMain(), runLoopSource, .commonModes)
        }

        CGEvent.tapEnable(tap: eventTap, enable: true)
    }

    func stop() {
        if let runLoopSource {
            CFRunLoopRemoveSource(CFRunLoopGetMain(), runLoopSource, .commonModes)
        }

        if let eventTap {
            CFMachPortInvalidate(eventTap)
        }

        runLoopSource = nil
        eventTap = nil
    }

    private func handle(type: CGEventType, event: CGEvent) -> Unmanaged<CGEvent>? {
        if type == .tapDisabledByTimeout || type == .tapDisabledByUserInput {
            if let eventTap {
                CGEvent.tapEnable(tap: eventTap, enable: true)
            }
            return Unmanaged.passUnretained(event)
        }

        guard type == .keyDown || type == .keyUp else {
            return Unmanaged.passUnretained(event)
        }

        let keyCode = Int(event.getIntegerValueField(.keyboardEventKeycode))
        let isDown = type == .keyDown
        let isUp = type == .keyUp

        if keyCode == Int(kVK_ANSI_V) {
            if swallowUntilKeyUp && isUp {
                swallowUntilKeyUp = false
                return nil
            }

            if !swallowUntilKeyUp
                && isDown
                && hasCommandOnly(event.flags)
                && FinderContext.isEligibleFinderForeground()
                && ClipboardPayloadReader.hasSavablePayload() {
                swallowUntilKeyUp = true
                DispatchQueue.main.async { [weak self] in
                    self?.onIntercept?()
                }
                return nil
            }
        }

        return Unmanaged.passUnretained(event)
    }

    private func hasCommandOnly(_ flags: CGEventFlags) -> Bool {
        let relevant: CGEventFlags = [.maskCommand, .maskShift, .maskControl, .maskAlternate]
        return flags.intersection(relevant) == .maskCommand
    }
}
