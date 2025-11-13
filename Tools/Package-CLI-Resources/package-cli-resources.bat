@echo off
setlocal enabledelayedexpansion

REM Fantasy CLI Resources Packaging Script (Windows)
REM This script automatically packages resources for Fantasy.Cil based on PackageConfig.json

echo ========================================
echo Fantasy CLI Resources Packaging Script
echo ========================================
echo.

REM Get script directory and project root
set "SCRIPT_DIR=%~dp0"
set "PROJECT_ROOT=%SCRIPT_DIR%..\..\"
set "CONFIG_FILE=%SCRIPT_DIR%PackageConfig.json"

REM Check if PowerShell is available
where powershell >nul 2>nul
if %errorlevel% neq 0 (
    echo Error: PowerShell is not available.
    echo This script requires PowerShell to run.
    pause
    exit /b 1
)

REM Check if config file exists
if not exist "%CONFIG_FILE%" (
    echo Error: Configuration file not found at %CONFIG_FILE%
    pause
    exit /b 1
)

echo Project Root: %PROJECT_ROOT%
echo.

REM Use PowerShell to process the JSON and create packages
powershell -ExecutionPolicy Bypass -Command ^
    "$ErrorActionPreference = 'Stop'; " ^
    "$configPath = '%CONFIG_FILE%'; " ^
    "$projectRoot = '%PROJECT_ROOT%'; " ^
    "$config = Get-Content $configPath | ConvertFrom-Json; " ^
    "$outputDir = Join-Path $projectRoot $config.outputDirectory; " ^
    "if (-not (Test-Path $outputDir)) { New-Item -ItemType Directory -Path $outputDir -Force | Out-Null }; " ^
    "$successCount = 0; " ^
    "$failedCount = 0; " ^
    "$totalCount = $config.packages.Count; " ^
    "Write-Host \"Output Directory: $($config.outputDirectory)\" -ForegroundColor Blue; " ^
    "Write-Host \"Packages to process: $totalCount\" -ForegroundColor Blue; " ^
    "Write-Host \"\"; " ^
    "for ($i = 0; $i -lt $totalCount; $i++) { " ^
        "$pkg = $config.packages[$i]; " ^
        "$num = $i + 1; " ^
        "Write-Host \"[$num/$totalCount] Processing: $($pkg.name)\" -ForegroundColor Yellow; " ^
        "$sourceDir = Join-Path $projectRoot $pkg.sourceDirectory; " ^
        "$outputPath = Join-Path $outputDir $pkg.outputFileName; " ^
        "if (-not (Test-Path $sourceDir)) { " ^
            "Write-Host \"  X Source directory not found: $($pkg.sourceDirectory)\" -ForegroundColor Red; " ^
            "$failedCount++; " ^
            "Write-Host \"\"; " ^
            "continue; " ^
        "}; " ^
        "Write-Host \"  Source: $($pkg.sourceDirectory)\" -ForegroundColor Blue; " ^
        "Write-Host \"  Output: $($config.outputDirectory)/$($pkg.outputFileName)\" -ForegroundColor Blue; " ^
        "if (Test-Path $outputPath) { Remove-Item $outputPath -Force }; " ^
        "try { " ^
            "$tempZip = $outputPath; " ^
            "Add-Type -AssemblyName System.IO.Compression.FileSystem; " ^
            "$excludePatterns = $pkg.excludePatterns; " ^
            "$filesToZip = Get-ChildItem -Path $sourceDir -Recurse -File | Where-Object { " ^
                "$file = $_; " ^
                "$relativePath = $file.FullName.Substring($sourceDir.Length).TrimStart([IO.Path]::DirectorySeparatorChar); " ^
                "$shouldExclude = $false; " ^
                "foreach ($pattern in $excludePatterns) { " ^
                    "$pattern = $pattern -replace '\\*\\*/', ''; " ^
                    "if ($relativePath -like $pattern) { " ^
                        "$shouldExclude = $true; " ^
                        "break; " ^
                    "}; " ^
                "}; " ^
                "-not $shouldExclude " ^
            "}; " ^
            "$zip = [System.IO.Compression.ZipFile]::Open($tempZip, [System.IO.Compression.ZipArchiveMode]::Create); " ^
            "foreach ($file in $filesToZip) { " ^
                "$relativePath = $file.FullName.Substring($sourceDir.Length).TrimStart([IO.Path]::DirectorySeparatorChar); " ^
                "$entry = $zip.CreateEntry($relativePath, [System.IO.Compression.CompressionLevel]::Optimal); " ^
                "$entryStream = $entry.Open(); " ^
                "$fileStream = [System.IO.File]::OpenRead($file.FullName); " ^
                "$fileStream.CopyTo($entryStream); " ^
                "$fileStream.Close(); " ^
                "$entryStream.Close(); " ^
            "}; " ^
            "$zip.Dispose(); " ^
            "$fileSize = [math]::Round((Get-Item $outputPath).Length / 1KB, 2); " ^
            "Write-Host \"  √ Success! Size: $fileSize KB\" -ForegroundColor Green; " ^
            "$successCount++; " ^
        "} catch { " ^
            "Write-Host \"  X Failed: $($_.Exception.Message)\" -ForegroundColor Red; " ^
            "$failedCount++; " ^
            "if ($zip) { $zip.Dispose() }; " ^
            "if (Test-Path $outputPath) { Remove-Item $outputPath -Force }; " ^
        "}; " ^
        "Write-Host \"\"; " ^
    "}; " ^
    "Write-Host \"========================================\" -ForegroundColor Blue; " ^
    "Write-Host \"Summary\" -ForegroundColor Blue; " ^
    "Write-Host \"========================================\" -ForegroundColor Blue; " ^
    "Write-Host \"Successful: $successCount\" -ForegroundColor Green; " ^
    "if ($failedCount -gt 0) { Write-Host \"Failed: $failedCount\" -ForegroundColor Red }; " ^
    "Write-Host \"\"; " ^
    "if ($failedCount -eq 0) { " ^
        "Write-Host \"All packages created successfully!\" -ForegroundColor Green; " ^
        "exit 0; " ^
    "} else { " ^
        "Write-Host \"Some packages failed. Please check the errors above.\" -ForegroundColor Yellow; " ^
        "exit 1; " ^
    "}"

if %errorlevel% neq 0 (
    echo.
    echo Script execution failed.
    pause
    exit /b 1
)

echo.
echo Done!
pause
