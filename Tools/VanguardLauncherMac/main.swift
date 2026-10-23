// Generals Vanguard launcher for macOS.
// Copyright (C) 2026 KuiseRapper
// SPDX-License-Identifier: GPL-3.0-or-later

import SwiftUI
import AppKit
import Foundation
import CryptoKit
import WebKit

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

struct FeaturedMod: Identifiable {
    let id: String
    let title: String
    let detail: String
    let page: URL
    let archiveHints: [String]
    let startID: String
    let downloadURL: URL?
    let includes: [String]
    var bundled: Bool = false
}

let FeaturedMods: [FeaturedMod] = [
    FeaturedMod(id: "shockwave", title: "ShockWave 1.201",
                detail: "USA, China and GLA get the mod's units and structures. Downloads from ModDB automatically.",
                page: URL(string: "https://www.moddb.com/mods/cc-shockwave/downloads/shockwave-version-12")!,
                archiveHints: ["shw_noinstl"], startID: "31249",
                downloadURL: URL(string: "https://github.com/KuiseRapper/generals-vanguard/releases/download/v0.5.11/ShockWave-1.1-noinstaller.zip")!,
                includes: ["ShockWave's USA, China and GLA: extra generals, units, structures, defences and support powers",
                           "ShockWave's UI, cameos and balance",
                           "Its challenge maps and skirmish AI"]),
    FeaturedMod(id: "rotr", title: "Rise of the Reds 1.8",
                detail: "The Russian Federation and the ECA. Downloads from ModDB automatically.",
                page: URL(string: "https://www.moddb.com/mods/rise-of-the-reds/downloads/rotr-eca-beta-18-no-installer-mac-compatible")!,
                archiveHints: ["rotrbeta_18_noinstl"], startID: "49529",
                downloadURL: URL(string: "https://github.com/KuiseRapper/generals-vanguard/releases/download/v0.5.11/RiseOfTheReds-1.8-noinstaller.zip")!,
                includes: ["Rise of the Reds' Russian Federation and European Continental Alliance factions",
                           "Their buildings, vehicles, aircraft, infantry, powers and cameos",
                           "RotR's reworked USA, China and GLA (not used by the combined mod)",
                           "Its campaign missions (separate pack, later)"]),
    FeaturedMod(id: "vanguardcombined", title: "Vanguard Combined 0.1",
                detail: "Our own merge: ShockWave as the base, with Rise of the Reds' Russia and ECA on top. In development.",
                page: URL(string: "https://github.com/KuiseRapper/generals-vanguard")!,
                archiveHints: ["vanguardcombined"], startID: "",
                downloadURL: nil,
                includes: ["ShockWave 1.201 as the base: USA, China and GLA, their generals, units, structures, defences, powers, UI and balance",
                           "Rise of the Reds' Russian Federation and ECA as two more armies, with their buildings, vehicles, aircraft, infantry, powers and cameos",
                           "Vanguard's fixes, AI work, performance and balance underneath both",
                           "RotR's campaign missions as a separate pack once the factions are stable",
                           "Credits: SWR Productions for ShockWave and Rise of the Reds; Vanguard for the merge"],
                bundled: true),
]

struct ModEntry: Identifiable {
    let id: String
    let name: String
    let version: String
    let url: URL
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
    @Published var mods: [ModEntry] = []
    @Published var activeMod: String? = UserDefaults.standard.string(forKey: "ActiveMod")
    @Published var showMods = false
    @Published var detailMod: FeaturedMod?
    @Published var modsPending: [String: Bool] = [:]
    @Published var modsBusy = false
    @Published var modsProgress = 0.0
    @Published var downloading: Set<String> = []
    @Published var downloadProgress: [String: Double] = [:]
    private var downloaders: [String: ModDownloader] = [:]

    var modsDir: URL {
        let documents = FileManager.default.urls(for: .documentDirectory, in: .userDomainMask)[0]
            .appendingPathComponent("Generals Vanguard", isDirectory: true)
            .appendingPathComponent("Mods", isDirectory: true)
        if (try? FileManager.default.createDirectory(at: documents, withIntermediateDirectories: true)) != nil {
            return documents
        }
        return support.appendingPathComponent("Mods", isDirectory: true)
    }

