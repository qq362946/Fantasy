#!/bin/bash
# Fantasy Protocol Editor - Cross-Platform Builder

set -e

APP_NAME="Fantasy Protocol Editor"
PROJECT_DIR="$(cd "$(dirname "$0")" && pwd)"

# 支持的平台和架构
MACOS_RIDS=("osx-arm64" "osx-x64")
MACOS_NAMES=("Apple Silicon (ARM64)" "Intel (x64)")

WINDOWS_RIDS=("win-x64" "win-arm64")
WINDOWS_NAMES=("x64" "ARM64")

echo "=========================================="
echo "Fantasy Protocol Editor - Cross-Platform Builder"
echo "=========================================="
echo ""
echo "Building for:"
echo "  macOS:"
echo "    - Apple Silicon (ARM64)"
echo "    - Intel (x64)"
echo "  Windows:"
echo "    - x64"
echo "    - ARM64"
echo ""

# 清理旧的输出目录
rm -rf "$PROJECT_DIR/bin/Release/osx-app"
rm -rf "$PROJECT_DIR/bin/Release/win-app"

##############################################
# 构建 macOS 应用程序
##############################################
echo ""
echo "=========================================="
echo "Building macOS Applications"
echo "=========================================="

APP_DIR="$PROJECT_DIR/bin/Release/osx-app"
mkdir -p "$APP_DIR"

