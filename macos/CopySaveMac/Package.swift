// swift-tools-version: 5.9
import PackageDescription

let package = Package(
    name: "CopySaveMac",
    platforms: [
        .macOS(.v13)
    ],
    products: [
        .executable(name: "CopySaveMac", targets: ["CopySaveMac"])
    ],
    targets: [
        .executableTarget(
            name: "CopySaveMac",
            path: "Sources/CopySaveMac"
        )
    ]
)
