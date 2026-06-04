#!/bin/bash

# Get the directory where the script is located
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
cd "$SCRIPT_DIR"

# Check if wine is installed
if ! command -v wine &> /dev/null; then
    echo "================================================================="
    echo "ERROR: Wine is not installed."
    echo "This is a patched Windows version of Subway Surfers."
    echo "To run it on Linux, you need Wine installed."
    echo "================================================================="
    echo ""
    echo "How to install Wine on popular distributions:"
    echo "  Ubuntu/Debian/Mint:  sudo apt update && sudo apt install wine"
    echo "  Fedora:              sudo dnf install wine"
    echo "  Arch Linux:          sudo pacman -S wine"
    echo ""
    read -p "Press Enter to exit..."
    exit 1
fi

echo "Launching Subway Surfers with Wine..."
wine "Subway Surfers.exe"
