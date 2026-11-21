# OrbKit Quest Completer

<p align="center">
  <img width="1151" height="840" alt="OrbKitMain" src="https://github.com/user-attachments/assets/0b5ee485-334a-4c34-b431-b2fd099f3efe" />
</p>

**An app for completing Discord game and video quests without token extraction, memory patching, or permanent Discord client modifications.**

*Developed by [Void Tech Studios](https://voidtechstudios.us/)*

---

## Features

### Manual Game Runner (Lowest Risk)
- Fetches Discord's official detectable games catalog (`https://discord.com/api/v9/applications/detectable`) and caches it locally for 12 hours.
- Resolves Win32 executable paths via `ExePicker` and allows selecting a specific binary when a game registers multiple executables.
- Copies a lightweight dummy host (`STUB\StubHost.exe`) to the exact registered relative game path under `%LocalAppData%\OrbKit\Staging` so Discord detects a real Windows executable and generates a valid `executable_fingerprint`.
- Configurable quest duration (1 to 120 minutes) with a randomized overshoot tail (0 to 15 minutes) so sessions do not terminate on the exact quest timer.
- Kills the dummy process tree and deletes the staged game executable automatically when a session finishes or is stopped.

### Optional CDP Mode (Chrome DevTools Protocol)
- Disabled on startup by default (`Auto-CDP` is off in safe mode). Nothing runs `127.0.0.1:9222` unless you enable `Auto-CDP` or click **Connect CDP**.
- Includes a **Relaunch Discord** button that restarts Discord (`Discord`, `DiscordCanary`, or `DiscordPTB` via `Update.exe`) with `--remote-debugging-port=9222` for that session only. No shortcut editing is required; closing and reopening Discord normally closes the debug port.
- Syncs active eligible quests and your current Discord Orb balance.
- Automatically disables quests if `questAccessSuspendedUntil` or `questEnrollmentBlockedUntil` is detected on the account.
- Runs video quests through Discord's own Chromium stack with configurable min/max heartbeat intervals (default `7.0s` to `9.5s` to match Discord's valid video player heartbeat), an auto-enroll toggle, and a reset to defaults button.

### Hot-Swappable CDP Patch Scripts
- CDP store lookups and API calls are decoupled into a JavaScript patch adapter (`Scripts\cdp_patch.js`) so Discord client updates can be patched without recompiling the executable, and to manually patch yourself instead of waiting for an OrbKit update (Advanced users only).
- Includes **Import .js**, **Export**, **Folder**, and **Reset** controls in the **Video Quests** tab.
- Every imported or manually placed script passes a three-layer validation and security pipeline before activation:
  1. **Static Security and Syntax Scan (`CdpScriptValidator`)**: Enforces a 128 KB size limit, required `__DQC_ADAPTER` methods (`fetchSnapshot`, `enrollQuest`, `sendVideoProgress`), and balanced delimiters. Blocks `fetch()`, `XMLHttpRequest`, `WebSocket`, `sendBeacon`, `EventSource`, absolute `http(s)://` and `ws(s)://` URLs, `getToken`, `localStorage`, `sessionStorage`, `document.cookie`, `Authorization`, webhooks, `oauth2/authorize`, `discordsays`, billing/payment paths, `eval()`, `Function()`, `atob()`, `fromCharCode`, dynamic `import()`, `document.createElement`, `DiscordNative`, `child_process`, and `require()`.
  2. **Zero-Execution V8 Compile Check**: When CDP is connected, Discord's V8 engine compiles imported scripts via `Runtime.compileScript` without executing them to catch syntax errors prior to saving.
  3. **Hash Confirmation and Safe Fallback**: Displays the script version and full SHA-256 hash before import. If a manually edited `Scripts\cdp_patch.js` fails validation at runtime, it is rejected and the app falls back to the built-in script.

### Intentionally Omitted
- Achievement OAuth forgery, console/activity spoofing (`PLAY_ON_XBOX`, `PLAY_ON_PLAYSTATION`, `PLAY_ACTIVITY`), and auto-claiming rewards are intentionally excluded to protect account standing. Claim completed quest rewards manually inside Discord.

---

## Requirements

- Windows 10 or Windows 11 (x64)
- .NET 10 Desktop Runtime (or .NET 10 SDK to build from source)
- Discord desktop client (`Discord`, `DiscordCanary`, or `DiscordPTB`)
- `STUB\StubHost.exe` located next to the main executable (OrbKit will automatically create the directory and file on launch if it is missing)

> Note: The Manual Game Runner works on all Discord desktop builds without CDP.

---

## Usage

