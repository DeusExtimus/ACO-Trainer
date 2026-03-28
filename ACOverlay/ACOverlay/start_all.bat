@echo off
cd /d "%~dp0"

echo ================================
echo   AC OVERLAY LAUNCHER
echo ================================
echo.

:: 1. ACOverlay starten (im Hintergrund)
echo [1/3] Starte ACOverlay...
start "ACOverlay" cmd /c "dotnet run"

:: Kurz warten bis ACOverlay hochgefahren ist
echo     Warte 5 Sekunden...
timeout /t 5 /nobreak >nul

:: 2. Webserver starten (im Hintergrund, ein Verzeichnis hoeher wo launcher.html liegt)
echo [2/3] Starte Webserver...
start "ACOverlay Webserver" cmd /c "cd /d "%~dp0.." && python -m http.server 8080"

:: Kurz warten bis der Webserver bereit ist
timeout /t 2 /nobreak >nul

:: 3. Browser oeffnen
echo [3/3] Oeffne Analyzer im Browser...
start "" "http://localhost:8080/launcher.html"

echo.
echo Alles gestartet. Dieses Fenster kann geschlossen werden.
echo.
echo Zum Beenden aller Prozesse: Taskmanager -> dotnet.exe + python.exe beenden
echo.
pause
