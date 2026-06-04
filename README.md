# 🏃‍♂️ Subway Surfers PC Native Controls Patcher

Welcome! This repository hosts a native controls patcher for the PC version of Subway Surfers. It injects keyboard input handling, restores missions, ensures null-safe daily words, and patches run sequences directly into the game's assembly using `Mono.Cecil`.

---

## 🎨 Project Vibe & Scope

> [!NOTE]
> **Vibe Coding in Progress** ☕✨
> 
> This is a project built and maintained purely in **vibe-coding mode**! I'm currently hanging out, fixing bugs as they pop up, and polishing the gameplay experience on PC. 
> 
> If you find issues or have ideas to make the patch even better, **suggestions and contributions are absolutely welcome!** Feel free to open an Issue, submit a Pull Request, or drop a suggestion.

---

## 🚀 Features

* **⌨️ Native Keyboard Controls**: Play Subway Surfers on PC seamlessly with standard keyboard inputs.
* **🔧 Core Fixes**:
  * Decouples the tilt/Y-value checks to optimize PC inputs.
  * Patches `Missions` and `UIMissionHelper` to run smoothly without crash states.
  * Standardizes fallback daily words in `DailyWord.Start()`.
  * Redirects death sequences and resets run states safely.
* **🔓 Character Unlock Utility**: Save-data injection support to unlock all characters, coins, hoverboards, upgrades, and headstarts.
* **🐧 Linux AppImage & Native Packaging**: Scripts to package the game as a single portable `.AppImage` application for Linux desktops.

---

## 🎮 Controls

Use these keys to navigate through the game:

| Action | Key / Input |
| :--- | :--- |
| **Jump** | ⬆️ Up Arrow / W |
| **Roll** | ⬇️ Down Arrow / S |
| **Move Left** | ⬅️ Left Arrow / A |
| **Move Right** | ➡️ Right Arrow / D |
| **Launch Hoverboard** | ⎵ Spacebar |
| **Quit Game** | ⎋ Escape (ESC) |

---

## ⚙️ Installation & Usage

### 🪟 Windows
1. **Run the Patcher**: Execute `Patcher.exe` to modify the game assembly files (if you are compiling from source).
2. **Launch Subway Surfers**: Double-click `Subway Surfers.exe` to play the patched version with native keyboard controls.
3. **Exit Hook**: When done playing, make sure to exit the controls helper icon from your system tray or press `Ctrl + Shift + E`.

### 🐧 Linux
This is a patched Windows version of Subway Surfers. To run it on Linux, you must have Wine installed, including 32-bit library support (since the game is a 32-bit Windows application).

#### Prerequisites: Wine 32-bit Setup
Select the commands for your Linux distribution to set up Wine and its 32-bit dependencies:

* **Ubuntu / Debian / Linux Mint:**
  ```bash
  sudo dpkg --add-architecture i386
  sudo apt update
  sudo apt install wine wine32
  ```

* **Fedora:**
  ```bash
  sudo dnf install wine wine.i686
  ```

* **Arch Linux:**
  1. Open `/etc/pacman.conf` in a text editor with root privileges.
  2. Uncomment the `[multilib]` section:
     ```ini
     [multilib]
     Include = /etc/pacman.d/mirrorlist
     ```
  3. Update your system database and install Wine:
     ```bash
     sudo pacman -Syu wine
     ```

#### Running the game using AppImage:
If you have a compiled `.AppImage` bundle:
1. Make sure Wine 32-bit is installed using the instructions above.
2. Right-click the `.AppImage` file -> **Properties** -> **Permissions** -> Check **Allow executing file as program** (or run `chmod +x SubwaySurfers-x86_64.AppImage`).
3. Double-click the AppImage to launch the game natively on your Linux desktop.

#### Packaging the AppImage from source:
1. Navigate to the `AppImage` directory:
   ```bash
   cd AppImage
   ```
2. Make `build-appimage.sh` executable and run it to prepare the build structure:
   ```bash
   chmod +x build-appimage.sh
   ./build-appimage.sh
   ```
3. Use the standard Linux `appimagetool` to bundle it:
   ```bash
   appimagetool SubwaySurfers.AppDir
   ```

---

## 🔓 How to Unlock All Characters & Max Stats

> [!WARNING]
> This step overrides your current local save game. Backup your existing saves if you wish to preserve them!

### Windows
1. Go to your local game data folder (replace `YOURUSERNAME` with your actual Windows username):
   `C:\Users\YOURUSERNAME\AppData\LocalLow\Kiloo Games\`
   * *Note: `AppData` is hidden by default. Enable "Show hidden files, folders, and drives" in your File Explorer folder options.*
2. Back up the existing `Subway Surf` directory.
3. Copy the contents of the `Unlock All Characters` directory from this repository and replace the `Subway Surf` folder in your `LocalLow\Kiloo Games` path.

### Linux (AppImage / Wine)
1. Navigate to your Wine prefix's local AppData folder (usually located at `~/.wine/drive_c/users/YOURUSERNAME/AppData/LocalLow/Kiloo Games/` or under Bottles' user directory).
2. Back up and replace the `Subway Surf` directory with the one from `Unlock All Characters`.

---

## 🤝 Contributing

Contributions are what make the open-source community an amazing place to learn, inspire, and create.
1. **Fork** the Project.
2. Create your **Feature Branch** (`git checkout -b feature/AmazingFeature`).
3. **Commit** your Changes (`git commit -m 'Add some AmazingFeature'`).
4. **Push** to the Branch (`git push origin feature/AmazingFeature`).
5. Open a **Pull Request**.

All feedback, bug reports, and pull requests are highly appreciated!

## Buy me a coffee
if you want to Buy me a coffee : [buymeacoffee](https://tinyurl.com/utbunyw8)
