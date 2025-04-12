#!/bin/bash

echo "1. Client"
echo "2. Server"
echo "3. All"

read -n 1 -p "Please select an option:" choice
echo ""
echo ""
script_dir=$(cd $(dirname $0) && pwd)
case $choice in
    1)
        dotnet $script_dir/Fantasy.Tools.NetworkProtocol.dll --p 1 --f $script_dir
        ;;
    2)
        dotnet $script_dir/Fantasy.Tools.NetworkProtocol.dll --p 2 --f $script_dir
        ;;
    3)
        dotnet $script_dir/Fantasy.Tools.NetworkProtocol.dll --p 3 --f $script_dir
        ;;
    *)
        echo "Invalid option"
        ;;
esac
