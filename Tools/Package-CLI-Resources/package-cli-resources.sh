#!/bin/bash

# Fantasy CLI Resources Packaging Script (Unix/macOS/Linux)
# This script automatically packages resources for Fantasy.Cil based on PackageConfig.json

set -e  # Exit on error

# Colors for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Get script directory and project root
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"
CONFIG_FILE="$SCRIPT_DIR/PackageConfig.json"

echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Fantasy CLI Resources Packaging Script${NC}"
echo -e "${BLUE}========================================${NC}"
echo ""

# Check if jq is installed
if ! command -v jq &> /dev/null; then
    echo -e "${RED}Error: 'jq' is not installed.${NC}"
    echo -e "${YELLOW}Please install jq to parse JSON:${NC}"
    echo "  macOS:  brew install jq"
    echo "  Ubuntu: sudo apt-get install jq"
    echo "  CentOS: sudo yum install jq"
    exit 1
fi

# Check if config file exists
if [ ! -f "$CONFIG_FILE" ]; then
    echo -e "${RED}Error: Configuration file not found at $CONFIG_FILE${NC}"
    exit 1
fi

# Read configuration
OUTPUT_DIR=$(jq -r '.outputDirectory' "$CONFIG_FILE")
PACKAGE_COUNT=$(jq '.packages | length' "$CONFIG_FILE")

echo -e "${BLUE}Project Root:${NC} $PROJECT_ROOT"
echo -e "${BLUE}Output Directory:${NC} $OUTPUT_DIR"
echo -e "${BLUE}Packages to process:${NC} $PACKAGE_COUNT"
echo ""

# Create output directory if it doesn't exist
FULL_OUTPUT_DIR="$PROJECT_ROOT/$OUTPUT_DIR"
mkdir -p "$FULL_OUTPUT_DIR"

# Process each package
SUCCESS_COUNT=0
FAILED_COUNT=0

for ((i=0; i<$PACKAGE_COUNT; i++)); do
    NAME=$(jq -r ".packages[$i].name" "$CONFIG_FILE")
    SOURCE_DIR=$(jq -r ".packages[$i].sourceDirectory" "$CONFIG_FILE")
    OUTPUT_FILE=$(jq -r ".packages[$i].outputFileName" "$CONFIG_FILE")

    echo -e "${YELLOW}[$((i+1))/$PACKAGE_COUNT] Processing: $NAME${NC}"

    FULL_SOURCE_DIR="$PROJECT_ROOT/$SOURCE_DIR"
    FULL_OUTPUT_PATH="$FULL_OUTPUT_DIR/$OUTPUT_FILE"

    # Check if source directory exists
    if [ ! -d "$FULL_SOURCE_DIR" ]; then
        echo -e "${RED}  ✗ Source directory not found: $SOURCE_DIR${NC}"
        FAILED_COUNT=$((FAILED_COUNT + 1))
        echo ""
        continue
    fi

    # Read exclude patterns
    EXCLUDE_COUNT=$(jq ".packages[$i].excludePatterns | length" "$CONFIG_FILE")
    EXCLUDE_ARGS=""

    for ((j=0; j<$EXCLUDE_COUNT; j++)); do
        PATTERN=$(jq -r ".packages[$i].excludePatterns[$j]" "$CONFIG_FILE")
        # Convert glob patterns to zip exclude format
        # Remove leading **/ for zip compatibility
        PATTERN_CLEAN=$(echo "$PATTERN" | sed 's|^\*\*/||')
        EXCLUDE_ARGS="$EXCLUDE_ARGS -x \"*/$PATTERN_CLEAN\""
    done

    # Remove old zip file if exists
    [ -f "$FULL_OUTPUT_PATH" ] && rm "$FULL_OUTPUT_PATH"

    # Create zip archive
    echo -e "${BLUE}  Source: $SOURCE_DIR${NC}"
    echo -e "${BLUE}  Output: $OUTPUT_DIR/$OUTPUT_FILE${NC}"

    cd "$FULL_SOURCE_DIR"

    # Build zip command with exclusions
    if [ $EXCLUDE_COUNT -gt 0 ]; then
        eval "zip -r -q \"$FULL_OUTPUT_PATH\" . $EXCLUDE_ARGS"
    else
        zip -r -q "$FULL_OUTPUT_PATH" .
    fi

    if [ $? -eq 0 ]; then
        FILE_SIZE=$(du -h "$FULL_OUTPUT_PATH" | cut -f1)
        echo -e "${GREEN}  ✓ Success! Size: $FILE_SIZE${NC}"
        SUCCESS_COUNT=$((SUCCESS_COUNT + 1))
    else
        echo -e "${RED}  ✗ Failed to create zip${NC}"
        FAILED_COUNT=$((FAILED_COUNT + 1))
    fi

    cd "$PROJECT_ROOT"
    echo ""
done

# Summary
echo -e "${BLUE}========================================${NC}"
echo -e "${BLUE}Summary${NC}"
echo -e "${BLUE}========================================${NC}"
echo -e "${GREEN}Successful: $SUCCESS_COUNT${NC}"
if [ $FAILED_COUNT -gt 0 ]; then
    echo -e "${RED}Failed: $FAILED_COUNT${NC}"
fi
echo ""

if [ $FAILED_COUNT -eq 0 ]; then
    echo -e "${GREEN}All packages created successfully!${NC}"
    exit 0
else
    echo -e "${YELLOW}Some packages failed. Please check the errors above.${NC}"
    exit 1
fi