    let launcherVersion = Bundle.main.object(forInfoDictionaryKey: "CFBundleShortVersionString") as? String ?? "0.4.0"
    // "latest" points at whatever release is newest, so a release that publishes launcher-mac.json
    // is picked up without pinning a tag. The older builds pinned v0.4.0 and never saw an update.
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
        refreshMods()
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
            if Self.newer(fetched.launcher.version, launcherVersion), let launcherURL = URL(string: fetched.launcher.url) {
                status = "Installing launcher \(fetched.launcher.version)…"
                let (launcherFile, _) = try await URLSession.shared.download(from: launcherURL)
                replaceLauncher(with: launcherFile)
            }
        } catch {
            status = "Update failed."
            alert("Update failed", error.localizedDescription)
        }
    }

    /// Replace the running .app with the copy in the downloaded DMG and start it again, so an
    /// update does not ask the player to drag anything out of a disk image.
    private func replaceLauncher(with dmg: URL) {
        let current = Bundle.main.bundlePath
        let script = """
        #!/bin/sh
        sleep 2
        WORK="$(mktemp -d /tmp/vanguard-launcher.XXXXXX)"
        if [ "\(dmg.pathExtension)" = "zip" ]; then
            unzip -q -o "\(dmg.path)" -d "$WORK" || exit 1
            rm -rf "\(current)"
            ditto "$WORK/VanguardLauncher.app" "\(current)"
        else
            hdiutil attach -nobrowse -quiet "\(dmg.path)" -mountpoint "$WORK" || exit 1
            rm -rf "\(current)"
            ditto "$WORK/VanguardLauncher.app" "\(current)"
            hdiutil detach "$WORK" -quiet
        fi
        rm -f "\(dmg.path)"
        open "\(current)"
        """
        let path = FileManager.default.temporaryDirectory.appendingPathComponent("vanguard-update.sh")
        try? script.write(to: path, atomically: true, encoding: .utf8)
        try? FileManager.default.setAttributes([.posixPermissions: 0o755], ofItemAtPath: path.path)
        let process = Process()
        process.executableURL = URL(fileURLWithPath: "/bin/sh")
        process.arguments = ["-c", "\(path.path) > /tmp/vanguard-update.log 2>&1 &"]
        try? process.run()
        DispatchQueue.main.asyncAfter(deadline: .now() + 0.5) { NSApp.terminate(nil) }
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
        if !inizhContains(zeroHour, "Weather.ini") {
            let box = NSAlert()
            box.messageText = "Zero Hour data incomplete"
            box.informativeText = "This Zero Hour folder's INIZH.big has no Data/INI/Default/Weather.ini. "
                + "Install or verify the Zero Hour expansion in Steam/EA (not just base Generals), "
                + "then choose the folder again.\n\nPlay anyway?"
            box.addButton(withTitle: "Play anyway")
            box.addButton(withTitle: "Cancel")
            if box.runModal() != .alertFirstButtonReturn { return }
        }
        moveDuplicateInizh(zeroHour)
        let executable = gameApp.appendingPathComponent("Contents/MacOS/generals")
        guard FileManager.default.fileExists(atPath: executable.path) else {
            alert("Game missing", "The game executable is missing; run Check for updates.")
            return
        }
        let process = Process()
        process.executableURL = executable
        var arguments = ["-root", zeroHour]
        // The ticked mod wins; with a single mod installed and none ticked, mount that one, so an
        // install always plays without a second step.
        var modID = activeMod
        if modID == nil, mods.count == 1 { modID = mods[0].id }
        if let id = modID, let mod = mods.first(where: { $0.id == id }) {
            arguments += ["-mod", mod.url.path + "/"]
            status = "Game started with \(mod.name)."
        }
        process.arguments = arguments
        let log = "launch \(Date()): \(arguments.joined(separator: " "))\n"
        if let data = log.data(using: .utf8) {
            let path = support.appendingPathComponent("launch.log")
            if let handle = try? FileHandle(forWritingTo: path) {
                handle.seekToEndOfFile(); handle.write(data); try? handle.close()
            } else {
                try? data.write(to: path)
            }
        }
        process.currentDirectoryURL = gameApp
        do {
            try process.run()
            status = "Game started."
        } catch {
            alert("Could not start the game", error.localizedDescription)
        }
    }

    // MARK: - mods

    func refreshMods() {
        try? FileManager.default.createDirectory(at: modsDir, withIntermediateDirectories: true)
        let keys: Set<URLResourceKey> = [.isDirectoryKey]
        let items = (try? FileManager.default.contentsOfDirectory(at: modsDir, includingPropertiesForKeys: Array(keys))) ?? []
        mods = items.compactMap { url in
            guard (try? url.resourceValues(forKeys: keys))?.isDirectory == true else { return nil }
            // A folder left by a failed install (an installer .exe, no .big) is not a mod.
            let inside = (try? FileManager.default.contentsOfDirectory(at: url, includingPropertiesForKeys: nil)) ?? []
            if !inside.contains(where: { $0.pathExtension.lowercased() == "big" }) {
                try? FileManager.default.removeItem(at: url)
                return nil
            }
            var name = url.lastPathComponent
            var version = "1.0"
            if let data = try? Data(contentsOf: url.appendingPathComponent("mod.json")),
               let obj = try? JSONSerialization.jsonObject(with: data) as? [String: Any] {
                name = obj["name"] as? String ?? name
                version = obj["version"] as? String ?? version
            }
            return ModEntry(id: url.lastPathComponent, name: name, version: version, url: url)
        }.sorted { $0.name.localizedCaseInsensitiveCompare($1.name) == .orderedAscending }
        if let active = activeMod, !mods.contains(where: { $0.id == active }) { setActiveMod(nil) }
        for featured in FeaturedMods {
            modsPending[featured.id] = mods.contains(where: { $0.id == featured.id })
        }
    }

    func setActiveMod(_ id: String?) {
        activeMod = id
        if let id { UserDefaults.standard.set(id, forKey: "ActiveMod") }
        else { UserDefaults.standard.removeObject(forKey: "ActiveMod") }
    }

    func installMod() {
        let panel = NSOpenPanel()
        panel.allowsMultipleSelection = false
        panel.canChooseFiles = true
        panel.canChooseDirectories = false
        panel.message = "Choose a mod archive (.zip) or a .big file"
        guard panel.runModal() == .OK, let url = panel.url else { return }
        let base = url.deletingPathExtension().lastPathComponent
        do {
            try installArchive(url, as: base)
            refreshMods()
            status = "Installed \(base)."
        } catch {
            alert("Could not install the mod", error.localizedDescription)
        }
    }

    func isInstalled(_ featured: FeaturedMod) -> Bool {
        guard let mod = mods.first(where: { $0.id == featured.id }) else { return false }
        let files = (try? FileManager.default.contentsOfDirectory(at: mod.url, includingPropertiesForKeys: nil)) ?? []
        return files.contains { $0.pathExtension.lowercased() == "big" }
    }

    func archiveInDownloads(_ featured: FeaturedMod) -> URL? {
        let downloads = FileManager.default.homeDirectoryForCurrentUser.appendingPathComponent("Downloads")
        let keys: Set<URLResourceKey> = [.contentModificationDateKey]
        let files = (try? FileManager.default.contentsOfDirectory(at: downloads, includingPropertiesForKeys: Array(keys))) ?? []
        let hints = featured.archiveHints.map { $0.lowercased() }
        return files
            .filter { $0.pathExtension.lowercased() == "zip" }
            .filter { file in hints.contains { file.lastPathComponent.lowercased().contains($0) } }
            .sorted { a, b in
                let da = (try? a.resourceValues(forKeys: keys))?.contentModificationDate ?? .distantPast
                let db = (try? b.resourceValues(forKeys: keys))?.contentModificationDate ?? .distantPast
                return da > db
            }
            .first
    }

    /// Download straight from ModDB in a hidden WebKit view, which passes the site's browser check,
    /// and install the archive when it lands. Falls back to the page only if the download fails.
    func downloadFeatured(_ featured: FeaturedMod) {
        guard let downloadURL = featured.downloadURL else { return }
        guard !downloading.contains(featured.id) else { return }
        downloading.insert(featured.id)
        downloadProgress[featured.id] = 0
        status = "Downloading \(featured.title)…"
        let downloader = ModDownloader(
            url: downloadURL,
            onProgress: { [weak self] value in
                Task { @MainActor in self?.downloadProgress[featured.id] = value }
            },
            onFinish: { [weak self] result in
                Task { @MainActor in
                    guard let self else { return }
                    self.downloading.remove(featured.id)
                    self.downloaders[featured.id] = nil
                    switch result {
                    case .success(let file):
                        do {
                            try self.installArchive(file, as: featured.id)
                            self.refreshMods()
                            self.setActiveMod(featured.id)
                            self.status = "Installed \(featured.title); it loads on Play."
                        } catch {
                            self.alert("Could not install the mod", error.localizedDescription)
                        }
                    case .failure(let error):
                        self.alert("Download failed", error.localizedDescription)
                    }
                }
            })
        downloaders[featured.id] = downloader
        downloader.startDownload()
    }

    func installFeaturedNow(_ featured: FeaturedMod) {
        do {
            try installFeatured(featured)
            refreshMods()
            setActiveMod(featured.id)
            status = "Installed \(featured.title); it loads on Play."
        } catch {
            alert("Could not install the mod", error.localizedDescription)
        }
    }

    func remove(_ featured: FeaturedMod) {
        guard let installed = mods.first(where: { $0.id == featured.id }) else { return }
        try? FileManager.default.removeItem(at: installed.url)
        if activeMod == featured.id { setActiveMod(nil) }
        modsPending[featured.id] = false
        refreshMods()
        status = "Removed \(featured.title)."
    }

    func installFeatured(_ featured: FeaturedMod) throws {
        let downloads = FileManager.default.homeDirectoryForCurrentUser.appendingPathComponent("Downloads")
        let keys: Set<URLResourceKey> = [.contentModificationDateKey]
        let files = (try? FileManager.default.contentsOfDirectory(at: downloads, includingPropertiesForKeys: Array(keys))) ?? []
        let hints = featured.archiveHints.map { $0.lowercased() }
        let archive = files
            .filter { $0.pathExtension.lowercased() == "zip" }
            .filter { file in hints.contains { file.lastPathComponent.lowercased().contains($0) } }
            .sorted { a, b in
                let da = (try? a.resourceValues(forKeys: keys))?.contentModificationDate ?? .distantPast
                let db = (try? b.resourceValues(forKeys: keys))?.contentModificationDate ?? .distantPast
                return da > db
            }
            .first
        guard let archive else {
            NSWorkspace.shared.open(featured.page)
            throw NSError(domain: "Vanguard", code: 10, userInfo: [NSLocalizedDescriptionKey:
                "No \(featured.title) archive in Downloads. The ModDB page is open; download the no-installer zip there, then press Update mods again."])
        }
        try installArchive(archive, as: featured.id)
    }

    func applyMods() {
        guard !modsBusy else { return }
        modsBusy = true
        modsProgress = 0
        let desired = modsPending
        Task {
            var failures: [String] = []
            for (index, featured) in FeaturedMods.enumerated() {
                let want = desired[featured.id] ?? false
                let installed = mods.first(where: { $0.id == featured.id })
                do {
                    if want, installed == nil { try installFeatured(featured) }
                    if !want, let installed { try FileManager.default.removeItem(at: installed.url) }
                } catch {
                    failures.append(featured.title)
                }
                modsProgress = Double(index + 1) / Double(FeaturedMods.count)
                refreshMods()
            }
            modsBusy = false
            status = failures.isEmpty ? "Mods updated." : "Could not update: \(failures.joined(separator: ", "))."
        }
    }

    private func installArchive(_ url: URL, as name: String) throws {
        let safe = name.replacingOccurrences(of: "/", with: "-").replacingOccurrences(of: ":", with: "-")
        let folder = modsDir.appendingPathComponent(safe, isDirectory: true)
        do {
            try FileManager.default.createDirectory(at: folder, withIntermediateDirectories: true)
            if url.pathExtension.lowercased() == "big" {
                try FileManager.default.copyItem(at: url, to: folder.appendingPathComponent(url.lastPathComponent))
            } else {
                try unzip(url, to: folder)
                let top = (try? FileManager.default.contentsOfDirectory(at: folder, includingPropertiesForKeys: nil)) ?? []
                if top.count == 1, (try? top[0].resourceValues(forKeys: [.isDirectoryKey]))?.isDirectory == true {
                    let inner = top[0]
                    for item in (try? FileManager.default.contentsOfDirectory(at: inner, includingPropertiesForKeys: nil)) ?? [] {
                        try FileManager.default.moveItem(at: item, to: folder.appendingPathComponent(item.lastPathComponent))
                    }
                    try FileManager.default.removeItem(at: inner)
                }
            }
            let json = folder.appendingPathComponent("mod.json")
            if !FileManager.default.fileExists(atPath: json.path) {
                let data = try JSONSerialization.data(withJSONObject: ["name": name, "version": "1.0"], options: [.prettyPrinted])
                try data.write(to: json)
            }
            let contents = (try? FileManager.default.contentsOfDirectory(at: folder, includingPropertiesForKeys: nil)) ?? []
            guard contents.contains(where: { $0.pathExtension.lowercased() == "big" }) else {
                try? FileManager.default.removeItem(at: folder)
                throw NSError(domain: "Vanguard", code: 11, userInfo: [NSLocalizedDescriptionKey:
                    "That archive is an installer, not a mod package. Use the no-installer release (the one with .big files)."])
            }
        } catch {
            try? FileManager.default.removeItem(at: folder)
            throw error
        }
    }

    func uninstallMod(_ entry: ModEntry) {
        let box = NSAlert()
        box.messageText = "Uninstall \(entry.name)?"
        box.informativeText = "This deletes \(entry.url.path)."
        box.addButton(withTitle: "Uninstall")
        box.addButton(withTitle: "Cancel")
        guard box.runModal() == .alertFirstButtonReturn else { return }
        try? FileManager.default.removeItem(at: entry.url)
        if activeMod == entry.id { setActiveMod(nil) }
        refreshMods()
        status = "Uninstalled \(entry.name)."
    }

    private func moveDuplicateInizh(_ zero: String) {
        let path = zero + "/Data/INI/INIZH.big"
        guard FileManager.default.fileExists(atPath: path) else { return }
        var backup = path + ".vanguard-backup"
        var index = 1
        while FileManager.default.fileExists(atPath: backup) {
            backup = path + ".vanguard-backup" + String(index)
            index += 1
        }
        try? FileManager.default.moveItem(atPath: path, toPath: backup)
    }

    private func inizhContains(_ zero: String, _ name: String) -> Bool {
        let path = zero + "/INIZH.big"
        guard let handle = FileHandle(forReadingAtPath: path) else { return false }
        defer { try? handle.close() }
        let needle = Data(name.utf8)
        var carry = Data()
        while true {
            let chunk = handle.readData(ofLength: 1 << 20)
            if chunk.isEmpty { return false }
            var combined = carry
            combined.append(chunk)
            if combined.range(of: needle) != nil { return true }
            carry = combined.suffix(needle.count - 1)
        }
    }

    private func containsReforgedGameData(_ zero: String) -> Bool {
        let path = zero + "/INIZH.big"
        guard let handle = FileHandle(forReadingAtPath: path) else { return false }
        defer { try? handle.close() }
        let needle = Data("UserDataLeafName".utf8)
        var carry = Data()
        while true {
            let chunk = handle.readData(ofLength: 1 << 20)
            if chunk.isEmpty { return false }
            var combined = carry
            combined.append(chunk)
            if combined.range(of: needle) != nil { return true }
            carry = combined.suffix(needle.count - 1)
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

            Button("Mods") { model.refreshMods(); model.showMods = true }
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
        .sheet(isPresented: $model.showMods) { ModsView(model: model) }
    }
}