### Running a Game Quest (Manual Game Runner)
1. Open **OrbKit Quest Completer** (defaults to the **Manual Game Runner** tab).
2. Search for the game by name or App ID in the **Detectable Games Catalog**.
3. Select the game. If multiple game executables exist, pick the desired executable under **Target Executable**.
4. Adjust **Quest Duration** and **Random Overshoot Max** on the right rail.
5. Accept the quest in Discord if you have not already. Click **START GAME SESSION**, and wait for the session to complete.
6. Claim your reward manually in Discord.

### Running Video Quests or Syncing Live Quests (CDP Mode)
1. Click **Relaunch Discord** in the top bar and wait about 5 seconds for Discord to finish loading.
2. Click **Connect CDP**. The `CDP` status bar will switch to `Connected` and your `ORBS` balance will populate.
3. Switch to the **Video Quests** tab to view active eligible quests.
4. Select a quest, adjust **Min/Max Ping Interval** or **Auto-enroll before watching** if needed, and click **RUN SELECTED QUEST**.
5. Claim your reward manually in Discord once finished.

---

## Building from Source

1. Open the solution in Visual Studio (with the .NET Desktop Development workload and .NET 10 SDK installed).
2. Build and run the `OrbKit_Quest_Completer` project.

---

## Editing the UI in Visual Studio Designer

The main form uses stacked full-docked panels inside `contentHost` (`pnlManual`, `pnlQuests`, `pnlInfo`) and stacked right-rail control groups inside `pnlControlsHost` (`grpControls`, `grpVideoControls`). Because inactive tabs have `Visible = false`, `FDSidebarItem` includes design-time tab switching so you do not have to manually toggle panel visibility or edit `Main.Designer.cs` by hand.

### How `FDSidebarItem` Links Panels
Each navigation button in the top header (`navManual`, `navQuests`, `navInfo`) exposes two linking properties under **Behavior Quest**:
- **`LinkedPanel`**: The primary content panel shown inside `contentHost`.
- **`LinkedSecondaryPanel`**: An optional secondary panel or group box (such as the matching right-rail controls inside `pnlControlsHost`).

### Switching Tabs Inside the Designer
1. Build the solution once so the WinForms Designer loads the compiled `ObsidianBlurpleTheme` controls.
2. Open `Main.cs [Design]`.
3. Click the tab button you want to edit in the top header (`navManual`, `navQuests`, or `navInfo`).
4. In the **Properties** window, double-click **`Selected`** to set it to `True`.
5. The setter automatically hides the sibling tabs linked panels, shows the selected tab's `LinkedPanel` and `LinkedSecondaryPanel`, and brings both to the front for direct editing.

> **Startup Tab Safety:** You can save `Main.cs [Design]` while viewing any tab. `Form1_Load` in `Main.cs` explicitly sets `navManual.Selected = true` and calls `SyncRightRailPanel()` on launch so the application always opens on **Manual Game Runner**.

For moving controls between overlapping containers or inspecting hidden elements, keep **View > Other Windows > Document Outline**, or use the hotkey (`Ctrl+Alt+T`) open alongside the Designer.

---

## Local Data and Configuration

App files are stored in `%LocalAppData%\OrbKit\`:
- `settings.json`: Persists settings like `AutoConnectCdp`, `VideoPaceMinSec`, `VideoPaceMaxSec`, and `AutoEnrollVideo`.
- `Cache\detectable_games.json`: Cached copy of Discord's detectable games list (refreshes every 12 hours or via **Refresh List**).
- `Staging\`: Temporary directory where `StubHost.exe` is copied during active game quest sessions and cleaned up afterward.

Custom CDP Patch scripts are stored and loaded in `<AppFolder>\Scripts\cdp_patch.js`.

---

## Custom CDP Patch Scripts

If a Discord client update changes an internal store name or API payload:
1. Click **Export** on the **Video Quests** tab to save a copy of the active `cdp_patch.js`.
2. Edit the exported script to update the store lookup or request payload while keeping the `globalThis.__DQC_ADAPTER` object and its required methods (`version`, `fetchSnapshot`, `enrollQuest`, `sendVideoProgress`).
3. Click **Import .js** to run the static security scan, V8 compile check, and SHA-256 confirmation, then activate it.
4. Click **Reset** at any time to remove `Scripts\cdp_patch.js` and revert to the built-in script.

---

## Links

- Void Tech Studios Website: [voidtechstudios.us](https://voidtechstudios.us/)
- Donate to help keep the lights on: [Buy Me a Coffee](https://buymeacoffee.com/rustforgedev)

---

## Disclaimer

OrbKit Quest Completer is an independent project by Void Tech Studios and is not affiliated with, endorsed by, or connected to Discord Inc.

This software is provided "AS IS", without warranty of any kind, express or implied. By downloading or using this application, you agree that you are doing so entirely at your own risk. Void Tech Studios is not responsible or liable for any account warnings, quest suspensions, bans, or other actions taken against your Discord account.