for i in "${!MACOS_RIDS[@]}"; do
    RID="${MACOS_RIDS[$i]}"
    ARCH_NAME="${MACOS_NAMES[$i]}"

    echo ""
    echo "=========================================="
    echo "Building for: macOS - $ARCH_NAME"
    echo "=========================================="

    BUNDLE_NAME="Fantasy Protocol Editor ($ARCH_NAME).app"
    BUILD_DIR="$PROJECT_DIR/bin/Release/net8.0/$RID/publish"

    # 1. 发布应用程序
    echo ""
    echo "[1/4] Publishing .NET application for $RID..."
    dotnet publish "$PROJECT_DIR/Fantasy.ProtocolEditor.csproj" \
        -c Release \
        -r "$RID" \
        --self-contained false \
        -p:PublishSingleFile=false

    # 2. 创建 .app bundle 结构
    echo ""
    echo "[2/4] Creating .app bundle structure..."
    mkdir -p "$APP_DIR/$BUNDLE_NAME/Contents/MacOS"
    mkdir -p "$APP_DIR/$BUNDLE_NAME/Contents/Resources"

    # 3. 复制文件
    echo ""
    echo "[3/4] Copying application files..."
    cp -r "$BUILD_DIR"/* "$APP_DIR/$BUNDLE_NAME/Contents/MacOS/"
    cp "$PROJECT_DIR/Assets/app-icon.icns" "$APP_DIR/$BUNDLE_NAME/Contents/Resources/"

    # 4. 创建 Info.plist
    echo ""
    echo "[4/4] Creating Info.plist..."
    cat > "$APP_DIR/$BUNDLE_NAME/Contents/Info.plist" << EOF
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>CFBundleDevelopmentRegion</key>
    <string>en</string>
    <key>CFBundleDisplayName</key>
    <string>$APP_NAME</string>
    <key>CFBundleExecutable</key>
    <string>Fantasy.ProtocolEditor</string>
    <key>CFBundleIconFile</key>
    <string>app-icon.icns</string>
    <key>CFBundleIdentifier</key>
    <string>com.fantasy.protocoleditor</string>
    <key>CFBundleInfoDictionaryVersion</key>
    <string>6.0</string>
    <key>CFBundleName</key>
    <string>$APP_NAME</string>
    <key>CFBundlePackageType</key>
    <string>APPL</string>
    <key>CFBundleShortVersionString</key>
    <string>1.0.0</string>
    <key>CFBundleVersion</key>
    <string>1</string>
    <key>LSMinimumSystemVersion</key>
    <string>10.15</string>
    <key>NSHighResolutionCapable</key>
    <true/>
    <key>NSPrincipalClass</key>
    <string>NSApplication</string>
</dict>
</plist>
EOF

    # 设置可执行权限
    chmod +x "$APP_DIR/$BUNDLE_NAME/Contents/MacOS/Fantasy.ProtocolEditor"

    echo ""
    echo "✓ Build completed for macOS - $ARCH_NAME!"
    echo ""
done

##############################################
# 构建 Windows 应用程序
##############################################
echo ""
echo "=========================================="
echo "Building Windows Applications"
echo "=========================================="

WIN_APP_DIR="$PROJECT_DIR/bin/Release/win-app"
mkdir -p "$WIN_APP_DIR"

for i in "${!WINDOWS_RIDS[@]}"; do
    RID="${WINDOWS_RIDS[$i]}"
    ARCH_NAME="${WINDOWS_NAMES[$i]}"

    echo ""
    echo "=========================================="
    echo "Building for: Windows - $ARCH_NAME"
    echo "=========================================="

    OUTPUT_DIR="$WIN_APP_DIR/Fantasy Protocol Editor (Windows $ARCH_NAME)"
    BUILD_DIR="$PROJECT_DIR/bin/Release/net8.0/$RID/publish"

    # 1. 发布应用程序
    echo ""
    echo "[1/2] Publishing .NET application for $RID..."
    dotnet publish "$PROJECT_DIR/Fantasy.ProtocolEditor.csproj" \
        -c Release \
        -r "$RID" \
        --self-contained false \
        -p:PublishSingleFile=false

    # 2. 复制文件到输出目录
    echo ""
    echo "[2/2] Copying application files..."
    mkdir -p "$OUTPUT_DIR"
    cp -r "$BUILD_DIR"/* "$OUTPUT_DIR/"

    echo ""
    echo "✓ Build completed for Windows - $ARCH_NAME!"
    echo ""
done

##############################################
# 总结
##############################################
echo ""
echo "=========================================="
echo "✓ All builds completed successfully!"
echo "=========================================="
echo ""
echo "macOS Application bundles:"
echo ""
for i in "${!MACOS_RIDS[@]}"; do
    ARCH_NAME="${MACOS_NAMES[$i]}"
    BUNDLE_NAME="Fantasy Protocol Editor ($ARCH_NAME).app"
    echo "  [$ARCH_NAME]"
    echo "    Location: $APP_DIR/$BUNDLE_NAME"
    echo "    Run: open \"$APP_DIR/$BUNDLE_NAME\""
    echo ""
done

echo "Windows Applications:"
echo ""
for i in "${!WINDOWS_RIDS[@]}"; do
    ARCH_NAME="${WINDOWS_NAMES[$i]}"
    OUTPUT_DIR="Fantasy Protocol Editor (Windows $ARCH_NAME)"
    echo "  [$ARCH_NAME]"
    echo "    Location: $WIN_APP_DIR/$OUTPUT_DIR"
    echo "    Executable: Fantasy.ProtocolEditor.exe"
    echo ""
done

echo "=========================================="
echo "Requirements & Installation Instructions"
echo "=========================================="
echo ""
echo "Requirements:"
echo "  - .NET 8.0 Runtime or SDK must be installed"
echo "  - Download from: https://dotnet.microsoft.com/download/dotnet/8.0"
echo ""
echo "Installation Instructions:"
echo ""
echo "macOS:"
echo "  # For Apple Silicon Mac:"
echo "  cp -r \"$APP_DIR/Fantasy Protocol Editor (Apple Silicon (ARM64)).app\" /Applications/"
echo ""
echo "  # For Intel Mac:"
echo "  cp -r \"$APP_DIR/Fantasy Protocol Editor (Intel (x64)).app\" /Applications/"
echo ""
echo "Windows:"
echo "  # Copy the entire folder to desired location, then run Fantasy.ProtocolEditor.exe"
echo "  # For x64:"
echo "  # $WIN_APP_DIR/Fantasy Protocol Editor (Windows x64)/"
echo ""
echo "  # For ARM64:"
echo "  # $WIN_APP_DIR/Fantasy Protocol Editor (Windows ARM64)/"
echo ""
