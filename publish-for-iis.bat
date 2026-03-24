@echo off
REM ============================================
REM IIS Deployment Script
REM ============================================
REM Usage: publish-for-iis.bat [TARGET_PATH]
REM
REM Examples:
REM   publish-for-iis.bat x:\my-app
REM   publish-for-iis.bat \\server\share\my-app
REM
REM If no path is provided, files will be published
REM to 'publish-iis' folder only (no copying).
REM ============================================

echo Publishing application for IIS deployment...

REM Clean previous publish
if exist "publish-iis" rmdir /s /q "publish-iis"

REM Publish optimized for IIS
dotnet publish CRFleet --configuration Release --framework net8.0 --output "publish-iis" --no-self-contained --runtime win-x64

REM Remove unnecessary files for IIS (keep CRFleet.exe as it's needed for IIS)
cd publish-iis
if exist CRFleet.pdb del CRFleet.pdb
if exist appsettings.Development.json del appsettings.Development.json
if exist Castle.Core.dll del Castle.Core.dll
if exist FakeItEasy.dll del FakeItEasy.dll

REM Create logs directory for debugging
if not exist logs mkdir logs

REM Update web.config for better error logging
powershell -Command "(gc web.config) -replace 'stdoutLogEnabled=\""false\"\"', 'stdoutLogEnabled=\"\"true\"\"' | Out-File -encoding ASCII web.config"

echo.
echo IIS deployment ready in 'publish-iis' folder
echo File count:
dir /b | find /c /v ""

REM Copy to target path if provided
if "%~1"=="" (
    echo.
    echo No target path specified. Files are ready in 'publish-iis' folder.
    echo To copy to deployment location, run:
    echo   publish-for-iis.bat [TARGET_PATH]
    goto :end
)

set TARGET_PATH=%~1

echo.
echo Clearing target: %TARGET_PATH%...
if exist "%TARGET_PATH%\*" del /q "%TARGET_PATH%\*"
if exist "%TARGET_PATH%\wwwroot" rmdir /s /q "%TARGET_PATH%\wwwroot"

echo Copying to %TARGET_PATH%...
xcopy /e /i /y "." "%TARGET_PATH%\"

echo.
echo Deployment copied to %TARGET_PATH%
echo Ready to deploy to IIS server
echo Ensure .NET 8 Runtime is installed on the server

:end
cd ..
pause