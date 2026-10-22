// Generals Vanguard launcher for macOS.
// Copyright (C) 2026 KuiseRapper
// SPDX-License-Identifier: GPL-3.0-or-later

import SwiftUI
import AppKit
import Foundation
import CryptoKit

struct ReleaseInfo: Codable {
    let version: String
    let url: String
    let sha256: String
    let size: Int?
}

struct Manifest: Codable {
    let launcher: ReleaseInfo
    let game: ReleaseInfo
    let notes: String?
}

@MainActor
final class LauncherModel: ObservableObject {
    @Published var installed: String?
    @Published var status = "Checking your installation…"
    @Published var busy = false
    @Published var showUpdate = false
    @Published var updateTitle = ""
    @Published var notes = ""
    @Published var progress: Double = 0
    @Published var banner: String?
    @Published var manifest: Manifest?
    @Published var zeroHour = ""

    let launcherVersion = Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "0.4.0"
    let manifestURL = URL(string: "https://github.com/KuiseRapper/generals-vanguard/releases/latest/download/launcher-mac.json")!
    let hero: NSImage?
    let support: URL

    var gameDir: URL { support.appendingPathComponent("game", isDirectory: true) }
    var gameApp: URL { gameDir.appendingPathComponent("Generals Vanguard.app", isDirectory: true) }

