#!/bin/bash

echo "1. Client Increment"
echo "2. Client all"
echo "3. Server Increment"
echo "4. Server all"
echo "5. Client and Server Increment"
echo "6. Client and Server all"

read -n 1 -p "Please select an option:" choice
echo ""
case $choice in
    1)
        dotnet Fantasy.Tools.ConfigTable.dll --p 1 --e 1
        ;;
    2)
        dotnet Fantasy.Tools.ConfigTable.dll --p 1 --e 2
        ;;
    3)
        dotnet Fantasy.Tools.ConfigTable.dll --p 2 --e 1
        ;;
    4)
        dotnet Fantasy.Tools.ConfigTable.dll --p 2 --e 2
        ;;
    5)
        dotnet Fantasy.Tools.ConfigTable.dll --p 3 --e 1
        ;;
    6)
        dotnet Fantasy.Tools.ConfigTable.dll --p 3 --e 2
        ;;
    *)
        echo "Invalid option"
        ;;
esac
