#!/bin/bash
# Exit on error
set -e

echo "=================================================="
echo "🚀 Subway Surfers Linux AppImage Builder"
echo "=================================================="

# Wine configuration
WINE_URL="https://github.com/Kron4ek/Wine-Builds/releases/download/10.0/wine-10.0-amd64-wow64.tar.xz"
WINE_TARBALL="wine-10.0-amd64-wow64.tar.xz"

# Download portable Wine if not present
if [ ! -f "$WINE_TARBALL" ]; then
    echo "📥 Downloading portable Wine 10.0 WoW64..."
    wget -O "$WINE_TARBALL" "$WINE_URL" || curl -L -o "$WINE_TARBALL" "$WINE_URL"
else
    echo "📦 Using cached Wine 10.0 WoW64 tarball."
fi

# Directories
BUILD_DIR="SubwaySurfers.AppDir"
# Clean previous build dir to avoid mixing old libraries
rm -rf "$BUILD_DIR"
mkdir -p "$BUILD_DIR/usr/bin"
mkdir -p "$BUILD_DIR/usr/share/subwaysurfers"

# Copy Game Assets
echo "📦 Copying game assets..."
cp -r "../Subway Surfers.exe" "$BUILD_DIR/usr/share/subwaysurfers/"
cp -r "../Subway Surfers_Data" "$BUILD_DIR/usr/share/subwaysurfers/"
cp -r "../Unlock All Characters" "$BUILD_DIR/usr/share/subwaysurfers/"

# Extract Wine
echo "📦 Extracting Wine runtime to AppDir..."
tar -xf "$WINE_TARBALL" --strip-components=1 -C "$BUILD_DIR/usr/"

# Create the AppRun entrypoint
echo "🔧 Creating AppRun Entrypoint..."
cat << 'EOF' > "$BUILD_DIR/AppRun"
#!/bin/bash
HERE="$(dirname "$(readlink -f "${0}")")"

# Configure local Wine environment
export PATH="$HERE/usr/bin:$PATH"
export LD_LIBRARY_PATH="$HERE/usr/lib:$HERE/usr/lib/wine:$LD_LIBRARY_PATH"
export WINEPREFIX="$HOME/.local/share/subwaysurfers/wineprefix"
export WINEDEBUG=-all

# Ensure the prefix directory exists
mkdir -p "$WINEPREFIX"

# Execute using local Wine
wine "$HERE/usr/share/subwaysurfers/Subway Surfers.exe" "$@"
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
echo "  ARCH=x86_64 appimagetool $BUILD_DIR"
echo "=================================================="