    init() {
        support = FileManager.default.urls(for: .applicationSupportDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("GeneralsVanguard", isDirectory: true)
        try? FileManager.default.createDirectory(at: support, withIntermediateDirectories: true)
        if let url = Bundle.main.url(forResource: "Background", withExtension: "png"),
           let image = NSImage(contentsOf: url) {
            hero = image
        } else {
            hero = nil
        }
        if let saved = UserDefaults.standard.string(forKey: "ZeroHourPath"),
           FileManager.default.fileExists(atPath: saved + "/INIZH.big") {
            zeroHour = saved
        } else {
            let candidate = FileManager.default.homeDirectoryForCurrentUser
                .appendingPathComponent("zh-data/zerohour").path
            zeroHour = FileManager.default.fileExists(atPath: candidate + "/INIZH.big") ? candidate : ""
        }
        installed = Self.readInstalled(gameDir)
        status = installed.map { "Game \($0) installed." } ?? "The game has not been installed yet."
        Task { await check(silent: true) }
    }

    static func readInstalled(_ directory: URL) -> String? {
        let file = directory.appendingPathComponent("version.txt")
        guard let value = try? String(contentsOf: file, encoding: .utf8) else { return nil }
        let trimmed = value.trimmingCharacters(in: .whitespacesAndNewlines)
        return trimmed.isEmpty ? nil : trimmed
    }

    static func newer(_ candidate: String, _ current: String) -> Bool {
        let left = candidate.split(separator: ".").map { Int($0) ?? 0 }
        let right = current.split(separator: ".").map { Int($0) ?? 0 }
        for index in 0..<max(left.count, right.count) {
            let a = index < left.count ? left[index] : 0
            let b = index < right.count ? right[index] : 0
            if a != b { return a > b }
        }
        return false
    }

    func alert(_ title: String, _ message: String) {
        let box = NSAlert()
        box.messageText = title
        box.informativeText = message
        box.addButton(withTitle: "OK")
        box.runModal()
    }

    func check(silent: Bool) async {
        guard !busy else { return }
        busy = true
        defer { busy = false }
        do {
            var request = URLRequest(url: manifestURL)
            request.cachePolicy = .reloadIgnoringLocalCacheData
            request.timeoutInterval = 30
            let (data, _) = try await URLSession.shared.data(for: request)
            let fetched = try JSONDecoder().decode(Manifest.self, from: data)
            manifest = fetched
            let local = Self.readInstalled(gameDir)
            let gameNewer = local == nil || Self.newer(fetched.game.version, local!)
            let launcherNewer = Self.newer(fetched.launcher.version, launcherVersion)
            if gameNewer || launcherNewer {
                banner = gameNewer
                    ? "Update available: game \(fetched.game.version) — click to review"
                    : "Launcher update \(fetched.launcher.version) available — click to review"
                updateTitle = local.map { "v\($0) → v\(fetched.game.version)" } ?? "Install v\(fetched.game.version)"
                notes = fetched.notes ?? "No release notes were attached to this package."
                if !silent { showUpdate = true }
                status = gameNewer ? "Game update \(fetched.game.version) available." : "A newer launcher is available."
            } else {
                banner = nil
                status = "Everything is up to date."
                if !silent { alert("Up to date", "The game and launcher are up to date.") }
            }
        } catch {
            status = "Could not check for updates."
            if !silent { alert("Update check failed", error.localizedDescription) }
        }
    }

    func install() async {
        guard !busy, let fetched = manifest else { return }
        busy = true
        progress = 0
        defer { busy = false }
        do {
            status = "Downloading game \(fetched.game.version)…"
            guard let url = URL(string: fetched.game.url) else { throw NSError(domain: "Vanguard", code: 1, userInfo: [NSLocalizedDescriptionKey: "The game URL is invalid."]) }
            let (temporary, _) = try await URLSession.shared.download(from: url)
            let data = try Data(contentsOf: temporary)
            let digest = SHA256.hash(data: data).map { String(format: "%02x", $0) }.joined()
            guard digest.caseInsensitiveCompare(fetched.game.sha256) == .orderedSame else {
                throw NSError(domain: "Vanguard", code: 2, userInfo: [NSLocalizedDescriptionKey: "The downloaded package failed its hash check."])
            }
            status = "Unpacking game \(fetched.game.version)…"
            let staging = support.appendingPathComponent("staging", isDirectory: true)
            try? FileManager.default.removeItem(at: staging)
            try FileManager.default.createDirectory(at: staging, withIntermediateDirectories: true)
            try unzip(temporary, to: staging)
            let app = try findApp(in: staging)
            let old = support.appendingPathComponent("game.old", isDirectory: true)
            try? FileManager.default.removeItem(at: old)
            try FileManager.default.createDirectory(at: support, withIntermediateDirectories: true)
            if FileManager.default.fileExists(atPath: gameDir.path) {
                try FileManager.default.moveItem(at: gameDir, to: old)
            }
            try FileManager.default.createDirectory(at: gameDir, withIntermediateDirectories: true)
            try FileManager.default.moveItem(at: app, to: gameApp)
            try fetched.game.version.write(to: gameDir.appendingPathComponent("version.txt"), atomically: true, encoding: .utf8)
            try? FileManager.default.removeItem(at: staging)
            try? FileManager.default.removeItem(at: old)
            installed = fetched.game.version
            status = "Game \(fetched.game.version) installed."
            showUpdate = false
            if Self.newer(fetched.launcher.version, launcherVersion) {
                status = "Game installed. A newer launcher is available; open the downloaded DMG."
                if let launcherURL = URL(string: fetched.launcher.url) {
                    NSWorkspace.shared.open(launcherURL)
                }
            }
        } catch {
            status = "Update failed."
            alert("Update failed", error.localizedDescription)
        }
    }

    private func unzip(_ archive: URL, to directory: URL) throws {
        let process = Process()
        process.executableURL = URL(fileURLWithPath: "/usr/bin/unzip")
        process.arguments = ["-q", "-o", archive.path, "-d", directory.path]
        try process.run()
        process.waitUntilExit()
        guard process.terminationStatus == 0 else {
            throw NSError(domain: "Vanguard", code: 3, userInfo: [NSLocalizedDescriptionKey: "The game package could not be unpacked."])
        }
    }

    private func findApp(in directory: URL) throws -> URL {
        let contents = try FileManager.default.contentsOfDirectory(at: directory, includingPropertiesForKeys: nil)
        if let app = contents.first(where: { $0.pathExtension == "app" }) { return app }
        for entry in contents {
            if let nested = try? FileManager.default.contentsOfDirectory(at: entry, includingPropertiesForKeys: nil),
               let app = nested.first(where: { $0.pathExtension == "app" }) {
                return app
            }
        }
        throw NSError(domain: "Vanguard", code: 4, userInfo: [NSLocalizedDescriptionKey: "The package has no .app inside."])
    }

    func play() {
        guard FileManager.default.fileExists(atPath: gameApp.path) else {
            Task { await check(silent: false) }
            return
        }
        if zeroHour.isEmpty || !FileManager.default.fileExists(atPath: zeroHour + "/INIZH.big") {
            chooseZeroHour()
        }
        guard !zeroHour.isEmpty, FileManager.default.fileExists(atPath: zeroHour + "/INIZH.big") else {
            alert("Zero Hour not found", "Choose the Zero Hour folder that holds INIZH.big.")
            return
        }
        let executable = gameApp.appendingPathComponent("Contents/MacOS/generals")
        guard FileManager.default.fileExists(atPath: executable.path) else {
            alert("Game missing", "The game executable is missing; run Check for updates.")
            return
        }
        let process = Process()
        process.executableURL = executable
        process.arguments = ["-root", zeroHour]
        process.currentDirectoryURL = gameApp
        do {
            try process.run()
            status = "Game started."
        } catch {
            alert("Could not start the game", error.localizedDescription)
        }
    }

    func chooseZeroHour() {
        let panel = NSOpenPanel()
        panel.canChooseDirectories = true
        panel.canChooseFiles = false
        panel.allowsMultipleSelection = false
        panel.message = "Pick the Zero Hour folder that contains INIZH.big."
        panel.prompt = "Use this folder"
        if panel.runModal() == .OK, let url = panel.url {
            if FileManager.default.fileExists(atPath: url.path + "/INIZH.big") {
                zeroHour = url.path
                UserDefaults.standard.set(zeroHour, forKey: "ZeroHourPath")
                status = "Zero Hour folder set."
            } else {
                alert("Not a Zero Hour folder", "That folder has no INIZH.big.")
            }
        }
    }

    func openGameFolder() {
        NSWorkspace.shared.open(FileManager.default.fileExists(atPath: gameDir.path) ? gameDir : support)
    }
}

struct FilledButtonStyle: ButtonStyle {
    let background: Color
    let foreground: Color
    func makeBody(configuration: Configuration) -> some View {
        configuration.label
            .font(.system(size: 14, weight: .bold))
            .frame(maxWidth: .infinity, minHeight: 46)
            .background(background.opacity(configuration.isPressed ? 0.75 : 1))
            .foregroundColor(foreground)
            .cornerRadius(4)
    }
}

struct VersionBlock: View {
    let caption: String
    let value: String
    var body: some View {
        VStack(alignment: .leading, spacing: 4) {
            Text(caption)
                .font(.system(size: 10, weight: .bold))
                .foregroundColor(Color(red: 0.56, green: 0.64, blue: 0.75))
            Text(value)
                .font(.system(size: 26, weight: .bold))
                .foregroundColor(.white)
        }
    }
}

struct SideView: View {
    @ObservedObject var model: LauncherModel
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("INSTALLATION")
                .font(.system(size: 11, weight: .bold))
                .foregroundColor(Color(red: 0.56, green: 0.64, blue: 0.75))
            Text(model.installed == nil ? "Not installed" : "Ready to play")
                .font(.system(size: 28, weight: .bold))
                .foregroundColor(.white)
                .padding(.top, 4)
            HStack(alignment: .top, spacing: 70) {
                VersionBlock(caption: "GAME", value: model.installed ?? "—")
                VersionBlock(caption: "LAUNCHER", value: model.launcherVersion)
            }
            .padding(.top, 22)

