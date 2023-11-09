#!/bin/bash

echo "Please select an option:"
echo "1. Client"
echo "2. Server"
echo "3. All"

read -p "Please select an option:" choice

case $choice in
    1)
        echo "Client"
        dotnet Exporter.dll --ExportPlatform 1
        ;;
    2)
        echo "Server"
        dotnet Exporter.dll --ExportPlatform 2
        ;;
    3)
        echo "All"
        dotnet Exporter.dll --ExportPlatform 3
        ;;
    *)
        echo "Invalid option"
        ;;
esac
