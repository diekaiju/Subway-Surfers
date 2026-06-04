#!/bin/bash
# Exit on error
set -e

echo "=================================================="
echo "🚀 Subway Surfers Linux AppImage Builder"
echo "=================================================="

# Directories
BUILD_DIR="SubwaySurfers.AppDir"
mkdir -p "$BUILD_DIR/usr/bin"
mkdir -p "$BUILD_DIR/usr/share/subwaysurfers"

# Copy Game Assets
echo "📦 Copying game assets..."
cp -r "../Subway Surfers.exe" "$BUILD_DIR/usr/share/subwaysurfers/"
cp -r "../Subway Surfers_Data" "$BUILD_DIR/usr/share/subwaysurfers/"
cp -r "../Unlock All Characters" "$BUILD_DIR/usr/share/subwaysurfers/"

# Create the AppRun entrypoint
echo "🔧 Creating AppRun Entrypoint..."
cat << 'EOF' > "$BUILD_DIR/AppRun"
#!/bin/bash
HERE="$(dirname "$(readlink -f "${0}")")"

# Execute using host Wine environment
if ! command -v wine &> /dev/null; then
    echo "========================================================"
    echo "ERROR: Wine is required to run Subway Surfers AppImage."
    echo "This is a 32-bit Windows application and needs wine32."
    echo "========================================================"
    echo ""
    echo "How to install Wine on popular distributions:"
    echo "  Ubuntu/Debian/Mint:"
    echo "    sudo dpkg --add-architecture i386"
    echo "    sudo apt update"
    echo "    sudo apt install wine wine32"
    echo ""
    echo "  Fedora:"
    echo "    sudo dnf install wine wine.i686"
    echo ""
    echo "  Arch Linux:"
    echo "    (Enable multilib in /etc/pacman.conf first)"
    echo "    sudo pacman -Syu wine"
    echo ""
    exit 1
fi

wine "$HERE/usr/share/subwaysurfers/Subway Surfers.exe"
EOF
chmod +x "$BUILD_DIR/AppRun"

# Create Desktop Entry
echo "🖥️ Creating Desktop Entry..."
cat << EOF > "$BUILD_DIR/subwaysurfers.desktop"
[Desktop Entry]
Type=Application
Name=Subway Surfers (Native Controls)
Comment=Play Subway Surfers on Linux with keyboard controls.
Exec=AppRun
Icon=subwaysurfers
Categories=Game;ArcadeGame;
Terminal=false
EOF

# Copy Icon (using a default icon placeholder or actual image if provided)
echo "🎨 Setting up icon..."
# If an icon doesn't exist, we generate a basic one or use a placeholder
if [ -f "../Subway Surfers_Data/ScreenSelector.bmp" ]; then
    cp "../Subway Surfers_Data/ScreenSelector.bmp" "$BUILD_DIR/subwaysurfers.bmp"
    # Touch png to prevent appimagetool warning
    touch "$BUILD_DIR/subwaysurfers.png"
else
    touch "$BUILD_DIR/subwaysurfers.png"
fi

echo "=================================================="
echo "✅ Build prep complete! AppDir structured at: $BUILD_DIR"
echo "To package into a single .AppImage file, run:"
echo "  appimagetool $BUILD_DIR"
echo "=================================================="