            Button("Play") { model.play() }
                .buttonStyle(FilledButtonStyle(background: Color(red: 0.95, green: 0.79, blue: 0.30),
                                               foreground: Color(red: 0.06, green: 0.07, blue: 0.09)))
                .padding(.top, 26)

            Button("Check for updates") { Task { await model.check(silent: false) } }
                .buttonStyle(FilledButtonStyle(background: Color(red: 0.08, green: 0.11, blue: 0.16),
                                               foreground: .white))
                .padding(.top, 12)

            HStack(spacing: 18) {
                Button("Set Zero Hour folder") { model.chooseZeroHour() }
                    .buttonStyle(.plain)
                    .foregroundColor(Color(red: 0.56, green: 0.64, blue: 0.75))
                Button("Open game folder") { model.openGameFolder() }
                    .buttonStyle(.plain)
                    .foregroundColor(Color(red: 0.56, green: 0.64, blue: 0.75))
            }
            .font(.system(size: 12))
            .padding(.top, 16)

            Text(model.status)
                .font(.system(size: 12))
                .foregroundColor(Color(red: 0.56, green: 0.64, blue: 0.75))
                .padding(.top, 26)
                .frame(maxWidth: .infinity, alignment: .leading)

            Text("Zero Hour: \(model.zeroHour.isEmpty ? "not found" : model.zeroHour)")
                .font(.system(size: 11))
                .foregroundColor(model.zeroHour.isEmpty ? Color(red: 0.75, green: 0.25, blue: 0.28)
                                                         : Color(red: 0.45, green: 0.53, blue: 0.64))
                .lineLimit(1)
                .truncationMode(.middle)
                .padding(.top, 12)
                .frame(maxWidth: .infinity, alignment: .leading)

