#!/bin/bash

echo "1. Client"
echo "2. Server"
echo "3. All"

read -n 1 -p "Please select an option:" choice
echo ""
echo ""
case $choice in
    1)
        dotnet Exporter.dll --ExportPlatform 1
        ;;
    2)
        dotnet Exporter.dll --ExportPlatform 2
        ;;
    3)
        dotnet Exporter.dll --ExportPlatform 3
        ;;
    *)
        echo "Invalid option"
        ;;
esac