/// Downloads a mod package with URLSession and installs it. The archives are hosted on the
/// project's 0.5.11 release because ModDB sits behind a Cloudflare challenge that a plain HTTP
/// client cannot pass; the launcher's Download button is a real one-click install from there.
final class ModDownloader: NSObject, URLSessionDownloadDelegate {
    private let url: URL
    private let onProgress: (Double) -> Void
    private let onFinish: (Result<URL, Error>) -> Void
    private var session: URLSession?
    private var finished = false

    init(url: URL, onProgress: @escaping (Double) -> Void,
         onFinish: @escaping (Result<URL, Error>) -> Void) {
        self.url = url
        self.onProgress = onProgress
        self.onFinish = onFinish
        super.init()
    }

    func startDownload() {
        session = URLSession(configuration: .default, delegate: self, delegateQueue: nil)
        session?.downloadTask(with: url).resume()
    }

    func urlSession(_ session: URLSession, downloadTask: URLSessionDownloadTask, didWriteData bytesWritten: Int64,
                    totalBytesWritten: Int64, totalBytesExpectedToWrite: Int64) {
        guard totalBytesExpectedToWrite > 0 else { return }
        onProgress(Double(totalBytesWritten) / Double(totalBytesExpectedToWrite))
    }

    func urlSession(_ session: URLSession, downloadTask: URLSessionDownloadTask, didFinishDownloadingTo location: URL) {
        guard !finished else { return }
        finished = true
        let status = (downloadTask.response as? HTTPURLResponse)?.statusCode ?? 0
        guard status == 200 else {
            onFinish(.failure(NSError(domain: "Vanguard", code: status, userInfo: [NSLocalizedDescriptionKey:
                "The download server returned HTTP \(status)."])))
            return
        }
        let name = downloadTask.response?.suggestedFilename ?? "mod.zip"
        let destination = FileManager.default.temporaryDirectory.appendingPathComponent(name)
        try? FileManager.default.removeItem(at: destination)
        do {
            try FileManager.default.moveItem(at: location, to: destination)
            onFinish(.success(destination))
        } catch {
            onFinish(.failure(error))
        }
    }

