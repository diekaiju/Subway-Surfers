#!/bin/bash

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Check if wine is installed
if ! command -v wine &> /dev/null; then
    echo "================================================================="
    echo "ERROR: Wine is not installed."
    echo "This is a patched 32-bit Windows version of Subway Surfers."
    echo "To run it on Linux, you need Wine installed with 32-bit support."
    echo "================================================================="
    echo ""
    echo "How to install Wine on popular distributions:"
    echo "  Ubuntu/Debian/Mint:  sudo dpkg --add-architecture i386 && sudo apt update && sudo apt install wine wine32"
    echo "  Fedora:              sudo dnf install wine wine.i686"
    echo "  Arch Linux:          (Enable multilib in /etc/pacman.conf first)"
    echo "                       sudo pacman -Syu wine"
    echo ""
    read -p "Press Enter to exit..."
    exit 1
fi

echo "Launching Subway Surfers with Wine..."
wine "Subway Surfers.exe"