            Spacer()
            Text("Generals Vanguard for Command & Conquer: Generals Zero Hour")
                .font(.system(size: 10))
                .foregroundColor(Color(red: 0.33, green: 0.40, blue: 0.50))
        }
        .padding(.horizontal, 28)
        .padding(.top, 52)
        .padding(.bottom, 24)
        .frame(width: 400, height: 640, alignment: .topLeading)
        .background(Color(red: 0.04, green: 0.06, blue: 0.10))
    }
}

struct UpdateView: View {
    @ObservedObject var model: LauncherModel
    var body: some View {
        VStack(alignment: .leading, spacing: 0) {
            Text("REVIEW / UPDATE")
                .font(.system(size: 11, weight: .bold))
                .foregroundColor(Color(red: 0.56, green: 0.64, blue: 0.75))
            Text(model.updateTitle)
                .font(.system(size: 28, weight: .bold))
                .foregroundColor(.white)
                .padding(.top, 6)
            ScrollView {
                Text(model.notes)
                    .font(.system(size: 13))
                    .foregroundColor(Color(red: 0.78, green: 0.82, blue: 0.89))
                    .frame(maxWidth: .infinity, alignment: .leading)
                    .padding(16)
            }
            .frame(height: 380)
            .background(Color(red: 0.05, green: 0.07, blue: 0.11))
            .cornerRadius(4)
            .padding(.top, 20)

            if model.busy {
                ProgressView()
                    .padding(.top, 14)
            }
            Text(model.status)
                .font(.system(size: 12))
                .foregroundColor(Color(red: 0.56, green: 0.64, blue: 0.75))
                .padding(.top, 10)

            HStack(spacing: 12) {
                Button("Update") { Task { await model.install() } }
                    .buttonStyle(FilledButtonStyle(background: Color(red: 0.95, green: 0.79, blue: 0.30),
                                                   foreground: Color(red: 0.06, green: 0.07, blue: 0.09)))
                    .frame(width: 170)
                    .disabled(model.busy)
                Button("Cancel") { model.showUpdate = false }
                    .buttonStyle(FilledButtonStyle(background: Color(red: 0.08, green: 0.11, blue: 0.16),
                                                   foreground: .white))
                    .frame(width: 130)
                    .disabled(model.busy)
            }
            .padding(.top, 16)
        }
        .padding(40)
        .frame(width: 1000, height: 640, alignment: .topLeading)
        .background(Color(red: 0.03, green: 0.04, blue: 0.07))
    }
}

struct ContentView: View {
    @ObservedObject var model: LauncherModel
    var body: some View {
        ZStack(alignment: .topLeading) {
            HStack(spacing: 0) {
                Group {
                    if let hero = model.hero {
                        Image(nsImage: hero).resizable().scaledToFill()
                    } else {
                        Color(red: 0.04, green: 0.06, blue: 0.10)
                    }
                }
                .frame(width: 600, height: 640)
                .clipped()
                SideView(model: model)
            }
            if let banner = model.banner {
                Button { model.showUpdate = true } label: {
                    Text(banner)
                        .font(.system(size: 12, weight: .bold))
                        .foregroundColor(Color(red: 0.95, green: 0.79, blue: 0.30))
                        .frame(maxWidth: .infinity, minHeight: 32, alignment: .leading)
                        .padding(.leading, 18)
                        .background(Color(red: 0.22, green: 0.17, blue: 0.06))
                }
                .buttonStyle(.plain)
            }
            if model.showUpdate {
                UpdateView(model: model)
            }
        }
        .frame(width: 1000, height: 640)
    }
}

final class AppDelegate: NSObject, NSApplicationDelegate {
    func applicationDidFinishLaunching(_ notification: Notification) {
        NSApp.appearance = NSAppearance(named: .darkAqua)
    }
}

@main
struct VanguardLauncherApp: App {
    @NSApplicationDelegateAdaptor(AppDelegate.self) var appDelegate
    @StateObject private var model = LauncherModel()
    var body: some Scene {
        WindowGroup("Generals Vanguard") {
            ContentView(model: model)
                .frame(width: 1000, height: 640)
        }
        .windowResizability(.contentSize)
        .commands {
            CommandGroup(replacing: .newItem) {}
        }
    }
}