    func urlSession(_ session: URLSession, task: URLSessionTask, didCompleteWithError error: Error?) {
        guard !finished, let error else { return }
        finished = true
        onFinish(.failure(error))
    }
}

struct ModRow: View {
    @ObservedObject var model: LauncherModel
    let featured: FeaturedMod
    var installed: Bool { model.isInstalled(featured) }
    var downloading: Bool { model.downloading.contains(featured.id) }
    var hasArchive: Bool { model.archiveInDownloads(featured) != nil }
    var body: some View {
        HStack(alignment: .top, spacing: 12) {
            Toggle("", isOn: Binding(
                get: { model.activeMod == featured.id },
                set: { on in
                    if on { model.setActiveMod(featured.id) }
                    else if model.activeMod == featured.id { model.setActiveMod(nil) }
                }))
                .toggleStyle(.checkbox)
                .labelsHidden()
                .disabled(!installed)
                .padding(.top, 2)
            VStack(alignment: .leading, spacing: 3) {
                HStack(spacing: 8) {
                    Text(featured.title).font(.system(size: 13, weight: .semibold)).foregroundColor(.white)
                    Text(featured.bundled ? "IN DEVELOPMENT" : (model.activeMod == featured.id ? "ACTIVE" : (installed ? "INSTALLED" : (hasArchive ? "READY TO INSTALL" : "NOT INSTALLED"))))
                        .font(.system(size: 9, weight: .bold))
                        .foregroundColor(featured.bundled ? Color(red: 0.55, green: 0.70, blue: 0.95) : model.activeMod == featured.id ? Color(red: 0.95, green: 0.79, blue: 0.30) : installed ? Color(red: 0.45, green: 0.82, blue: 0.55)
                                                   : (hasArchive ? Color(red: 0.95, green: 0.79, blue: 0.30)
                                                                 : Color(red: 0.55, green: 0.62, blue: 0.72)))
                        .padding(.horizontal, 6).padding(.vertical, 2)
                        .background(Color.white.opacity(0.06), in: Capsule())
                }
                Text(featured.detail)
                    .font(.system(size: 10))
                    .foregroundColor(Color(red: 0.45, green: 0.53, blue: 0.64))
                    .fixedSize(horizontal: false, vertical: true)
            }
            Spacer(minLength: 8)
            Button { model.detailMod = featured } label: {
                Image(systemName: "info.circle")
            }
            .buttonStyle(.plain)
            .foregroundColor(Color(red: 0.56, green: 0.64, blue: 0.75))
            .help("What is in this mod?")
            if featured.bundled {
                Text("Ships with Vanguard").font(.system(size: 10)).foregroundColor(Color(red: 0.45, green: 0.53, blue: 0.64))
            } else if downloading {
                ProgressView(value: model.downloadProgress[featured.id] ?? 0).frame(width: 90)
            } else if installed {
                Button("Remove") { model.remove(featured) }.buttonStyle(.bordered)
            } else if hasArchive {
                Button("Install") { model.installFeaturedNow(featured) }
                    .buttonStyle(.borderedProminent)
            } else {
                Button("Download") { model.downloadFeatured(featured) }.buttonStyle(.borderedProminent)
            }
        }
        .padding(12)
        .background(Color.white.opacity(0.04), in: RoundedRectangle(cornerRadius: 8))
    }
}

struct ModsView: View {
    @ObservedObject var model: LauncherModel
    private var custom: [ModEntry] { model.mods.filter { mod in !FeaturedMods.contains(where: { $0.id == mod.id }) } }
    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            HStack {
                Text("Mods").font(.system(size: 22, weight: .bold)).foregroundColor(.white)
                Spacer()
                Button { model.showMods = false } label: {
                    Image(systemName: "xmark.circle.fill")
                        .font(.system(size: 20))
                        .foregroundColor(Color(red: 0.45, green: 0.53, blue: 0.64))
                }
                .buttonStyle(.plain)
                .keyboardShortcut(.cancelAction)
            }
            Text("Tick a mod to install and mount it, untick to remove it. Downloads go straight into the Mods folder.")
                .font(.system(size: 11))
                .foregroundColor(Color(red: 0.56, green: 0.64, blue: 0.75))
            HStack(spacing: 8) {
                Text(model.modsDir.path)
                    .font(.system(size: 9, design: .monospaced))
                    .foregroundColor(Color(red: 0.45, green: 0.53, blue: 0.64))
                    .lineLimit(1).truncationMode(.middle)
                Button("Open folder") { NSWorkspace.shared.open(model.modsDir) }
                    .buttonStyle(.plain)
                    .font(.system(size: 10))
                    .foregroundColor(Color(red: 0.95, green: 0.79, blue: 0.30))
            }
            ScrollView {
                VStack(spacing: 8) {
                    ForEach(FeaturedMods) { featured in
                        ModRow(model: model, featured: featured)
                    }
                    if !custom.isEmpty {
                        Text("OTHER MODS")
                            .font(.system(size: 10, weight: .bold))
                            .foregroundColor(Color(red: 0.45, green: 0.53, blue: 0.64))
                            .frame(maxWidth: .infinity, alignment: .leading)
                            .padding(.top, 6)
                        ForEach(custom) { mod in
                            HStack {
                                VStack(alignment: .leading, spacing: 2) {
                                    Text(mod.name).foregroundColor(.white)
                                    Text("Installed · v\(mod.version) · \(mod.id)")
                                        .font(.system(size: 10))
                                        .foregroundColor(Color(red: 0.45, green: 0.53, blue: 0.64))
                                }
                                Spacer()
                                Button("Remove") { model.uninstallMod(mod) }.buttonStyle(.bordered)
                            }
                            .padding(12)
                            .background(Color.white.opacity(0.04), in: RoundedRectangle(cornerRadius: 8))
                        }
                    }
                }
            }
            Text("The ticked mod is the one Play loads. Download installs it; Remove deletes its files.")
                .font(.system(size: 10))
                .foregroundColor(Color(red: 0.45, green: 0.53, blue: 0.64))
            HStack(spacing: 10) {
                Button("Install from file\u{2026}") { model.installMod() }
                    .buttonStyle(.bordered)
                Spacer()
                Button("Close") { model.showMods = false }
                    .buttonStyle(.borderedProminent)
                    .keyboardShortcut(.defaultAction)
            }
        }
        .padding(24)
        .frame(width: 660, height: 560)
        .background(Color(red: 0.05, green: 0.07, blue: 0.11))
        .sheet(item: $model.detailMod) { featured in
            ModDetailView(model: model, featured: featured)
        }
    }
}

struct ModDetailView: View {
    @ObservedObject var model: LauncherModel
    let featured: FeaturedMod
    private var installed: Bool { model.isInstalled(featured) }
    var body: some View {
        VStack(alignment: .leading, spacing: 14) {
            HStack(alignment: .firstTextBaseline) {
                Text(featured.title).font(.system(size: 22, weight: .bold)).foregroundColor(.white)
                Spacer()
                if featured.bundled {
                    Text("IN DEVELOPMENT").font(.system(size: 10, weight: .bold))
                        .foregroundColor(Color(red: 0.55, green: 0.70, blue: 0.95))
                } else {
                    Text(installed ? "INSTALLED" : "NOT INSTALLED").font(.system(size: 10, weight: .bold))
                        .foregroundColor(installed ? Color(red: 0.45, green: 0.82, blue: 0.55) : Color(red: 0.55, green: 0.62, blue: 0.72))
                }
                Button { model.detailMod = nil } label: {
                    Image(systemName: "xmark.circle.fill").font(.system(size: 20))
                        .foregroundColor(Color(red: 0.45, green: 0.53, blue: 0.64))
                }
                .buttonStyle(.plain)
                .keyboardShortcut(.cancelAction)
            }
            Text(featured.detail)
                .font(.system(size: 12))
                .foregroundColor(Color(red: 0.56, green: 0.64, blue: 0.75))
            Text("WHAT IT INCLUDES")
                .font(.system(size: 10, weight: .bold))
                .foregroundColor(Color(red: 0.45, green: 0.53, blue: 0.64))
            VStack(alignment: .leading, spacing: 6) {
                ForEach(featured.includes, id: \.self) { line in
                    HStack(alignment: .top, spacing: 8) {
                        Image(systemName: "checkmark.circle.fill")
                            .font(.system(size: 11))
                            .foregroundColor(Color(red: 0.45, green: 0.82, blue: 0.55))
                        Text(line)
                            .font(.system(size: 12))
                            .foregroundColor(Color(red: 0.85, green: 0.89, blue: 0.95))
                            .fixedSize(horizontal: false, vertical: true)
                    }
                }
            }
            if !featured.bundled {
                Text("Installs into \(model.modsDir.path)/\(featured.id)")
                    .font(.system(size: 10, design: .monospaced))
                    .foregroundColor(Color(red: 0.45, green: 0.53, blue: 0.64))
                    .lineLimit(1).truncationMode(.middle)
            }
            Spacer(minLength: 4)
            HStack {
                if !featured.bundled {
                    if installed {
                        Button("Remove") { model.remove(featured); model.detailMod = nil }
                            .buttonStyle(.bordered)
                    } else if model.downloading.contains(featured.id) {
                        ProgressView(value: model.downloadProgress[featured.id] ?? 0).frame(width: 120)
                    } else {
                        Button("Download and install") { model.downloadFeatured(featured); model.detailMod = nil }
                            .buttonStyle(.borderedProminent)
                    }
                }
                Spacer()
                Button("Close") { model.detailMod = nil }
                    .buttonStyle(.bordered)
            }
        }
        .padding(24)
        .frame(width: 560, height: 430)
        .background(Color(red: 0.05, green: 0.07, blue: 0.11))
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
